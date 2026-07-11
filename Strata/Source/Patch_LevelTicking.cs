using HarmonyLib;
using Verse;

namespace Strata
{
    // A deep base is many full maps, and every one runs the ambient sims
    // (temperature, gas, weather upkeep, wildlife) each tick. On a Strata
    // underground level with nobody home, that work is almost entirely wasted -
    // so we run it only every Nth tick. Buildings and any stray pawns still
    // tick normally (that path is separate); only the per-map ambient upkeep is
    // throttled, and it stays fully live on any level a colonist is standing on
    // or the player is currently looking at.
    public static class LevelTicking
    {
        private const int Duty = 4; // run 1 in 4 ambient ticks on dormant levels

        public static bool ShouldThrottle(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return false;
            }
            if (Find.CurrentMap == map)
            {
                return false;
            }
            // Never throttle a level that has anyone on it - colonists, prisoners,
            // bugs, or raiders mid-fight.
            if (map.mapPawns.AllPawnsSpawned.Count > 0)
            {
                return false;
            }
            // Skip this tick unless it lands on the duty cycle.
            return (Find.TickManager.TicksGame + map.uniqueID) % Duty != 0;
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
