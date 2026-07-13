using HarmonyLib;
using Verse.AI.Group;

namespace Strata
{
    [HarmonyPatch(typeof(Lord), nameof(Lord.GotoToil))]
    public static class Patch_Lord_GotoToil_RaidCoordinator
    {
        public static void Postfix(Lord __instance)
        {
            RaidCoordinator.NotifyLeadToilChanged(__instance);
        }
    }
}
