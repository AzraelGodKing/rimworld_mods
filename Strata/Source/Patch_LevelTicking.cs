using HarmonyLib;
using Verse;

namespace Strata
{
    // A deep base is many full maps, and every one runs the ambient sims
    // (temperature, gas, weather upkeep, wildlife) each tick. On a vacant Strata
    // pocket level (A+ or B+) with nobody home, that work is throttled.
    public static class LevelTicking
    {
        public static bool ShouldThrottle(Map map)
        {
            return StrataLevelPerfUtility.ShouldThrottleAmbient(map);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.MapPreTick))]
    public static class Patch_MapPreTick
    {
        public static bool Prefix(Map __instance)
        {
            return !LevelTicking.ShouldThrottle(__instance);
        }
    }

    // MapPostTick always runs so smoke, raid pursuit, and other MapComponents
    // stay live on vacant underground levels.
}
