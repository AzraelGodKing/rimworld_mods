using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    [HarmonyPatch]
    internal static class Patch_RimatomicsCoolingNetTick
    {
        private static bool Prepare()
        {
            return ModsConfig.IsActive("Dubwise.Rimatomics");
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Rimatomics.CoolingNet"), "Tick");
        }

        private static void Postfix(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            RimatomicsCoolingBackend backend = ShaftFluidRegistry.Get("rimatomics_coolant") as RimatomicsCoolingBackend;
            backend?.RecalculateLoopRatio(__instance);
        }
    }
}
