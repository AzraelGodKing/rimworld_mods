using Verse;

namespace Strata
{
    public static class AtmosphereVolumeUtility
    {
        // Alert/thought worry bands — slightly above hediff harm thresholds so
        // UI warns before severity ramps hard (O₂ harm 0.12, CO₂ harm 0.18).
        public const float OxygenWorryThreshold = 0.14f;
        public const float CarbonDioxideWorryThreshold = 0.16f;

        // Metabolism (pawn inhale / plant exchange) only on B1+ sealed volumes.
        public static bool AllowsBreathMetabolism(Map map, Room room)
        {
            if (map == null || room == null || room.UsesOutdoorTemperature)
            {
                return false;
            }
            if (AtmosphericMix.ForcesAmbientInEnclosedRooms(map))
            {
                return false;
            }
            return StrataMapUtility.IsUnderground(map) && !StrataMapUtility.IsUpperLevel(map);
        }
    }
}
