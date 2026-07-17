using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots' Return2BaseRoom issues Goto toward a cell in the recharge
    // station's room. On Strata linked levels that room is on another map, so the
    // bot path-fails instantly and the think tree re-issues Goto every tick.
    [HarmonyPatch]
    public static class Patch_RobotReturnBaseRelay
    {
        private static bool Prepare()
        {
            return AccessTools.TypeByName("AIRobot.X2_JobGiver_Return2BaseRoom") != null;
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                AccessTools.TypeByName("AIRobot.X2_JobGiver_Return2BaseRoom"),
                "TryIssueJobPackage");
        }

        public static void Postfix(ThinkNode __instance, Pawn pawn, ref ThinkResult __result)
        {
            Job job = __result.Job;
            if (job?.def != JobDefOf.Goto || !StrataPawnUtility.IsMiscRobot(pawn))
            {
                return;
            }
            Map rechargeMap = StrataPawnUtility.GetMiscRobotRechargeMap(pawn);
            if (rechargeMap == null || rechargeMap == pawn.Map)
            {
                return;
            }
            if (!CanPortalToRecharge(pawn))
            {
                return;
            }
            MapPortal firstStep = LevelGraph.BestFirstStep(pawn.Map, rechargeMap, pawn.Position);
            if (firstStep == null)
            {
                // No route — drop the bad Goto so the think tree does not spam.
                __result = ThinkResult.NoJob;
                return;
            }
            Job portalJob = PawnRelay.MakeRelayJob(pawn, firstStep);
            if (portalJob != null)
            {
                __result = new ThinkResult(portalJob, __instance, JobTag.Misc);
            }
            else
            {
                __result = ThinkResult.NoJob;
            }
        }

        // Return-to-base is allowed even when the work relay would block a bot
        // that is low on charge — that is exactly when it needs the stairs.
        private static bool CanPortalToRecharge(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer || pawn.Drafted || pawn.InMentalState || pawn.IsBurning())
            {
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return false;
            }
            return LevelGraph.AnyLinkFrom(pawn.Map);
        }
    }
}
