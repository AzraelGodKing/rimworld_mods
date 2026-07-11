using HarmonyLib;
using Verse;

namespace Strata
{
    // RimWorld only auto-instantiates MapComponents declared in XML; register ours
    // on every map so smoke simulation and raid pursuit always run.
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Patch_Map_FinalizeInit
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<SmokeMapComponent>() == null)
            {
                __instance.components.Add(new SmokeMapComponent(__instance));
            }
            if (__instance.GetComponent<MapComponent_RaidPursuit>() == null)
            {
                __instance.components.Add(new MapComponent_RaidPursuit(__instance));
            }
        }
    }
}
