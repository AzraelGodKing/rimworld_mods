using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // After VEF registers a pipe connector, link same-cell shaft junctions too
    // (Helixien gas, VTE air ducts, etc.).
    [HarmonyPatch]
    internal static class Patch_VefShaftFluidPipeNet
    {
        private static bool Prepare()
        {
            if (!ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core"))
            {
                return false;
            }
            return TargetMethod() != null;
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
