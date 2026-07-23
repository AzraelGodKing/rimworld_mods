using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Gentle ghost-pad drain: clearing thousands of managed deck cells in one
    // tick mass-dirties map sections and RGB-corrupts the view (worse with
    // -disable-compute-shaders). Cells are enqueued once and cleared a small
    // row-major slice per tick from WorldComponent_StrataGravshipStacks, so the
    // section regen budget keeps up while multi-flight ghost pads still drain.
    public static class StrataDeferredCellClear
    {
        private const int CellsPerTick = 128;

        private class MapQueue
        {
            public int mapId;
            public Queue<IntVec3> cells = new Queue<IntVec3>();
            // Cells already enqueued — avoids duplicates on repeated sweeps.
            public HashSet<IntVec3> queued = new HashSet<IntVec3>();
        }

        private static readonly List<MapQueue> queues = new List<MapQueue>();

        public static bool HasWork => queues.Count > 0;

        public static void Enqueue(Map map, List<IntVec3> cells)
        {
            if (map == null || cells == null || cells.Count == 0)
            {
                return;
            }
            MapQueue queue = null;
            for (int i = 0; i < queues.Count; i++)
            {
                if (queues[i].mapId == map.uniqueID)
                {
                    queue = queues[i];
                    break;
                }
            }
            if (queue == null)
            {
                queue = new MapQueue { mapId = map.uniqueID };
                queues.Add(queue);
            }
            int added = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (queue.queued.Add(cells[i]))
                {
                    queue.cells.Enqueue(cells[i]);
                    added++;
                }
            }
            if (added > 0)
            {
                Log.Message("[Strata] Deferred clear: queued " + added
                    + " off-pad cell(s) on map " + map.uniqueID
                    + " (" + queue.cells.Count + " pending).");
            }
        }

        // One small slice per tick, one map at a time.
        public static void DrainTick()
        {
            if (queues.Count == 0)
            {
                return;
            }
            MapQueue queue = queues[0];
            Map map = StrataGravshipOrphanLevels.FindMapById(queue.mapId);
            if (map == null || queue.cells.Count == 0)
            {
                queues.RemoveAt(0);
                return;
            }

            bool upper = StrataMapUtility.IsUpperLevel(map);
            var tracker = map.GetComponent<MapComponent_StrataProjectedSubstructure>();

            int done = 0;
            while (done < CellsPerTick && queue.cells.Count > 0)
            {
                IntVec3 cell = queue.cells.Dequeue();
                queue.queued.Remove(cell);
                if (!cell.InBounds(map))
                {
                    continue;
                }
                // Re-check: the pad/roof support may have returned since the
                // sweep, or something may occupy the cell (skip; re-swept later).
                TerrainDef terrain = cell.GetTerrain(map);
                bool ghostDeck = upper
                    ? terrain?.defName == UpperDeckUtility.RoofDeckDefName
                    : terrain?.defName == GravshipDeckUtility.DeckDefName;
                bool ghostHull = !upper
                    && terrain?.defName == GravshipDeckUtility.HullDefName;
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(map, cell);
                if ((!ghostDeck && !ghostHull && sub == null) || CellHasPawn(map, cell))
                {
                    continue;
                }
                if (upper)
                {
                    if (UpperDeckUtility.SourceSupportsUpperDeck(map, cell))
                    {
                        continue;
                    }
                }
                else
                {
                    Map host = (map.Parent as PocketMapParent)?.sourceMap;
                    if (host != null)
                    {
                        if (StrataGravshipUtility.CellOnGravship(host, cell))
                        {
                            continue;
                        }
                        // Preserve the live one-cell hull rim around the pad.
                        if (ghostHull && CellTouchesHostPad(host, cell))
                        {
                            continue;
                        }
                    }
                }
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                tracker?.UnmarkProjected(cell);
                // Terrain/roof revert on managed ghost deck/hull — a cell that
                // only carried a stale substructure thing keeps its floor.
                if (ghostDeck || ghostHull)
                {
                    map.terrainGrid.SetTerrain(
                        cell,
                        ghostHull ? GravshipDeckUtility.VoidTerrain : ReplacementFor(map, cell, upper));
                    map.roofGrid.SetRoof(cell, null);
                }
                done++;
            }

            if (queue.cells.Count == 0)
            {
                queues.RemoveAt(0);
                Log.Message("[Strata] Deferred clear: map " + queue.mapId + " drained.");
            }
        }

        // Ghost deck inside a hull field becomes hull (seamless); in open void it
        // becomes void. Upper decks always revert to open sky.
        internal static TerrainDef ReplacementFor(Map map, IntVec3 cell, bool upper)
        {
            if (upper)
            {
                return UpperDeckUtility.OpenSky;
            }
            for (int i = 0; i < 4; i++)
            {
                IntVec3 adj = cell + GenAdj.CardinalDirections[i];
                if (adj.InBounds(map)
                    && adj.GetTerrain(map)?.defName == GravshipDeckUtility.HullDefName)
                {
                    return GravshipDeckUtility.HullTerrain;
                }
            }
            return GravshipDeckUtility.VoidTerrain;
        }

        private static bool CellHasPawn(Map map, IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CellTouchesHostPad(Map host, IntVec3 cell)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 adj = cell + GenAdj.CardinalDirections[i];
                if (adj.InBounds(host) && StrataGravshipUtility.CellOnGravship(host, adj))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
