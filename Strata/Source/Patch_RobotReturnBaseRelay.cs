using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots ThinkNode JobGivers that override TryIssueJobPackage directly
    // (not ThinkNode_JobGiver): Return2BaseRoom, RechargeEnergyIdle.
    [HarmonyPatch]
    public static class Patch_RobotReturnBaseRelay
    {
        private static readonly string[] TargetTypeNames =
        {
            "AIRobot.X2_JobGiver_Return2BaseRoom",
            "AIRobot.X2_JobGiver_RechargeEnergyIdle",
        };

        private static bool Prepare()
        {
            return DiscoverTargetMethods().Count > 0;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return DiscoverTargetMethods();
        }

        public static bool Prefix(ThinkNode __instance, Pawn pawn, JobIssueParams jobParams, ref ThinkResult __result)
        {
            return !StrataRobotSoftCompat.TryIssueCrossMapRechargeRelay(__instance, pawn, ref __result);
        }

        public static void Postfix(ThinkNode __instance, Pawn pawn, ref ThinkResult __result)
        {
            StrataRobotSoftCompat.SanitizeCrossMapThinkResult(__instance, pawn, ref __result);
            // After return-to-base / idle recharge: if the bot is parked underground
            // with its station while the surface has work, send it up.
            if (__result.Job != null
                && StrataRobotSoftCompat.IsIdleAtBaseJob(__result.Job)
                && StrataRobotSoftCompat.TryIssueRobotWorkRelay(__instance, pawn, ref __result))
            {
                return;
            }
            if (__result.Job == null)
            {
                StrataRobotSoftCompat.TryIssueRobotWorkRelay(__instance, pawn, ref __result);
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
                MethodInfo method = StrataRobotSoftCompat.ResolveThinkNodeTryIssueJobPackage(type);
                if (method != null)
                {
                    methods.Add(method);
                    patched.Add(type.Name);
                }
                else
                {
                    missing.Add(typeName + ".TryIssueJobPackage");
                }
            }
            StrataRobotSoftCompat.LogPatchResults("return/recharge ThinkNode", patched, missing);
            return methods;
        }
    }
}
