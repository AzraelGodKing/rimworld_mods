using System.Collections.Generic;
using Verse;

namespace Strata
{
    // Dig GetOtherMap must return quickly: GenSpawn of ~75k rocks on a 275² map
    // freezes Unity for minutes. GenSteps schedule fill here; StrataPocketMapOpen
    // drains it in a yielding LongEvent after the pocket exists.
    internal static class StrataRockFillScheduler
    {
        private const int CellsPerFrame = 2000;

        private struct Pending
        {
            public int mapId;
            public BoolGrid skipCarveMask;
        }

        private static Pending? pending;

        public static void Schedule(Map map, BoolGrid skipCarveMask = null)
        {
            if (map == null)
            {
                return;
            }
            pending = new Pending
            {
                mapId = map.uniqueID,
                skipCarveMask = skipCarveMask,
            };
        }

        public static bool TryBegin(Map map, out BoolGrid skipCarveMask)
        {
            skipCarveMask = null;
            if (pending == null || map == null || pending.Value.mapId != map.uniqueID)
            {
                return false;
            }
            skipCarveMask = pending.Value.skipCarveMask;
            pending = null;
            return true;
        }

        public static void Clear()
        {
            pending = null;
        }

        // Yields periodically so LongEventHandler can pump frames / UI.
        // B1: rock indices precomputed on workers before the GenSpawn loop.
        public static IEnumerable<object> SpawnMineablesChunked(Map map, BoolGrid skipCarveMask)
        {
            if (map == null)
            {
                yield break;
            }

            List<ThingDef> rocks = StrataRockUtility.RocksForMap(map);
            byte[] rockIndices = StrataRockPlan.BuildRockIndices(map, rocks);
            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            int sinceYield = 0;
            int width = map.Size.x;
            try
            {
                foreach (IntVec3 cell in map.AllCells)
                {
                    if (skipCarveMask != null && skipCarveMask[cell])
                    {
                        continue;
                    }
                    if (cell.GetEdifice(map) != null)
                    {
                        continue;
                    }
                    int index = cell.z * width + cell.x;
                    ThingDef rockDef = StrataRockPlan.RockAtIndex(
                        rocks,
                        index >= 0 && index < rockIndices.Length ? rockIndices[index] : (byte)0);
                    Thing rock = ThingMaker.MakeThing(rockDef);
                    GenSpawn.Spawn(rock, cell, map, Rot4.North, WipeMode.Vanish,
                        respawningAfterLoad: true);
                    sinceYield++;
                    if (sinceYield >= CellsPerFrame)
                    {
                        sinceYield = 0;
                        yield return null;
                    }
                }
            }
            finally
            {
                map.regionAndRoomUpdater.Enabled = regionsWereEnabled;
            }
        }
    }
}
