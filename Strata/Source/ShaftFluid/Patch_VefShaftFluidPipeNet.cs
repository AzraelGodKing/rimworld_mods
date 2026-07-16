using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // After VEF registers a pipe connector, link same-cell shaft helixien junctions too.
    [HarmonyPatch]
    internal static class Patch_VefShaftFluidPipeNet
    {
        private static bool Prepare()
        {
            return ModsConfig.IsActive("VanillaExpanded.HelixienGas")
                && ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("PipeSystem.PipeNetManager"), "RegisterConnector");
        }

        private static void Postfix(object comp)
        {
            VefPipeNetworkUtil.OnConnectorRegistered(comp);
        }
    }
}
