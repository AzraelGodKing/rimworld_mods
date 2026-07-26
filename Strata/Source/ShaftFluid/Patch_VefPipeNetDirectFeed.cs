using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // PipeSystem zeros Extra* after reading it at the start of PipeSystemTick.
    // Prefix: DirectFeed (ExtraProduction) like AASB.
    // Postfix: equalize storages after this tick deposited production into tanks/overflow.
    [HarmonyPatch]
    internal static class Patch_VefPipeNetDirectFeed
    {
        private static List<VefPipeSystemBackend> cachedVefBackends;

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("PipeSystem.PipeNet"), "PipeSystemTick");
        }

        private static List<VefPipeSystemBackend> VefBackends()
        {
            if (cachedVefBackends != null)
            {
                return cachedVefBackends;
            }
            List<VefPipeSystemBackend> list = new List<VefPipeSystemBackend>();
            foreach (ShaftFluidBackend backend in ShaftFluidRegistry.All.Values)
            {
                if (backend is VefPipeSystemBackend vef)
                {
                    list.Add(vef);
                }
            }
            cachedVefBackends = list;
            return list;
        }

        private static void Prefix(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            List<VefPipeSystemBackend> backends = VefBackends();
            for (int i = 0; i < backends.Count; i++)
            {
                backends[i].InjectDirectFeedBeforeTick(__instance);
            }
        }

        private static void Postfix(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            List<VefPipeSystemBackend> backends = VefBackends();
            for (int i = 0; i < backends.Count; i++)
            {
                backends[i].EqualizeAfterPipeSystemTick(__instance);
            }
        }
    }
}
