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
            if (__result.Job != null || __instance.emergency)
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.WorkRelayActive)
            {
                return;
            }
            if (pawn == null || !pawn.IsFreeColonist)
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
            PawnRelay.TouchColonistWorkScan(pawn);
            var links = LevelGraph.ReachableLevels(pawn.Map);
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Workshop);
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
                // Soft cap so a few pawns commute to a busy level, not all of them.
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, 3);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
            }
        }
    }
}
