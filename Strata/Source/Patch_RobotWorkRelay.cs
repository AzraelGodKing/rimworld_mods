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
            // Real work job — leave it. Idle/wait fillers still allow a relay.
            if (__result.Job != null && !IsIdleFillerJob(__result.Job))
            {
                return;
            }
            if (StrataRobotSoftCompat.TryIssueRobotWorkRelay(__instance, pawn, ref __result))
            {
                return;
            }
        }

        private static bool IsIdleFillerJob(Job job)
        {
            if (job?.def == null)
            {
                return true;
            }
            JobDef def = job.def;
            if (def == JobDefOf.Wait
                || def == JobDefOf.Wait_Wander
                || def == JobDefOf.Wait_MaintainPosture)
            {
                return true;
            }
            string name = def.defName;
            return name != null
                && (name.IndexOf("Wait", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Wander", System.StringComparison.OrdinalIgnoreCase) >= 0);
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
                MethodInfo method = AccessTools.DeclaredMethod(
                    type,
                    "TryIssueJobPackage",
                    new[] { typeof(Pawn), typeof(JobIssueParams) });
                // Some AIRobot builds inherit TryIssueJobPackage from ThinkNode_JobGiver.
                if (method == null)
                {
                    method = AccessTools.Method(
                        type,
                        "TryIssueJobPackage",
                        new[] { typeof(Pawn), typeof(JobIssueParams) });
                }
                if (method != null && !methods.Contains(method))
                {
                    methods.Add(method);
                }
            }
            if (methods.Count > 0)
            {
                StrataLog.Verbose("[Strata] Robot work relay: patched " + methods.Count + " AIRobot JobGiver(s).");
            }
            return methods;
        }
    }
}
