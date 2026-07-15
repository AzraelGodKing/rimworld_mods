using HarmonyLib;
using Verse;

namespace Strata
{
    // RimWorld only auto-instantiates MapComponents declared in XML; register ours
    // on every map so the atmosphere simulation and raid pursuit always run.
    // Old saves deserialize a SmokeMapComponent, which IS an
    // AtmosphereMapComponent, so this never doubles up.
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Patch_Map_FinalizeInit
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<AtmosphereMapComponent>() == null)
            {
                __instance.components.Add(new AtmosphereMapComponent(__instance));
            }
            if (__instance.GetComponent<MapComponent_RaidPursuit>() == null)
            {
                __instance.components.Add(new MapComponent_RaidPursuit(__instance));
            }
            __instance.GetComponent<AtmosphereMapComponent>()?.TrySeedBreathableAir();
        }
    }
}
