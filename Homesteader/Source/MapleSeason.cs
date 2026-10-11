using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Maple sap runs hard in cool spring weather, slows in fall and winter, and nearly stops in summer.
    /// </summary>
    internal static class MapleSeason
    {
        internal static float YieldFactor(Map map)
        {
            if (map == null)
            {
                return 1f;
            }

            float outdoor = map.mapTemperature.OutdoorTemp;
            switch (GenLocalDate.Season(map))
            {
                case Season.Spring:
                    return outdoor < 12f ? 1.45f : 1.15f;
                case Season.Winter:
                case Season.PermanentWinter:
                    return outdoor < 5f ? 0.85f : 0.55f;
                case Season.Fall:
                    return 0.7f;
                default:
                    return outdoor > 22f ? 0.12f : 0.28f;
            }
        }
    }

    public class CompProperties_MapleSap : CompProperties
    {
        public CompProperties_MapleSap()
        {
            compClass = typeof(CompMapleSap);
        }
    }

    public class CompMapleSap : ThingComp
    {
        public override string CompInspectStringExtra()
        {
            Map map = parent.MapHeld;
            if (map == null)
            {
                return null;
            }

            float f = MapleSeason.YieldFactor(map);
            if (f >= 1.1f)
            {
                return "Homesteader_MapleSeason_Running".Translate();
            }

            if (f <= 0.35f)
            {
                return "Homesteader_MapleSeason_Dry".Translate();
            }

            return "Homesteader_MapleSeason_Slow".Translate();
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.YieldNow))]
    public static class Patch_MapleHarvestYield
    {
        public static void Postfix(Plant __instance, ref int __result)
        {
            if (__result <= 0 || __instance?.Map == null || __instance.TryGetComp<CompMapleSap>() == null)
            {
                return;
            }

            __result = Mathf.Max(1, Mathf.RoundToInt(__result * MapleSeason.YieldFactor(__instance.Map)));
        }
    }
}
