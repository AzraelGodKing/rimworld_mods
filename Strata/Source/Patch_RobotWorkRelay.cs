using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots use their own JobGivers instead of JobGiver_Work, so the
    // normal work relay never fires. When a bot finds nothing locally, send it
    // through a portal toward a linked level that has haul/clean work waiting.
    [HarmonyPatch(typeof(ThinkNode_JobGiver), nameof(ThinkNode_JobGiver.TryIssueJobPackage))]
    public static class Patch_RobotWorkRelay
    {
        public static void Postfix(ThinkNode_JobGiver __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (__result.Job != null)
            {
                return;
            }
            if (pawn.IsFreeColonist)
            {
                return;
            }
            if (!StrataPawnUtility.IsMiscRobot(pawn))
            {
                return;
            }
            if (!StrataPawnUtility.IsWorkSeekingJobGiver(__instance))
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.workRelayEnabled)
            {
                return;
            }
            if (PawnRelay.IsRobotWorkScanCooldown(pawn))
            {
                return;
            }
            // Low-charge bots on the wrong level should return home, not scan for work.
            if (StrataPawnUtility.IsMiscRobotRechargeCrossMap(pawn)
                && StrataPawnUtility.MiscRobotNeedsRecharge(pawn))
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            PawnRelay.TouchRobotWorkScan(pawn);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!StrataPawnUtility.MiscRobotHasWorkOn(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, 2);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
            }
        }
    }
}
