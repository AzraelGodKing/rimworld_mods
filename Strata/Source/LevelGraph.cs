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

        // The best portal on 'from' for a pawn heading toward 'target': among
        // first steps whose subtree actually reaches the target, prefer the
        // shortest walk for that pawn, with a bonus for powered elevators so
        // haulers ride instead of taking the long stairs.
        public static MapPortal BestFirstStep(Map from, Map target, IntVec3 pawnPos)
        {
            if (from == null || target == null)
            {
                return null;
            }
            MapPortal best = null;
            float bestScore = float.MaxValue;
            foreach (Thing thing in from.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (!(thing is MapPortal portal) || !portal.Spawned)
                {
                    continue;
                }
                Map other = OtherMapSafe(portal);
                if (other == null || (other != target && !Reaches(other, target, from)))
                {
                    continue;
                }
                float score = pawnPos.IsValid ? pawnPos.DistanceTo(portal.Position) : 1f;
                if (IsPoweredElevator(portal))
                {
                    score *= 0.6f;
                }
                if (score < bestScore)
                {
                    bestScore = score;
                    best = portal;
                }
            }
            return best;
        }

        private static bool IsPoweredElevator(MapPortal portal)
        {
            if (portal is Building_ElevatorUp)
            {
                return true; // riding up is always available
            }
            return portal is Building_ElevatorDown
                && portal.TryGetComp<CompPowerTrader>()?.PowerOn == true;
        }

        // Whether 'target' is reachable from 'start' without passing back
        // through 'exclude'. Small graphs; plain BFS.
        private static bool Reaches(Map start, Map target, Map exclude)
        {
            if (start == target)
            {
                return true;
            }
            var visited = new HashSet<Map> { start, exclude };
            var queue = new Queue<Map>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Map current = queue.Dequeue();
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    Map other = thing is MapPortal portal ? OtherMapSafe(portal) : null;
                    if (other == null || !visited.Add(other))
                    {
                        continue;
                    }
                    if (other == target)
                    {
                        return true;
                    }
                    queue.Enqueue(other);
                }
            }
            return false;
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
