using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Colonists (and other portal-capable pawns) whose mod uses a custom
    // JobGiver instead of JobGiver_Work. Markers registered via
    // WorkRelaySignals.RegisterWorkSeekingJobGiverMarker opt the giver in.
    [HarmonyPatch(typeof(ThinkNode_JobGiver), nameof(ThinkNode_JobGiver.TryIssueJobPackage))]
    public static class Patch_ExternalJobGiverRelay
    {
        public static void Postfix(ThinkNode_JobGiver __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (__result.Job != null)
            {
                return;
            }
            if (!WorkRelaySignals.IsRegisteredWorkSeekingGiver(__instance))
            {
                return;
            }
            // Robots already handled by Patch_RobotWorkRelay.
            if (StrataPawnUtility.IsMiscRobot(pawn))
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.workRelayEnabled)
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
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
