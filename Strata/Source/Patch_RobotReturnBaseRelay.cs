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

        // Prefix skips vanilla cross-map Goto construction entirely — the original
        // job giver pathfinds toward an unreachable room every think pass.
        public static bool Prefix(ThinkNode __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.RobotSoftCompatActive)
            {
                return true;
            }
            if (!StrataPawnUtility.IsMiscRobot(pawn))
            {
                return true;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReturnBasePrefixHit);
            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }
            if (!StrataPawnUtility.IsMiscRobotRechargeCrossMap(pawn))
            {
                return true;
            }
            Map rechargeMap = StrataPawnUtility.GetMiscRobotRechargeMap(pawn);
            if (rechargeMap == null)
            {
                return true;
            }
            if (!CanPortalToRecharge(pawn))
            {
                return true;
            }
            if (PawnRelay.IsReturnBaseRetryCooldown(pawn))
            {
                __result = ThinkResult.NoJob;
                return false;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReachableLevelsCall);
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.BestFirstStepCall);
            MapPortal firstStep = LevelGraph.BestFirstStep(pawn.Map, rechargeMap, pawn.Position);
            if (firstStep == null)
            {
                PawnRelay.TouchReturnBaseRetry(pawn);
                __result = ThinkResult.NoJob;
                return false;
            }
            Job portalJob = PawnRelay.MakeReturnBasePortalJob(pawn, firstStep);
            if (portalJob != null)
            {
                StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReturnBasePortalJob);
                StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.EnterPortalRobotJob);
                PawnRelay.TouchReturnBaseRetry(pawn);
                __result = new ThinkResult(portalJob, __instance, JobTag.Misc);
            }
            else
            {
                PawnRelay.TouchReturnBaseRetry(pawn);
                __result = ThinkResult.NoJob;
            }
            return false;
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
            return LevelGraph.AnyLinkFrom(pawn.Map);
        }
    }
}

