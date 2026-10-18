using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // When a colonist finds no work on their own level, look for work signals on
    // linked levels and send them down (or up) the stairs. Vanilla AI picks the
    // actual job the moment they arrive.
    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
    public static class Patch_WorkRelay
    {
        public static void Postfix(JobGiver_Work __instance, Pawn pawn, ref ThinkResult __result)
        {
            // The emergency giver runs first and often finds nothing. It must not
            // eat the arrival check, or a later empty normal pass never blacklists
            // and the pawn climbs the same stairs again.
            bool arriving = false;
            if (!__instance.emergency)
            {
                arriving = WorkRelayAntiLoop.ConsumePendingEmptyCheck(pawn);
                TryPreferIntendedWork(pawn, __instance, ref __result);
            }

            if (arriving && __result.Job == null && pawn?.Map != null)
            {
                WorkRelayAntiLoop.Blacklist(pawn, pawn.Map);
            }

            if (__result.Job != null)
            {
                if (!__instance.emergency
                    && PortalRelayChain.IntendedWorkDefName(pawn) == null
                    && !StrataGuardsForMeSoftCompat.IsOnDuty(pawn))
                {
                    TryLeaveForBetterWork(pawn, __instance, ref __result);
                }
                return;
            }
            if (__instance.emergency)
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.WorkRelayActive)
            {
                return;
            }
            if (pawn?.Map == null || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            // Colonists + colony mechs (Biotech).
            if (!StrataPawnUtility.CanWorkRelay(pawn))
            {
                return;
            }
            if (StrataGuardsForMeSoftCompat.IsOnDuty(pawn))
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }

            if (PawnRelay.TryIssueBestWork(pawn, __instance, out ThinkResult relay, out bool sawCandidate))
            {
                __result = relay;
                return;
            }

            // Match Misc. Robots: only cooldown after we actually looked / found
            // candidates (never burn 7500t on a no-link or empty first pass).
            if (sawCandidate)
            {
                PawnRelay.TouchColonistWorkScanFailed(pawn);
            }
            else
            {
                PawnRelay.TouchColonistWorkScanEmpty(pawn);
            }
        }

        // On arrival, try the work type that caused the trip before vanilla's
        // any-job pick sticks.
        private static void TryPreferIntendedWork(Pawn pawn, JobGiver_Work giver, ref ThinkResult result)
        {
            string name = PortalRelayChain.IntendedWorkDefName(pawn);
            if (name == null)
            {
                return;
            }
            WorkTypeDef wanted = DefDatabase<WorkTypeDef>.GetNamedSilentFail(name);
            if (wanted == null)
            {
                PortalRelayChain.ClearIntendedWork(pawn);
                return;
            }
            if (result.Job?.workGiverDef?.workType == wanted)
            {
                PortalRelayChain.ClearIntendedWork(pawn);
                return;
            }
            if (TryMakeWorkJob(pawn, wanted, out Job job))
            {
                result = new ThinkResult(job, giver, JobTag.MiscWork);
                PortalRelayChain.ClearIntendedWork(pawn);
                return;
            }
            PortalRelayChain.NoteIntendedWorkMiss(pawn);
        }

        private static void TryLeaveForBetterWork(Pawn pawn, JobGiver_Work giver, ref ThinkResult result)
        {
            if (StrataMod.Settings == null || !StrataMod.Settings.WorkRelayActive || pawn?.Map == null)
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map) || !StrataPawnUtility.CanWorkRelay(pawn) || !PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }
            WorkTypeDef localType = result.Job?.workGiverDef?.workType;
            if (localType == WorkTypeDefOf.Firefighter || localType == WorkTypeDefOf.Doctor)
            {
                return;
            }
            // Simple checkbox work priorities stay on the old "only when idle" relay.
            if (Find.PlaySettings == null || !Find.PlaySettings.useWorkPriorities)
            {
                return;
            }
            int maxDepth = StrataMod.Settings.priorityWorkMaxDepth;
            bool emergencyOverride = StrataMod.Settings.priorityWorkEmergency;
            if (maxDepth <= 0 && !emergencyOverride)
            {
                return;
            }
            int localPriority = localType != null && pawn.workSettings != null
                ? pawn.workSettings.GetPriority(localType)
                : 4;
            List<LevelGraph.LevelLink> links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            int best = -1;
            int bestPriority = int.MaxValue;
            WorkTypeDef bestType = null;
            bool bestEmergency = false;
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (link.map == null || WorkRelayAntiLoop.IsBlacklisted(pawn, link.map))
                {
                    continue;
                }
                if (!WorkRelaySignals.TryBestSignalled(pawn, link.map, out int remotePriority, out WorkTypeDef remoteType, out bool emergency))
                {
                    continue;
                }
                bool emergencyTrip = emergencyOverride && emergency && remoteType != null
                    && (remoteType == WorkTypeDefOf.Firefighter || remoteType == WorkTypeDefOf.Doctor);
                bool better = maxDepth > 0 && link.depth <= maxDepth && remotePriority < localPriority;
                if (!emergencyTrip && !better)
                {
                    continue;
                }
                bool wins = best < 0
                    || (emergencyTrip && !bestEmergency)
                    || (emergencyTrip == bestEmergency && remotePriority < bestPriority)
                    || (emergencyTrip == bestEmergency && remotePriority == bestPriority && link.depth < links[best].depth);
                if (!wins)
                {
                    continue;
                }
                best = i;
                bestPriority = remotePriority;
                bestType = remoteType;
                bestEmergency = emergencyTrip;
            }
            if (best < 0)
            {
                return;
            }
            int cap = WorkRelaySignals.WorkClaimCap(links[best].map);
            Job job = PawnRelay.TryClaimAndRelay(pawn, links[best], RelayPurpose.Work, cap, intendedWork: bestType);
            if (job != null)
            {
                result = new ThinkResult(job, giver, JobTag.MiscWork);
            }
        }

        private static bool TryMakeWorkJob(Pawn pawn, WorkTypeDef type, out Job job)
        {
            job = null;
            if (pawn?.workSettings == null || type?.workGiversByPriority == null)
            {
                return false;
            }
            for (int i = 0; i < type.workGiversByPriority.Count; i++)
            {
                WorkGiver giver = type.workGiversByPriority[i]?.Worker;
                if (giver == null || !GiverUsable(pawn, giver) || giver.ShouldSkip(pawn))
                {
                    continue;
                }
                try
                {
                    Job direct = giver.NonScanJob(pawn);
                    if (direct != null)
                    {
                        job = direct;
                        return true;
                    }
                    if (giver is WorkGiver_Scanner scanner
                        && (TryScanThings(pawn, scanner, out job) || TryScanCells(pawn, scanner, out job)))
                    {
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    StrataLog.Verbose("[Strata] Intended work giver " + giver.def.defName
                        + " failed: " + e.GetType().Name);
                }
            }
            return false;
        }

        private static bool GiverUsable(Pawn pawn, WorkGiver giver)
        {
            WorkTypeDef type = giver.def?.workType;
            if (type != null && pawn.workSettings != null && !pawn.workSettings.WorkIsActive(type))
            {
                return false;
            }
            return giver.MissingRequiredCapacity(pawn) == null;
        }

        private const int WorkScanCap = 80;
        private const int WorkCellCheckCap = 16;

        private static bool TryScanThings(Pawn pawn, WorkGiver_Scanner scanner, out Job job)
        {
            job = null;
            System.Collections.IEnumerable globals = scanner.PotentialWorkThingsGlobal(pawn);
            if (globals != null)
            {
                int seen = 0;
                foreach (Thing thing in globals)
                {
                    seen++;
                    if (seen > WorkScanCap)
                    {
                        break;
                    }
                    if (thing == null || !thing.Spawned || thing.Map != pawn.Map || thing.IsForbidden(pawn))
                    {
                        continue;
                    }
                    if (!scanner.HasJobOnThing(pawn, thing))
                    {
                        continue;
                    }
                    Job next = scanner.JobOnThing(pawn, thing);
                    if (next != null)
                    {
                        job = next;
                        return true;
                    }
                }
                return false;
            }

            ThingRequest request = scanner.PotentialWorkThingRequest;
            if (request.group == ThingRequestGroup.Undefined)
            {
                return false;
            }
            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                request,
                scanner.PathEndMode,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()),
                9999f,
                thing => thing != null && !thing.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, thing));
            if (found == null)
            {
                return false;
            }
            job = scanner.JobOnThing(pawn, found);
            return job != null;
        }

        private static bool TryScanCells(Pawn pawn, WorkGiver_Scanner scanner, out Job job)
        {
            job = null;
            System.Collections.Generic.IEnumerable<IntVec3> cells = scanner.PotentialWorkCellsGlobal(pawn);
            if (cells == null)
            {
                return false;
            }
            var nearest = new List<IntVec3>(WorkCellCheckCap);
            var nearestDist = new List<int>(WorkCellCheckCap);
            int seen = 0;
            foreach (IntVec3 cell in cells)
            {
                seen++;
                if (seen > WorkScanCap)
                {
                    break;
                }
                if (!cell.InBounds(pawn.Map) || !cell.InAllowedArea(pawn))
                {
                    continue;
                }
                int dist = pawn.Position.DistanceToSquared(cell);
                int slot = nearest.Count;
                if (slot < WorkCellCheckCap)
                {
                    nearest.Add(cell);
                    nearestDist.Add(dist);
                }
                else
                {
                    int worst = 0;
                    for (int i = 1; i < nearestDist.Count; i++)
                    {
                        if (nearestDist[i] > nearestDist[worst])
                        {
                            worst = i;
                        }
                    }
                    if (dist >= nearestDist[worst])
                    {
                        continue;
                    }
                    nearest[worst] = cell;
                    nearestDist[worst] = dist;
                }
            }
            for (int i = 0; i < nearest.Count; i++)
            {
                if (!scanner.HasJobOnCell(pawn, nearest[i]))
                {
                    continue;
                }
                Job next = scanner.JobOnCell(pawn, nearest[i]);
                if (next != null)
                {
                    job = next;
                    return true;
                }
            }
            return false;
        }
    }
}
