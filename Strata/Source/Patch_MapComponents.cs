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
            if (__instance.GetComponent<ShoringMapComponent>() == null)
            {
                __instance.components.Add(new ShoringMapComponent(__instance));
            }
            if (__instance.GetComponent<FloodMapComponent>() == null)
            {
                __instance.components.Add(new FloodMapComponent(__instance));
            }
            if (ModsConfig.IsActive("Ludeon.RimWorld.Odyssey")
                && __instance.GetComponent<MapComponent_StrataGravshipUpperDeckSync>() == null)
            {
                __instance.components.Add(new MapComponent_StrataGravshipUpperDeckSync(__instance));
            }
            if (ModsConfig.IsActive("Ludeon.RimWorld.Odyssey")
                && StrataGravshipUtility.IsGravshipLinkedLevel(__instance)
                && __instance.GetComponent<MapComponent_StrataProjectedSubstructure>() == null)
            {
                __instance.components.Add(new MapComponent_StrataProjectedSubstructure(__instance));
            }
            if (__instance.GetComponent<MapComponent_StrataDeferredGen>() == null
                && (StrataDeferredGenUtility.HasPending(__instance)
                    || StrataMapUtility.IsUnderground(__instance)
                    || StrataMapUtility.IsUpperLevel(__instance)))
            {
                __instance.components.Add(new MapComponent_StrataDeferredGen(__instance));
            }
            if (StrataMapUtility.IsUpperLevel(__instance)
                && __instance.GetComponent<MapComponent_DepthDim>() == null)
            {
                __instance.components.Add(new MapComponent_DepthDim(__instance));
            }
            // Every colony / linked floor can be the viewed map that scans
            // siblings for off-view hostiles.
            if (__instance.GetComponent<MapComponent_CrossLevelThreatWatch>() == null
                && (__instance.IsPlayerHome
                    || StrataCrossLevelThreatNotify.IsLinkedFloor(__instance)))
            {
                __instance.components.Add(new MapComponent_CrossLevelThreatWatch(__instance));
            }
            StrataDeferredGenUtility.AttachPending(__instance);
        }
    }
}
