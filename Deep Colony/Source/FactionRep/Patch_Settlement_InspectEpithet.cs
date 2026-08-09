using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetInspectString))]
    public static class Patch_Settlement_InspectEpithet
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            string epithet = FactionEpithetUtility.TryGetEpithet(__instance.Faction);
            if (epithet.NullOrEmpty()) return;
            if (__result.NullOrEmpty())
                __result = epithet;
            else
                __result = __result + "\n" + epithet;
        }
    }
}
