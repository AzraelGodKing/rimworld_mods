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
            TerrainDef voidT = upper
                ? UpperDeckUtility.OpenSky
                : GravshipDeckUtility.VoidTerrain;
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
                // Re-check: the pad may have grown over this cell since the sweep,
                // or a pawn may be standing here (skip; a later sweep re-queues).
                TerrainDef terrain = cell.GetTerrain(map);
                bool managed = upper
                    ? terrain?.defName == UpperDeckUtility.RoofDeckDefName
                    : GravshipDeckUtility.IsManagedDeckTerrain(terrain);
                if (!managed || CellHasPawn(map, cell))
                {
                    continue;
                }
                Map host = (map.Parent as PocketMapParent)?.sourceMap;
                if (host != null && StrataGravshipUtility.CellOnGravship(host, cell))
                {
                    continue;
                }
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(map, cell);
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                tracker?.UnmarkProjected(cell);
                map.terrainGrid.SetTerrain(cell, voidT);
                map.roofGrid.SetRoof(cell, null);
                done++;
            }

            if (queue.cells.Count == 0)
            {
                queues.RemoveAt(0);
                Log.Message("[Strata] Deferred clear: map " + queue.mapId + " drained.");
            }
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
    }
}
