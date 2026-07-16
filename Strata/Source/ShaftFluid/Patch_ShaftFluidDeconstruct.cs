using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(Building), nameof(Building.DeconstructibleBy))]
    public static class Patch_ShaftFluidDeconstruct
    {
        public static void Postfix(Building __instance, ref AcceptanceReport __result)
        {
            if (!__result.Accepted)
            {
                return;
            }
            CompShaftFluidJunctionLink link = __instance.TryGetComp<CompShaftFluidJunctionLink>();
            if (link != null && link.IsAutoSpawned)
            {
                __result = "It is driven by the junction on the level above.";
            }
        }
    }
}
