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
        public static bool BarrierProtecting(Map map) =>
            (StormproofMod.Settings == null || StormproofMod.Settings.allowAtmosphericBarrier)
            && AnyActive(StormproofRegistry.AtmosphericBarriers, map);

        public static bool ClimateProtecting(Map map) =>
            (StormproofMod.Settings == null || StormproofMod.Settings.allowClimateStabilizer)
            && AnyActive(StormproofRegistry.ClimateStabilizers, map);

        public static bool SkyProtecting(Map map) =>
            (StormproofMod.Settings == null || StormproofMod.Settings.allowSkyRestorer)
            && AnyActive(StormproofRegistry.SkyRestorers, map);

        public static bool DroughtProtecting(Map map) =>
            (StormproofMod.Settings == null || StormproofMod.Settings.allowDroughtCondenser)
            && AnyActive(StormproofRegistry.DroughtCondensers, map);

        private static bool AnyActive<T>(HashSet<T> set, Map map) where T : ThingComp
        {
            if (map == null || set.Count == 0)
            {
                return false;
            }
            foreach (T comp in set)
            {
                if (comp?.parent == null || !comp.parent.Spawned || comp.parent.Map != map)
                {
                    continue;
                }
                if (comp is IHazardDefender defender && defender.Protecting)
                {
                    return true;
                }
            }
            return false;
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
}
