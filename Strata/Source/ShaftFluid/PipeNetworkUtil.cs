using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Nudge mod pipe comps to rebuild their net after a junction spawns or loads.
    // Keep this cheap: only touch pipe-like comps / neighbors (not every wall/furniture).
    internal static class PipeNetworkUtil
    {
        private static readonly Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();

        private static readonly string[] RebuildMethodNames =
        {
            "ResetPipeNet",
            "SetupPipe",
            "SetupNet",
            "RebuildPipeNet",
            "ConnectToNeighbors",
        };

        private static bool? rimatomicsActive;

        private static bool? rimefellerActive;

        public static void RebuildFor(Thing thing)
        {
            if (thing is not ThingWithComps twc || !thing.Spawned)
            {
                return;
            }
            List<ThingComp> comps = twc.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                ThingComp comp = comps[i];
                if (comp == null || !LooksLikePipeComp(comp))
                {
                    continue;
                }
                for (int m = 0; m < RebuildMethodNames.Length; m++)
                {
                    TryInvoke(comp, RebuildMethodNames[m]);
                }
            }
            VefPipeNetworkUtil.ReconnectJunction(thing);
            if (RimatomicsActive)
            {
                RimatomicsPipeNetworkUtil.ReconnectJunction(thing);
            }
            if (RimefellerActive)
            {
                RimefellerPipeNetworkUtil.ReconnectJunction(thing);
            }
        }

        public static void RebuildNeighbors(Thing thing)
        {
            if (thing?.Map == null)
            {
                return;
            }
            Map map = thing.Map;
            foreach (IntVec3 cell in thing.OccupiedRect().ExpandedBy(1))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> list = cell.GetThingList(map);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing other = list[i];
                    if (other != null && other != thing && other.Spawned && IsPipeRelevant(other))
                    {
                        RebuildFor(other);
                    }
                }
            }
        }

        private static bool RimatomicsActive
        {
            get
            {
                if (rimatomicsActive == null)
                {
                    rimatomicsActive = ModLister.GetActiveModWithIdentifier("Dubwise.Rimatomics") != null;
                }
                return rimatomicsActive.Value;
            }
        }

        private static bool RimefellerActive
        {
            get
            {
                if (rimefellerActive == null)
                {
                    rimefellerActive = ModLister.GetActiveModWithIdentifier("Dubwise.Rimefeller") != null;
                }
                return rimefellerActive.Value;
            }
        }

        private static bool IsPipeRelevant(Thing thing)
        {
            if (thing is not ThingWithComps twc)
            {
                return false;
            }
            List<ThingComp> comps = twc.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                ThingComp comp = comps[i];
                if (comp is CompShaftFluidTie || LooksLikePipeComp(comp))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LooksLikePipeComp(ThingComp comp)
        {
            string name = comp.GetType().Name;
            return name.IndexOf("Pipe", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Resource", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Plumbing", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TryInvoke(object target, string methodName)
        {
            Type type = target.GetType();
            string key = type.FullName + "|" + methodName;
            if (!methodCache.TryGetValue(key, out MethodInfo method))
            {
                method = AccessTools.Method(type, methodName, Type.EmptyTypes);
                methodCache[key] = method;
            }
            method?.Invoke(target, null);
        }
    }
}
