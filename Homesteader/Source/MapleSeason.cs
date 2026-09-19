using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Maple sap runs hard in early spring / cold snaps and nearly stops in summer.
    /// </summary>
    internal static class MapleSeason
    {
        internal const string MapleDefName = "Homesteader_Plant_MapleTree";

        internal static float YieldFactor(Map map)
        {
            if (map == null)
            {
                return 1f;
            }

            Season season = GenLocalDate.Season(map);
            float outdoor = map.mapTemperature.OutdoorTemp;
            float factor;
            switch (season)
            {
                case Season.Spring:
                    factor = outdoor < 12f ? 1.45f : 1.15f;
                    break;
                case Season.Winter:
                    factor = outdoor < 5f ? 0.85f : 0.55f;
                    break;
                case Season.Fall:
                    factor = 0.7f;
                    break;
                default:
                    factor = outdoor > 22f ? 0.12f : 0.28f;
                    break;
            }

            return factor;
        }

        internal static string InspectLine(Map map)
        {
            float f = YieldFactor(map);
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

    [HarmonyPatch]
    internal static class Patch_MapleHarvestYield
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Plant), "YieldNow");
        }

        private static void Postfix(Plant __instance, ref int __result)
        {
            if (__result <= 0 || __instance?.def == null
                || __instance.def.defName != MapleSeason.MapleDefName
                || __instance.Map == null)
            {
                return;
            }

            float factor = MapleSeason.YieldFactor(__instance.Map);
            __result = UnityEngine.Mathf.Max(0,
                UnityEngine.Mathf.RoundToInt(__result * factor));
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString))]
    internal static class Patch_MapleInspectSeason
    {
        private static void Postfix(ThingWithComps __instance, ref string __result)
        {
            if (__instance?.def == null
                || __instance.def.defName != MapleSeason.MapleDefName
                || __instance.Map == null)
            {
                return;
            }

            string line = MapleSeason.InspectLine(__instance.Map);
            if (line.NullOrEmpty())
            {
                return;
            }

            if (__result.NullOrEmpty())
            {
                __result = line;
            }
            else
            {
                __result = __result + "\n" + line;
            }
        }
    }
}
