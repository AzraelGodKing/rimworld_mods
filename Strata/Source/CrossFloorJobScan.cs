using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Prototype (setting default off): instead of per-need "does that floor
    // have X" probes, ask vanilla itself. The pawn is placed on a linked
    // level's stair cell for the length of one think-node call, the vanilla
    // job giver runs there, and the pawn is put back in a finally block before
    // anything else can observe it. A found job becomes: walk to the stairs,
    // EnterPortal (PortalRelayChain handles multi-hop), then that job from the
    // queue (Patch_EnterPortalKeepJobs / CrossLevelOrderedJobs carry it over).
    //
    // Replaces, when on: work (JobRelay.TryAssignBest), food, joy, patient bed.
    // Never replaced: rest/owned bed, meditation, mental breaks, carried
    // pawns, prisoners, babies, entities, rituals, pets, robots, visitors -
    // see docs/ideas/strata-generic-job-scan.md.
    public static class CrossFloorJobScan
    {
        private const int MaxProbesPerCall = 3;

        private static readonly AccessTools.FieldRef<Thing, sbyte> MapIndexRef =
            AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");

        private static readonly AccessTools.FieldRef<Thing, IntVec3> PositionRef =
            AccessTools.FieldRefAccess<Thing, IntVec3>("positionInt");

        private static readonly AccessTools.FieldRef<PriorityWork, IntVec3> PriorityCellRef =
            OptionalFieldRef<IntVec3>("prioritizedCell");

        private static readonly AccessTools.FieldRef<PriorityWork, WorkGiverDef> PriorityGiverRef =
            OptionalFieldRef<WorkGiverDef>("prioritizedWorkGiver");

        private static readonly AccessTools.FieldRef<PriorityWork, int> PriorityTickRef =
            OptionalFieldRef<int>("prioritizeTick");

        // Null when a game update renames the field: the probe then just skips the restore.
        private static AccessTools.FieldRef<PriorityWork, T> OptionalFieldRef<T>(string name)
        {
            return AccessTools.Field(typeof(PriorityWork), name) != null
                ? AccessTools.FieldRefAccess<PriorityWork, T>(name)
                : null;
        }

        private static readonly MethodInfo TryGiveJobMethod =
            AccessTools.Method(typeof(ThinkNode_JobGiver), "TryGiveJob");

        private static Pawn scanningPawn;

        public static bool IsScanning => scanningPawn != null;

        public static bool Enabled
        {
            get
            {
                StrataSettings s = StrataMod.Settings;
                return s != null && s.genericCrossFloorScan && !s.performanceModeEnabled
                    && !StrataFaultGuard.IsTripped(StrataFaultGuard.System.CrossFloorScan);
            }
        }

        // Per-need relays call this first and step aside when it is true.
        public static bool Replaces(RelayPurpose purpose)
        {
            if (!Enabled)
            {
                return false;
            }
            switch (purpose)
            {
                case RelayPurpose.Work:
                case RelayPurpose.Food:
                case RelayPurpose.Joy:
                case RelayPurpose.Medical:
                    return true;
                default:
                    return false;
            }
        }

        public static Job TryScanJobGiver(Pawn pawn, ThinkNode_JobGiver giver, RelayPurpose purpose, int cap)
        {
            if (giver == null || TryGiveJobMethod == null)
            {
                return null;
            }
            return TryScan(pawn, purpose, cap, () => (Job)TryGiveJobMethod.Invoke(giver, new object[] { pawn }));
        }

        public static Job TryScanWork(Pawn pawn, JobGiver_Work giver, JobIssueParams jobParams)
        {
            if (giver == null)
            {
                return null;
            }
            return TryScan(pawn, RelayPurpose.Work, WorkClaimCapFor, () => giver.TryIssueJobPackage(pawn, jobParams).Job);
        }

        private const int WorkClaimCapFor = -1;

        private static Job TryScan(Pawn pawn, RelayPurpose purpose, int cap, Func<Job> scanHere)
        {
            if (IsScanning || pawn?.Map == null || !pawn.Spawned || pawn.jobs == null)
            {
                return null;
            }
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);
            int probes = 0;
            for (int i = 0; i < links.Count && probes < MaxProbesPerCall; i++)
            {
                LevelGraph.LevelLink link = links[i];
                Map dest = link.map;
                if (dest == null || dest == pawn.Map || dest.Disposed)
                {
                    continue;
                }
                if (link.firstStep == null || !link.firstStep.IsEnterable(out _))
                {
                    continue;
                }
                if (purpose == RelayPurpose.Work && WorkRelayAntiLoop.IsBlacklisted(pawn, dest))
                {
                    continue;
                }
                int claimCap = cap == WorkClaimCapFor ? WorkRelaySignals.WorkClaimCap(dest) : cap;
                if (!RelayClaims.CanClaim(pawn, dest, purpose, claimCap))
                {
                    continue;
                }
                IntVec3 arrival = link.arrivalCell;
                if (!arrival.IsValid || !arrival.InBounds(dest))
                {
                    continue;
                }
                probes++;

                Job found = ScanAt(pawn, dest, arrival, scanHere);
                if (found == null)
                {
                    continue;
                }
                if (found.def == JobDefOf.EnterPortal || found.def.defName.StartsWith("Strata_", System.StringComparison.Ordinal))
                {
                    // A relay or cross-level carry from the other floor; never chain those.
                    JobMaker.ReturnToPool(found);
                    continue;
                }

                Job hop = PawnRelay.TryClaimAndRelay(pawn, link, purpose, claimCap,
                    intendedWork: found.workGiverDef?.workType);
                if (hop == null)
                {
                    JobMaker.ReturnToPool(found);
                    continue;
                }
                pawn.jobs.jobQueue.EnqueueLast(found, purpose == RelayPurpose.Work ? JobTag.MiscWork : JobTag.SatisfyingNeeds);
                StrataLog.Verbose("[Strata] Generic scan: " + pawn.LabelShort + " -> " + found.def.defName
                    + " on " + StrataLevelLabels.Get?.GetLabel(dest));
                return hop;
            }
            return null;
        }

        // Swap, run, restore - all inside this call, on the main thread, so no
        // tick, render or save can ever see the pawn on the other map.
        private static Job ScanAt(Pawn pawn, Map dest, IntVec3 cell, Func<Job> scanHere)
        {
            int index = Find.Maps.IndexOf(dest);
            if (index < 0 || index > sbyte.MaxValue || !UnityEngine.Application.isPlaying)
            {
                return null;
            }
            sbyte oldIndex = MapIndexRef(pawn);
            IntVec3 oldPos = PositionRef(pawn);
            // Vanilla givers also write pawn state while the pawn stands on the
            // other floor: JobGiver_Work clears prioritized work it can't find
            // there, wander flips nextMoveOrderIsWait. Put them back unless the
            // probe found the job it is kept for.
            Pawn_MindState mind = pawn.mindState;
            PriorityWork priority = PriorityCellRef != null && PriorityGiverRef != null && PriorityTickRef != null
                ? mind?.priorityWork
                : null;
            IntVec3 oldPriorityCell = priority != null ? PriorityCellRef(priority) : IntVec3.Invalid;
            WorkGiverDef oldPriorityGiver = priority != null ? PriorityGiverRef(priority) : null;
            int oldPriorityTick = priority != null ? PriorityTickRef(priority) : 0;
            bool oldMoveIsWait = mind?.nextMoveOrderIsWait ?? false;
            int tick = Find.TickManager.TicksGame;
            Job job = null;
            scanningPawn = pawn;
            try
            {
                MapIndexRef(pawn) = (sbyte)index;
                PositionRef(pawn) = cell;
                job = scanHere();
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                StrataFaultGuard.Report(StrataFaultGuard.System.CrossFloorScan,
                    "scanning " + pawn.LabelShort + " on " + dest + ": " + inner.GetType().Name + ": " + inner.Message);
                job = null;
            }
            finally
            {
                MapIndexRef(pawn) = oldIndex;
                PositionRef(pawn) = oldPos;
                scanningPawn = null;
                if (mind != null)
                {
                    mind.nextMoveOrderIsWait = oldMoveIsWait;
                    if (job == null && priority != null)
                    {
                        PriorityCellRef(priority) = oldPriorityCell;
                        PriorityGiverRef(priority) = oldPriorityGiver;
                        PriorityTickRef(priority) = oldPriorityTick;
                    }
                }
            }
            if (job != null && Find.TickManager.TicksGame != tick)
            {
                JobMaker.ReturnToPool(job);
                return null;
            }
            return job;
        }
    }
}
