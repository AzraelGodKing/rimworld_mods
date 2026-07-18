using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots recharge JobGivers that extend ThinkNode_JobGiver and issue
    // GoRecharge / Goto / GoDespawn toward the station Thing via TryGiveJob.
    [HarmonyPatch]
    public static class Patch_RobotRechargeJobGiverRelay
    {
        private static readonly string[] TargetTypeNames =
        {
            "AIRobot.X2_JobGiver_RechargeEnergy",
            "AIRobot.X2_JobGiver_Return2BaseAndWait",
            "AIRobot.X2_JobGiver_Return2BaseDespawn",
        };

        private static bool Prepare()
        {
            return DiscoverTargetMethods().Count > 0;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return DiscoverTargetMethods();
        }

        public static bool Prefix(ThinkNode_JobGiver __instance, Pawn pawn, ref Job __result)
        {
            ThinkResult relay = ThinkResult.NoJob;
            if (StrataRobotSoftCompat.TryIssueCrossMapRechargeRelay(__instance, pawn, ref relay))
            {
                __result = relay.Job;
                return false;
            }
            return true;
        }

        public static void Postfix(ThinkNode_JobGiver __instance, Pawn pawn, ref Job __result)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.RobotSoftCompatActive)
            {
                return;
            }
            if (!StrataPawnUtility.IsMiscRobot(pawn) || __result == null)
            {
                return;
            }
            if (!StrataRobotSoftCompat.NeedsCrossMapRechargeRelay(pawn, out Map rechargeMap))
            {
                return;
            }
            if (!StrataRobotSoftCompat.IsBadCrossMapRechargeJob(pawn, __result, rechargeMap))
            {
                return;
            }
            StrataRobotDiagnostics.Increment(StrataRobotDiagnostics.Counter.RechargeRelayPostfixFix);
            ThinkResult relayResult = ThinkResult.NoJob;
            if (StrataRobotSoftCompat.TryIssueCrossMapRechargeRelay(__instance, pawn, ref relayResult))
            {
                __result = relayResult.Job;
            }
            else
            {
                __result = null;
            }
        }

        private static List<MethodBase> DiscoverTargetMethods()
        {
            var methods = new List<MethodBase>();
            var patched = new List<string>();
            var missing = new List<string>();
            foreach (string typeName in TargetTypeNames)
            {
                Type type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    continue;
                }
                MethodInfo method = StrataRobotSoftCompat.ResolveJobGiverTryGiveJob(type);
                if (method != null)
                {
                    methods.Add(method);
                    patched.Add(type.Name);
                }
                else
                {
                    missing.Add(typeName + ".TryGiveJob");
                }
            }
            StrataRobotSoftCompat.LogPatchResults("return/recharge JobGiver", patched, missing);
            return methods;
        }
    }
}
