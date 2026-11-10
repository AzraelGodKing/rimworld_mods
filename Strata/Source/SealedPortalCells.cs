using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // GasGrid.GasCanMoveTo runs for every cell gas tries to spread into, so the
    // sealed-portal check must not walk the cell's things. Each map keeps the
    // cells covered by sealed Strata portals, rebuilt only when portal topology
    // (LevelGraph.Epoch) or any seal (SealVersion) changes.
    public static class SealedPortalCells
    {
        private sealed class Entry
        {
            public int epoch = -1;
            public int sealVersion = -1;
            public readonly HashSet<int> cells = new HashSet<int>();
        }

        private static readonly Dictionary<int, Entry> byMapId = new Dictionary<int, Entry>();
        private static int sealVersion;

        // A hatch was sealed or opened somewhere. A landing's seal is read from
        // its entrance on another map, so one global version covers both ends.
        public static void Notify_SealChanged()
        {
            sealVersion++;
        }

        public static void ResetSession()
        {
            byMapId.Clear();
            sealVersion = 0;
        }

        public static bool Contains(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }
            if (!byMapId.TryGetValue(map.uniqueID, out Entry entry))
            {
                entry = new Entry();
                byMapId[map.uniqueID] = entry;
            }
            if (entry.epoch != LevelGraph.Epoch || entry.sealVersion != sealVersion)
            {
                Rebuild(map, entry);
            }
            return entry.cells.Count > 0 && entry.cells.Contains(map.cellIndices.CellToIndex(cell));
        }

        private static void Rebuild(Map map, Entry entry)
        {
            entry.epoch = LevelGraph.Epoch;
            entry.sealVersion = sealVersion;
            entry.cells.Clear();
            List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
            for (int i = 0; i < portals.Count; i++)
            {
                Thing portal = portals[i];
                if (!IsStrataPortalDef(portal.def) || !StrataPortalUtility.IsSealedPortal(portal))
                {
                    continue;
                }
                foreach (IntVec3 c in portal.OccupiedRect())
                {
                    if (c.InBounds(map))
                    {
                        entry.cells.Add(map.cellIndices.CellToIndex(c));
                    }
                }
            }
        }

        private static bool IsStrataPortalDef(ThingDef def)
        {
            string name = def?.defName;
            return name != null
                && (name.StartsWith("Strata_Stairs", StringComparison.Ordinal)
                    || name.StartsWith("Strata_Elevator", StringComparison.Ordinal)
                    || name.StartsWith("Strata_Gravship", StringComparison.Ordinal));
        }
    }
}
