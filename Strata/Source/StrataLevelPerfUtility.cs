using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Tracks per-level hibernation for the Levels tab and dev tools.
    public static class StrataLevelPerfUtility
    {
        public const int AtmosphereReducedMultiplier = 4;
        public const int AtmosphereVacantMultiplier = 8;
        public const int AtmospherePerformanceMultiplier = 4;
        public const int AtmosphereMultiLevelMultiplier = 2;

        private static readonly HashSet<int> forcedHibernateMapIds = new HashSet<int>();
        private static int cachedPocketLevelCount = -1;
        private static int cachedPocketLevelCountTick = -1;

        public static int PawnCount(Map map)
        {
            return map?.mapPawns.AllPawnsSpawned.Count ?? 0;
        }

        // Colonists plus Misc. Robots — wild animals and raiders should not
        // keep a vacant pocket level from hibernating.
        public static int ColonyPresenceCount(Map map)
        {
            if (map == null)
            {
                return 0;
            }
            int count = map.mapPawns.FreeColonistsSpawnedCount;
            if (count > 0)
            {
                return count;
            }
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (StrataPawnUtility.IsMiscRobot(pawns[i]))
                {
                    count++;
                }
            }
            return count;
        }

        public static bool HibernateEnabled()
        {
            StrataSettings settings = StrataMod.Settings;
            if (settings == null)
            {
                return true;
            }
            return settings.throttleVacantLevels;
        }

        public static bool ReduceBackgroundLevelsEnabled()
        {
            StrataSettings settings = StrataMod.Settings;
            if (settings == null)
            {
                return true;
            }
            return settings.reduceBackgroundLevels;
        }

        public static bool IsForcedHibernate(Map map)
        {
            return map != null && forcedHibernateMapIds.Contains(map.uniqueID);
        }

        public static bool IsStrataPocketLevel(Map map)
        {
            return map != null
                && (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map));
        }

        // Vacant A+/B+ pocket maps run ambient sims at reduced rate (see LevelTicking).
        public static bool ShouldThrottleAmbient(Map map)
        {
            if (!HibernateEnabled() || map == null || !IsStrataPocketLevel(map))
            {
                return false;
            }
            if (Find.CurrentMap == map && !IsForcedHibernate(map))
            {
                return false;
            }
            if (ColonyPresenceCount(map) > 0)
            {
                ClearForcedHibernate(map);
                return false;
            }
            if (IsForcedHibernate(map))
            {
                return true;
            }
            return (Find.TickManager.TicksGame + map.uniqueID) % 4 != 0;
        }

        // Occupied pocket level you are not viewing — slower gas/O₂ sim, no overlay rebuild.
        public static bool ShouldReduceAtmosphere(Map map)
        {
            if (!ReduceBackgroundLevelsEnabled() || map == null || !IsStrataPocketLevel(map))
            {
                return false;
            }
            if (Find.CurrentMap == map && !IsForcedHibernate(map))
            {
                return false;
            }
            if (ShouldThrottleAmbient(map))
            {
                return false;
            }
            return ColonyPresenceCount(map) > 0;
        }

        public static int StrataPocketLevelCount()
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (cachedPocketLevelCount >= 0 && tick - cachedPocketLevelCountTick < 250)
            {
                return cachedPocketLevelCount;
            }

            int count = 0;
            IReadOnlyList<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (IsStrataPocketLevel(maps[i]))
                {
                    count++;
                }
            }

            cachedPocketLevelCount = count;
            cachedPocketLevelCountTick = tick;
            return count;
        }

        public static bool HasMultipleStrataPocketLevels()
        {
            return StrataPocketLevelCount() > 1;
        }

        public static int AtmosphereCycleMultiplier(Map map)
        {
            int multiplier = 1;
            if (ShouldThrottleAmbient(map))
            {
                multiplier = AtmosphereVacantMultiplier;
            }
            else if (ShouldReduceAtmosphere(map))
            {
                multiplier = AtmosphereReducedMultiplier;
            }
            if (StrataMod.Settings?.performanceModeEnabled == true)
            {
                multiplier *= AtmospherePerformanceMultiplier;
            }
            if (HasMultipleStrataPocketLevels()
                && map != null
                && map != Find.CurrentMap
                && IsStrataPocketLevel(map))
            {
                multiplier *= AtmosphereMultiLevelMultiplier;
            }
            return multiplier;
        }

        public static bool IsHibernating(Map map)
        {
            if (map == null || !IsStrataPocketLevel(map))
            {
                return false;
            }
            if (IsForcedHibernate(map))
            {
                return ColonyPresenceCount(map) == 0;
            }
            if (!HibernateEnabled())
            {
                return false;
            }
            if (Find.CurrentMap == map)
            {
                return false;
            }
            return ColonyPresenceCount(map) == 0;
        }

        public static void ForceHibernateAllEmptyLevels()
        {
            forcedHibernateMapIds.Clear();
            foreach (Map map in Find.Maps)
            {
                if (IsStrataPocketLevel(map) && ColonyPresenceCount(map) == 0)
                {
                    forcedHibernateMapIds.Add(map.uniqueID);
                }
            }
            Log.Message("[Strata] Forced hibernate on " + forcedHibernateMapIds.Count + " empty underground level(s).");
        }

        public static void ClearForcedHibernate(Map map)
        {
            if (map != null)
            {
                forcedHibernateMapIds.Remove(map.uniqueID);
            }
        }
    }
}
