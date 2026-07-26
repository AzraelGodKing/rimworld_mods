using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Upper decks do not roll their own weather — MapComponent_WeatherSync
    // copies the floor below. Without this, A+ would clear or change rain
    // independently of the surface you see through open sky.
    [HarmonyPatch(typeof(WeatherDecider), "WeatherDeciderTick")]
    public static class Patch_WeatherDecider_Tick
    {
        public static bool Prefix(Map ___map)
        {
            return ___map == null || !StrataMapUtility.IsUpperLevel(___map);
        }
    }
}
