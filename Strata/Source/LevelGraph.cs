using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Treats portal-linked maps as a graph of base levels. Stateless: rebuilt on
    // demand from spawned portals, so there is nothing extra to save or load.
    public static class LevelGraph
    {
        // Reusable buffers; game logic is single-threaded.
        private static readonly Queue<Map> openQueue = new Queue<Map>();
        private static readonly List<LevelLink> resultBuffer = new List<LevelLink>();

        public struct LevelLink
        {
            public Map map;          // a level reachable from the start map
            public MapPortal firstStep; // the portal to take first from the start map
            public int depth;        // number of stairwells between here and there
            public IntVec3 arrivalCell; // where a pawn lands on that level (last leg's exit)
        }

        public static bool AnyLinkFrom(Map map)
        {
            if (map == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && OtherMapSafe(portal) != null)
                {
                    return true;
                }
            }
            return false;
        }

        // All levels reachable from 'from', nearest first, each paired with the
        // first portal to walk into. The buffer is reused; do not store it.
        public static List<LevelLink> ReachableLevels(Map from)
        {
            resultBuffer.Clear();
            if (from == null)
            {
                return resultBuffer;
            }

            var visited = new HashSet<Map> { from };
            var firstSteps = new Dictionary<Map, MapPortal>();
            var depths = new Dictionary<Map, int> { { from, 0 } };
            openQueue.Clear();
            openQueue.Enqueue(from);

            while (openQueue.Count > 0)
            {
                Map current = openQueue.Dequeue();
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (!(thing is MapPortal portal))
                    {
                        continue;
                    }
                    Map other = OtherMapSafe(portal);
                    if (other == null || visited.Contains(other))
                    {
                        continue;
                    }
                    visited.Add(other);
                    MapPortal firstStep = current == from ? portal : firstSteps[current];
                    firstSteps[other] = firstStep;
                    depths[other] = depths[current] + 1;
                    resultBuffer.Add(new LevelLink
                    {
                        map = other,
                        firstStep = firstStep,
                        depth = depths[other],
                        arrivalCell = portal.GetDestinationLocation(),
                    });
                    openQueue.Enqueue(other);
                }
            }
            return resultBuffer;
        }

        // The linked map on the far side of a portal, without ever triggering
        // pocket map generation.
        public static Map OtherMapSafe(MapPortal portal)
        {
            if (portal == null || !portal.Spawned)
            {
                return null;
            }
            if (portal is PocketMapExit exit)
            {
                Map other = exit.GetOtherMap();
                return other != null && Find.Maps.Contains(other) ? other : null;
            }
            return portal.PocketMapExists ? portal.PocketMap : null;
        }
    }
}
