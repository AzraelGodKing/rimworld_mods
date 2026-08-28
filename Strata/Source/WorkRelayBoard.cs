using System.Collections.Generic;
using Verse;

namespace Strata
{
    /// <summary>
    /// Published per-map work signals for work relay. Filled by the board scanner
    /// (or sync fallback); pawn think only reads.
    /// </summary>
    public static class WorkRelayBoard
    {
        public sealed class BoardEntry
        {
            public int mapId;
            public int workVersion;
            public int builtTick;
            public bool construction;
            public bool mining;
            public bool plantCutting;
            public bool growing;
            public bool hunting;
            public bool hauling;
            public bool haulExports;
            public bool cleaning;
            public bool firefighting;
            public bool research;
            public bool flick;
            public bool warden;
            public bool handling;
            public bool childcare;
            public int blueprintFrames;
            public int haulableCount;
        }

        private static readonly Dictionary<int, BoardEntry> entries = new Dictionary<int, BoardEntry>();

        private static readonly HashSet<int> dirtyMaps = new HashSet<int>();

        [StrataSessionReset]
        internal static void ResetSession()
        {
            entries.Clear();
            dirtyMaps.Clear();
        }

        public static void Invalidate(Map map)
        {
            if (map == null)
            {
                return;
            }
            int id = map.uniqueID;
            entries.Remove(id);
            dirtyMaps.Add(id);
        }

        public static void InvalidateAll()
        {
            entries.Clear();
            dirtyMaps.Clear();
        }

        public static bool IsDirty(Map map)
        {
            return map != null && dirtyMaps.Contains(map.uniqueID);
        }

        public static void ClearDirty(Map map)
        {
            if (map != null)
            {
                dirtyMaps.Remove(map.uniqueID);
            }
        }

        public static void Publish(BoardEntry entry)
        {
            if (entry == null)
            {
                return;
            }
            entries[entry.mapId] = entry;
            dirtyMaps.Remove(entry.mapId);
        }

        public static bool TryGet(Map map, out BoardEntry entry)
        {
            entry = null;
            if (map == null)
            {
                return false;
            }
            return entries.TryGetValue(map.uniqueID, out entry) && entry != null;
        }

        public static bool TryGetFresh(Map map, int maxAgeTicks, out BoardEntry entry)
        {
            entry = null;
            if (!TryGet(map, out entry))
            {
                return false;
            }
            if (entry.workVersion != WorkRelaySignals.WorkVersion)
            {
                return false;
            }
            if (Find.TickManager.TicksGame - entry.builtTick > maxAgeTicks)
            {
                return false;
            }
            return true;
        }
    }
}
