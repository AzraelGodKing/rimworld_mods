using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public static class Patch_World_FinalizeInit
    {
        public static void Postfix(World __instance)
        {
            if (__instance.GetComponent<StrataLevelLabels>() == null)
            {
                __instance.components.Add(new StrataLevelLabels(__instance));
            }
            if (__instance.GetComponent<StrataRitualTravel>() == null)
            {
                __instance.components.Add(new StrataRitualTravel(__instance));
            }
        }
    }
}
