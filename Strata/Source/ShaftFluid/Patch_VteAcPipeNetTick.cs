using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // After each VTE AC net tick, re-apply combined efficiency across shaft-paired maps
    // so underground AC (no local controller/compressor) stays powered by the surface side.
    [HarmonyPatch]
    internal static class Patch_VteAcPipeNetTick
    {
        private static bool Prepare()
        {
            return ModsConfig.IsActive("VanillaExpanded.Temperature");
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("VanillaTemperatureExpanded.AcPipeNet"), "PipeSystemTick");
        }

        private static void Postfix(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            VteAcPipeBackend backend = ShaftFluidRegistry.Get(VteAcPipeBackend.Channel) as VteAcPipeBackend;
            backend?.OnPipeNetTicked(__instance);
        }
    }
}
