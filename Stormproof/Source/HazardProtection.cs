using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Shared queries for late-game natural-hazard buildings. Protection is
    // map-wide while a building is powered (same fantasy as the solar shield):
    // one late-game installation hardens the whole colony map.
    public static class HazardProtection
    {
        // Each defender's Protecting already includes its mod-settings switch.
        public static bool BarrierProtecting(Map map) =>
            AnyProtecting(StormproofRegistry.AtmosphericBarriers.On(map));

        public static bool ClimateProtecting(Map map) =>
            AnyProtecting(StormproofRegistry.ClimateStabilizers.On(map));

        public static bool SkyProtecting(Map map) =>
            AnyProtecting(StormproofRegistry.SkyRestorers.On(map));

        public static bool DroughtProtecting(Map map) =>
            AnyProtecting(StormproofRegistry.DroughtCondensers.On(map));

        private static bool AnyProtecting<T>(List<T> defenders) where T : CompHazardDefender
        {
            for (int i = 0; i < defenders.Count; i++)
            {
                if (defenders[i].Protecting)
                {
                    return true;
                }
            }
            return false;
        }

        // Temperature conditions only know their affected maps, not the map
        // being asked about: one stabilized map is enough to zero the offset.
        public static float ClimateOffset(GameCondition condition, float offset)
        {
            if (offset == 0f || condition == null)
            {
                return offset;
            }
            foreach (Map map in condition.AffectedMaps)
            {
                if (ClimateProtecting(map))
                {
                    return 0f;
                }
            }
            return offset;
        }

        public static bool ConditionActive(Map map, GameConditionDef def) =>
            map != null && def != null && map.gameConditionManager.ConditionIsActive(def);

        public static bool AnyConditionActive(Map map, params GameConditionDef[] defs)
        {
            if (map == null || defs == null)
            {
                return false;
            }
            for (int i = 0; i < defs.Length; i++)
            {
                if (ConditionActive(map, defs[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public interface IHazardDefender
    {
        bool Protecting { get; }
    }

    // Buildings that jump from idle nameplate draw to activePowerConsumption
    // while their triggering hazard is on. Grid forecasts must use this, not
    // Props.PowerConsumption alone (AZR-319).
    public interface IHazardPowerRamp
    {
        float ForecastDrawWatts { get; }
    }
}
