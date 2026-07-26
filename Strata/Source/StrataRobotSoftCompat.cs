using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Shared Misc. Robots cross-level recharge routing. Misc. Robots JobGivers
    // target recharge-station rooms/things on rechargeStation.Map; on Strata linked
    // levels that map is another pocket map, so vanilla Goto/GoAndWait/GoRecharge
    // path-fail or reservation-fail every think pass.
    internal static class StrataRobotSoftCompat
    {
        internal static bool TryIssueCrossMapRechargeRelay(ThinkNode node, Pawn pawn, ref ThinkResult result)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.RobotSoftCompatActive)
            {
                return false;
            }
            if (!StrataPawnUtility.IsMiscRobot(pawn))
            {
                return false;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.RechargeRelayPrefixHit);
            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return false;
            }
            if (!NeedsCrossMapRechargeRelay(pawn, out Map rechargeMap))
            {
                return false;
            }
            // Cross-map: never fall through to vanilla recharge/return JobGivers.
            if (PawnRelay.IsReturnBaseRetryCooldown(pawn))
            {
                result = ThinkResult.NoJob;
                return true;
            }
            if (!CanPortalToRecharge(pawn))
            {
                PawnRelay.TouchReturnBaseRetry(pawn);
                result = ThinkResult.NoJob;
                return true;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReachableLevelsCall);
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.BestFirstStepCall);
            MapPortal firstStep = LevelGraph.BestFirstStep(pawn.Map, rechargeMap, pawn.Position, pawn);
            if (firstStep == null)
            {
                PawnRelay.TouchReturnBaseRetry(pawn);
                result = ThinkResult.NoJob;
                return true;
            }
            Job portalJob = PawnRelay.MakeReturnBasePortalJob(pawn, firstStep, rechargeMap);
            PawnRelay.TouchReturnBaseRetry(pawn);
            if (portalJob != null)
            {
                StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReturnBasePortalJob);
                StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.EnterPortalRobotJob);
                result = new ThinkResult(portalJob, node, JobTag.Misc);
            }
            else
            {
                result = ThinkResult.NoJob;
            }
            return true;
        }

        internal static void SanitizeCrossMapThinkResult(ThinkNode node, Pawn pawn, ref ThinkResult result)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.RobotSoftCompatActive)
            {
                return;
            }
            if (!StrataPawnUtility.IsMiscRobot(pawn) || result.Job == null)
            {
                return;
            }
            if (!NeedsCrossMapRechargeRelay(pawn, out Map rechargeMap))
            {
                return;
            }
            if (!IsBadCrossMapRechargeJob(pawn, result.Job, rechargeMap))
            {
                return;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.RechargeRelayPostfixFix);
            ThinkResult relayResult = ThinkResult.NoJob;
            if (TryIssueCrossMapRechargeRelay(node, pawn, ref relayResult))
            {
                result = relayResult;
            }
            else
            {
                result = ThinkResult.NoJob;
            }
        }

        internal static bool IsBadCrossMapRechargeJob(Pawn pawn, Job job, Map rechargeMap)
        {
            if (job == null || pawn?.Map == null || rechargeMap == null || rechargeMap == pawn.Map)
            {
                return false;
            }
            if (job.def == JobDefOf.Goto)
            {
                return true;
            }
            if (job.targetA.HasThing)
            {
                Thing target = job.targetA.Thing;
                if (target.Spawned && target.Map != pawn.Map)
                {
                    return true;
                }
            }
            string defName = job.def?.defName;
            if (defName != null
                && (defName.IndexOf("GoAndWait", StringComparison.OrdinalIgnoreCase) >= 0
                    || defName.IndexOf("GoRecharge", StringComparison.OrdinalIgnoreCase) >= 0
                    || defName.IndexOf("GoDespawn", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return job.targetA.HasThing;
            }
            return false;
        }

        internal static bool NeedsCrossMapRechargeRelay(Pawn pawn, out Map rechargeMap)
        {
            rechargeMap = null;
            if (!StrataPawnUtility.IsMiscRobot(pawn) || pawn?.Map == null)
            {
                return false;
            }
            if (StrataPawnUtility.IsMiscRobotRechargeCrossMap(pawn))
            {
                rechargeMap = StrataPawnUtility.GetMiscRobotRechargeMap(pawn);
                return rechargeMap != null;
            }
            Thing station = StrataPawnUtility.GetMiscRobotRechargeStation(pawn);
            if (station is { Spawned: true })
            {
                rechargeMap = station.Map;
                return rechargeMap != pawn.Map;
            }
            return false;
        }

        internal static MethodInfo ResolveThinkNodeTryIssueJobPackage(Type type)
        {
            if (type == null)
            {
                return null;
            }
            return AccessTools.DeclaredMethod(
                type,
                "TryIssueJobPackage",
                new[] { typeof(Pawn), typeof(JobIssueParams) });
        }

        internal static MethodInfo ResolveJobGiverTryGiveJob(Type type)
        {
            if (type == null)
            {
                return null;
            }
            return AccessTools.DeclaredMethod(type, "TryGiveJob", new[] { typeof(Pawn) });
        }

        internal static void LogPatchResults(string category, List<string> patched, List<string> missing)
        {
            if (patched.Count > 0)
            {
                Log.Message("[Strata] Robot " + category + " relay: patched "
                    + patched.Count + " — " + string.Join(", ", patched) + ".");
            }
            if (missing.Count > 0)
            {
                Log.Warning("[Strata] Robot " + category + " relay: FAILED — "
                    + string.Join(", ", missing) + ".");
                StrataFaultGuard.Report(StrataFaultGuard.System.RobotSoftCompat,
                    category + " patch miss: " + string.Join(", ", missing));
            }
            if (patched.Count == 0 && missing.Count == 0)
            {
                Log.Message("[Strata] Robot " + category + " relay: AIRobot assembly not loaded.");
            }
        }

        internal static bool CanPortalToRecharge(Pawn pawn)
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
