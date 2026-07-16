using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Dig shafts are only designated via the Dig down gizmo, not the architect menu.
    [HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
    public static class Patch_HideDigShaftDesignator
    {
        public static void Postfix(Designator_Build __instance, ref bool __result)
        {
            if (__result && __instance.PlacingDef?.defName == "Strata_DigDownShaft")
            {
                __result = false;
            }
        }
    }
}
