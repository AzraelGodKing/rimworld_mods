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
            // Work-relay arrival: real job clears the check; null → blacklist floor.
            if (WorkRelayAntiLoop.ConsumePendingEmptyCheck(pawn))
            {
                if (__result.Job == null && !__instance.emergency && pawn?.Map != null)
                {
                    WorkRelayAntiLoop.Blacklist(pawn, pawn.Map);
                }
            }

            if (__result.Job != null)
            {
                if (!__instance.emergency)
                {
                    TryPreferIntendedWork(pawn, __instance, ref __result);
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
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }

            var links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksForWork(links, pawn);
            bool sawCandidate = false;
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
                sawCandidate = true;
                int cap = WorkRelaySignals.WorkClaimCap(link.map);
                WorkRelaySignals.TryBestSignalled(pawn, link.map, out _, out WorkTypeDef intended, out _);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, cap, intendedWork: intended);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
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
            PortalRelayChain.ClearIntendedWork(pawn);
            if (wanted == null || result.Job?.workGiverDef?.workType == wanted)
            {
                return;
            }
            if (TryMakeWorkJob(pawn, wanted, out Job job))
            {
                result = new ThinkResult(job, giver, JobTag.MiscWork);
            }
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
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
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
                if (emergencyTrip && remoteType != WorkTypeDefOf.Firefighter && remoteType != WorkTypeDefOf.Doctor)
                {
                    continue;
                }
                int cap = WorkRelaySignals.WorkClaimCap(link.map);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, cap, intendedWork: remoteType);
                if (job != null)
                {
                    result = new ThinkResult(job, giver, JobTag.MiscWork);
                    return;
                }
            }
        }

        private static bool TryMakeWorkJob(Pawn pawn, WorkTypeDef type, out Job job)
        {
            job = null;
            if (pawn?.workSettings == null || type?.workGiversByPriority == null)
            {
                return false;
            }
            try
            {
                for (int i = 0; i < type.workGiversByPriority.Count; i++)
                {
                    WorkGiverDef giverDef = type.workGiversByPriority[i];
                    if (giverDef?.Worker is not WorkGiver_Scanner scanner)
                    {
                        continue;
                    }
                    if (scanner.ShouldSkip(pawn))
                    {
                        continue;
                    }
                    System.Collections.IEnumerable things = scanner.PotentialWorkThingsGlobal(pawn);
                    if (things == null)
                    {
                        continue;
                    }
                    int seen = 0;
                    foreach (Thing thing in things)
                    {
                        seen++;
                        if (seen > 12)
                        {
                            break;
                        }
                        if (thing == null || !thing.Spawned || thing.Map != pawn.Map)
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
                }
            }
            catch (System.Exception e)
            {
                StrataLog.Verbose("[Strata] Intended work " + type.defName + " could not be issued: " + e.GetType().Name);
            }
            return false;
        }
    }
}
