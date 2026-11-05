using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Walking cost between a portal's landing and another portal on the same
    // floor, from the real pathfinder: walls, doors and slow terrain count, and
    // a portal that cannot be walked to is not used at all. Measured in cells
    // so it adds up with LevelPathSearch's ride costs. Cached per portal pair
    // and refreshed every RefreshTicks, because walls go up without the portal
    // topology changing.
    internal static class LevelWalkCost
    {
        public const int RefreshTicks = 2500;

        // Vanilla path cost of one cardinal step on open ground.
        private const float CostPerCell = 13f;

        private struct Entry
        {
            public float cost;
            public int tick;
        }

        private static readonly Dictionary<long, Entry> cache = new Dictionary<long, Entry>();

        internal static int Now => Find.TickManager?.TicksGame ?? 0;

        public static float Between(MapPortal from, MapPortal to)
        {
            if (from == null || to?.Map == null)
            {
                return float.PositiveInfinity;
            }
            long key = ((long)from.thingIDNumber << 32) | (uint)to.thingIDNumber;
            int now = Now;
            if (cache.TryGetValue(key, out Entry hit) && now - hit.tick < RefreshTicks)
            {
                return hit.cost;
            }
            float cost = Measure(from, to);
            cache[key] = new Entry { cost = cost, tick = now };
            return cost;
        }

        private static float Measure(MapPortal from, MapPortal to)
        {
            Map map = to.Map;
            IntVec3 start = from.GetDestinationLocation();
            if (!start.IsValid || !start.InBounds(map))
            {
                start = from.Position;
            }
            float straight = start.DistanceTo(to.Position);
            // Before the game is running (loading, map generation) the
            // pathfinder is not ready: fall back to a straight line.
            if (Current.ProgramState != ProgramState.Playing || map.pathFinder == null || map.reachability == null)
            {
                return straight;
            }
            TraverseParms parms = TraverseParms.For(TraverseMode.PassDoors);
            if (!map.reachability.CanReach(start, to, PathEndMode.Touch, parms))
            {
                return float.PositiveInfinity;
            }
            using (PawnPath path = map.pathFinder.FindPathNow(start, to, parms, null, PathEndMode.Touch))
            {
                return path != null && path.Found ? path.TotalCost / CostPerCell : straight;
            }
        }

        internal static void Clear()
        {
            cache.Clear();
        }

        public static void ResetSession()
        {
            cache.Clear();
        }
    }
}
