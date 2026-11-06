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
        // The toxic, plant-growth and temperature patches ask once per cell,
        // plant or condition, many times a tick. The answer is worked out once
        // per map per tick and then read from the memo.
        public static bool BarrierProtecting(Map map) =>
            Memo(map, StormproofRegistry.AtmosphericBarriers.On(map), m => m.barrierMemo);

        public static bool ClimateProtecting(Map map) =>
            Memo(map, StormproofRegistry.ClimateStabilizers.On(map), m => m.climateMemo);

        public static bool SkyProtecting(Map map) =>
            Memo(map, StormproofRegistry.SkyRestorers.On(map), m => m.skyMemo);

        public static bool DroughtProtecting(Map map) =>
            Memo(map, StormproofRegistry.DroughtCondensers.On(map), m => m.droughtMemo);

        private static bool Memo<T>(Map map, List<T> defenders,
            System.Func<MapComponent_Stormproof, TickMemo> pick) where T : CompHazardDefender
        {
            if (defenders.Count == 0)
            {
                return false;
            }
            MapComponent_Stormproof comp = MapComponent_Stormproof.For(map);
            TickMemo memo = comp == null ? null : pick(comp);
            int tick = Find.TickManager.TicksGame;
            if (memo != null && memo.tick == tick)
            {
                return memo.value;
            }
            bool any = false;
            for (int i = 0; i < defenders.Count && !any; i++)
            {
                any = defenders[i].Protecting;
            }
            if (memo != null)
            {
                memo.tick = tick;
                memo.value = any;
            }
            return any;
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

    // One cached yes/no per map per tick. Derived, never saved.
    public sealed class TickMemo
    {
        public int tick = -1;
        public bool value;
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
