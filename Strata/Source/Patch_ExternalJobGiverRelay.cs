using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Colonists (and other portal-capable pawns) whose mod uses a custom
    // JobGiver instead of JobGiver_Work. Markers registered via
    // WorkRelaySignals.RegisterWorkSeekingJobGiverMarker opt the giver in.
    //
    // Patches only registered JobGiver types (dynamic TargetMethods), not every
    // ThinkNode_JobGiver in the game — same hitch pattern as the old robot relay.
    [HarmonyPatch]
    public static class Patch_ExternalJobGiverRelay
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
            if (__result.Job != null)
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.WorkRelayActive)
            {
                return;
            }
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return;
            }
            if (!StrataPawnUtility.CanWorkRelay(pawn))
            {
                return;
            }
            // Robots already handled by Patch_RobotWorkRelay.
            if (StrataPawnUtility.IsMiscRobot(pawn))
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }

            bool sawCandidate = false;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
                sawCandidate = true;
                int cap = WorkRelaySignals.WorkClaimCap(link.map);
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Work, cap);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
            }

            if (sawCandidate)
            {
                PawnRelay.TouchColonistWorkScanFailed(pawn);
            }
            else
            {
                PawnRelay.TouchColonistWorkScanEmpty(pawn);
            }
        }

        private static List<MethodBase> DiscoverTargetMethods()
        {
            var methods = new List<MethodBase>();
            if (WorkRelaySignals.RegisteredWorkSeekingGiverMarkerCount == 0)
            {
                return methods;
            }
            Type jobGiverBase = typeof(ThinkNode_JobGiver);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                if (types == null)
                {
                    continue;
                }
                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.IsAbstract || !jobGiverBase.IsAssignableFrom(type))
                    {
                        continue;
                    }
                    if (!WorkRelaySignals.IsRegisteredWorkSeekingGiverType(type))
                    {
                        continue;
                    }
                    MethodInfo method = AccessTools.DeclaredMethod(type, "TryIssueJobPackage");
                    if (method != null)
                    {
                        methods.Add(method);
                    }
                }
            }
            if (methods.Count > 0)
            {
                StrataLog.Verbose("[Strata] External work relay: patched " + methods.Count + " registered JobGiver(s).");
            }
            return methods;
        }
    }
}
