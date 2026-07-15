using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Tracks per-level hibernation for the Levels tab and dev tools.
    public static class StrataLevelPerfUtility
    {
        private static readonly HashSet<int> forcedHibernateMapIds = new HashSet<int>();

        public static int PawnCount(Map map)
        {
            return map?.mapPawns.AllPawnsSpawned.Count ?? 0;
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

        public static bool IsForcedHibernate(Map map)
        {
            return map != null && forcedHibernateMapIds.Contains(map.uniqueID);
        }

        public static bool IsHibernating(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return false;
            }
            if (IsForcedHibernate(map))
            {
                return PawnCount(map) == 0;
            }
            if (!HibernateEnabled())
            {
                return false;
            }
            if (Find.CurrentMap == map)
            {
                return false;
            }
            return PawnCount(map) == 0;
        }

        public static void ForceHibernateAllEmptyLevels()
        {
            forcedHibernateMapIds.Clear();
            foreach (Map map in Find.Maps)
            {
                if (StrataMapUtility.IsUnderground(map) && PawnCount(map) == 0)
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
