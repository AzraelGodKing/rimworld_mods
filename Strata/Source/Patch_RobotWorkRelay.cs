using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots use their own JobGivers instead of JobGiver_Work, so the
    // normal work relay never fires. When a bot finds nothing locally, send it
    // through a portal toward a linked level that has haul/clean work waiting.
    //
    // Patches only AIRobot work-seeking JobGivers (dynamic TargetMethods), not
    // every ThinkNode_JobGiver in the game — the old blanket postfix ran on
    // every colonist/animal/raider think pass and caused periodic hitches.
    [HarmonyPatch]
    public static class Patch_RobotWorkRelay
    {
        private static bool Prepare()
        {
            return DiscoverTargetMethods().Count > 0;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return DiscoverTargetMethods();
        }

        public static void Postfix(ThinkNode_JobGiver __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (!StrataPawnUtility.IsMiscRobot(pawn))
            {
                return;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.WorkSeekingGiverCall);
            if (__result.Job != null)
            {
                return;
            }
            if (StrataMod.Settings != null
                && (!StrataMod.Settings.RobotSoftCompatActive || !StrataMod.Settings.robotWorkRelayEnabled))
            {
                return;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.WorkRelayPostfixHit);
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
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.ReachableLevelsCall);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!StrataPawnUtility.MiscRobotHasWorkOn(pawn, link.map))
                {
                    continue;
                }
                StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.BestFirstStepCall);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, 2);
                if (job != null)
                {
                    StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.WorkRelayJobIssued);
                    StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.EnterPortalRobotJob);
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
            }
        }

        private static List<MethodBase> DiscoverTargetMethods()
        {
            var methods = new List<MethodBase>();
            Type robotType = AccessTools.TypeByName("AIRobot.X2_AIRobot");
            if (robotType == null)
            {
                return methods;
            }
            Type jobGiverBase = typeof(ThinkNode_JobGiver);
            foreach (Type type in robotType.Assembly.GetTypes())
            {
                if (type.IsAbstract || !jobGiverBase.IsAssignableFrom(type))
                {
                    continue;
                }
                if (!StrataPawnUtility.IsWorkSeekingJobGiverType(type))
                {
                    continue;
                }
                MethodInfo method = AccessTools.DeclaredMethod(type, "TryIssueJobPackage");
                if (method != null)
                {
                    methods.Add(method);
                }
            }
            if (methods.Count > 0)
            {
                Log.Message("[Strata] Robot work relay: patched " + methods.Count + " AIRobot JobGiver(s).");
            }
            return methods;
        }
    }
}
