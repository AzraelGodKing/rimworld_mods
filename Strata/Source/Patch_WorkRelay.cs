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

            if (__result.Job != null || __instance.emergency)
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

            var links = LevelGraph.ReachableLevels(pawn.Map);
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Workshop);
            bool sawCandidate = false;
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
                sawCandidate = true;
                int cap = WorkRelaySignals.WorkClaimCap(link.map);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, cap);
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
    }
}
