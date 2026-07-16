using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Nudge mod pipe comps to rebuild their net after a junction spawns or loads.
    internal static class PipeNetworkUtil
    {
        public static void RebuildFor(Thing thing)
        {
            if (thing is not ThingWithComps twc || !thing.Spawned)
            {
                return;
            }
            foreach (ThingComp comp in twc.AllComps)
            {
                if (comp == null)
                {
                    continue;
                }
                TryInvoke(comp, "ResetPipeNet");
                TryInvoke(comp, "SetupPipe");
                TryInvoke(comp, "SetupNet");
                TryInvoke(comp, "RebuildPipeNet");
                TryInvoke(comp, "ConnectToNeighbors");
                TryInvoke(comp, "SetPipeNet");
            }
            TryInvoke(twc, "ResetPipeNet");
            TryInvoke(twc, "SetupPipe");
            VefPipeNetworkUtil.ReconnectJunction(thing);
            RimatomicsPipeNetworkUtil.ReconnectJunction(thing);
            RimefellerPipeNetworkUtil.ReconnectJunction(thing);
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
                foreach (Thing other in cell.GetThingList(map))
                {
                    if (other != null && other != thing && other.Spawned)
                    {
                        RebuildFor(other);
                    }
                }
            }
        }

        private static void TryInvoke(object target, string methodName)
        {
            MethodInfo method = AccessTools.Method(target.GetType(), methodName, new System.Type[0]);
            method?.Invoke(target, null);
        }
    }
}
