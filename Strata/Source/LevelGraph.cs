using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Treats portal-linked maps as a graph of base levels. Stateless: rebuilt on
    // demand from spawned portals, so there is nothing extra to save or load.
    public static class LevelGraph
    {
        // Reusable buffers; game logic is single-threaded.
        private static readonly Queue<Map> openQueue = new Queue<Map>();
        private static readonly List<LevelLink> resultBuffer = new List<LevelLink>();
        private static readonly HashSet<Map> reachesVisited = new HashSet<Map>();
        private static readonly Queue<Map> reachesQueue = new Queue<Map>();

        // ReachableLevels scratch (not reentrant-safe; do not call ReachableLevels
        // from inside a ReachableLevels iteration).
        private static readonly HashSet<Map> bfsVisited = new HashSet<Map>();
        private static readonly Dictionary<Map, MapPortal> bfsFirstSteps = new Dictionary<Map, MapPortal>();
        private static readonly Dictionary<Map, int> bfsDepths = new Dictionary<Map, int>();

        // Cached first-step portal lists per (from, target). Pawn position still
        // picks the nearest candidate at call time; topology is shared by all bots.
        private static int graphEpoch;
        private static readonly Dictionary<long, RouteCacheEntry> routeCache = new Dictionary<long, RouteCacheEntry>();

        // ReachableLevels is called from many relay paths in the same tick;
        // cache the BFS result per map until portal topology changes.
        private static Map cachedReachableFrom;
        private static int cachedReachableEpoch = -1;
        private static readonly List<LevelLink> reachableCache = new List<LevelLink>();

        private struct RouteCacheEntry
        {
            public int epoch;
            public int fromPortalHash;
            public List<MapPortal> portals;
        }

        public struct LevelLink
        {
            public Map map;          // a level reachable from the start map
            public MapPortal firstStep; // the portal to take first from the start map
            public int depth;        // number of stairwells between here and there
            public IntVec3 arrivalCell; // where a pawn lands on that level (last leg's exit)
        }

        internal static void InvalidateCache()
        {
            graphEpoch++;
            routeCache.Clear();
            cachedReachableFrom = null;
            cachedReachableEpoch = -1;
            reachableCache.Clear();
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
            if (cachedReachableFrom == from && cachedReachableEpoch == graphEpoch)
            {
                resultBuffer.AddRange(reachableCache);
                return resultBuffer;
            }

            bfsVisited.Clear();
            bfsVisited.Add(from);
            bfsFirstSteps.Clear();
            bfsDepths.Clear();
            bfsDepths[from] = 0;
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
                    if (other == null || bfsVisited.Contains(other))
                    {
                        continue;
                    }
                    bfsVisited.Add(other);
                    MapPortal firstStep = current == from ? portal : bfsFirstSteps[current];
                    bfsFirstSteps[other] = firstStep;
                    bfsDepths[other] = bfsDepths[current] + 1;
                    resultBuffer.Add(new LevelLink
                    {
                        map = other,
                        firstStep = firstStep,
                        depth = bfsDepths[other],
                        arrivalCell = portal.GetDestinationLocation(),
                    });
                    openQueue.Enqueue(other);
                }
            }
            reachableCache.Clear();
            reachableCache.AddRange(resultBuffer);
            cachedReachableFrom = from;
            cachedReachableEpoch = graphEpoch;
            return resultBuffer;
        }

        // The best portal on 'from' for a pawn heading toward 'target': among
        // first steps whose subtree actually reaches the target, prefer the
        // shortest walk for that pawn, with a bonus for powered elevators so
        // haulers ride instead of taking the long stairs.
        // When 'pawn' is set, only portals that pawn can reach are returned
        // (sealed-off A1 stairs behind a closed room are skipped).
        public static MapPortal BestFirstStep(Map from, Map target, IntVec3 pawnPos, Pawn pawn = null)
        {
            if (from == null || target == null)
            {
                return null;
            }
            List<MapPortal> candidates = GetRouteCandidates(from, target);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }
            MapPortal best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                MapPortal portal = candidates[i];
                if (portal == null || !portal.Spawned || portal.Map != from)
                {
                    continue;
                }
                if (pawn != null && !pawn.CanReach(portal, PathEndMode.Touch, Danger.Deadly))
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

        // All first-step portals toward target (reachable or not). Used when a
        // sealed room blocks the A1 stair and we need a B1 detour to reach it.
        public static List<MapPortal> FirstStepCandidates(Map from, Map target)
        {
            return GetRouteCandidates(from, target) ?? new List<MapPortal>();
        }

        private static List<MapPortal> GetRouteCandidates(Map from, Map target)
        {
            long key = RouteKey(from, target);
            int portalHash = PortalTopologyHash(from);
            if (routeCache.TryGetValue(key, out RouteCacheEntry entry)
                && entry.epoch == graphEpoch
                && entry.fromPortalHash == portalHash
                && entry.portals != null)
            {
                return entry.portals;
            }

            var candidates = new List<MapPortal>();
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
                candidates.Add(portal);
            }

            routeCache[key] = new RouteCacheEntry
            {
                epoch = graphEpoch,
                fromPortalHash = portalHash,
                portals = candidates,
            };
            return candidates;
        }

        private static long RouteKey(Map from, Map target)
        {
            return ((long)from.uniqueID << 32) | (uint)target.uniqueID;
        }

        private static int PortalTopologyHash(Map map)
        {
            int hash = map.uniqueID;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && portal.Spawned)
                {
                    hash = Gen.HashCombineInt(hash, portal.thingIDNumber);
                }
            }
            return hash;
        }

        private static bool IsPoweredElevator(MapPortal portal)
        {
            // Return landings are always free (nobody gets trapped).
            if (portal is Building_ElevatorUp || portal is Building_ElevatorBuildUpLanding)
            {
                return true;
            }
            // Powered checks the grid's real energy state; the shaft transmitter's
            // PowerOn is forced true whenever wired.
            if (portal is Building_ElevatorDown elevator && elevator.Powered)
            {
                return true;
            }
            return portal is Building_ElevatorBuildUp tower && tower.Powered;
        }

        // Whether 'target' is reachable from 'start' without passing back
        // through 'exclude'. Small graphs; plain BFS.
        private static bool Reaches(Map start, Map target, Map exclude)
        {
            if (start == target)
            {
                return true;
            }
            reachesVisited.Clear();
            reachesVisited.Add(start);
            reachesVisited.Add(exclude);
            reachesQueue.Clear();
            reachesQueue.Enqueue(start);
            while (reachesQueue.Count > 0)
            {
                Map current = reachesQueue.Dequeue();
                foreach (Thing thing in current.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    Map other = thing is MapPortal portal ? OtherMapSafe(portal) : null;
                    if (other == null || !reachesVisited.Add(other))
                    {
                        continue;
                    }
                    if (other == target)
                    {
                        return true;
                    }
                    reachesQueue.Enqueue(other);
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

            if (portal.PocketMapExists)
            {
                Map pocket = portal.PocketMap;
                if (pocket != null && Find.Maps.Contains(pocket))
                {
                    return pocket;
                }
            }

            // Fallback when pocket bookkeeping lags but the landing still exists
            // (common on A+ towers after join-to-existing-upper-level).
            if (portal.exit != null && portal.exit.Spawned && portal.exit.Map != null
                && Find.Maps.Contains(portal.exit.Map))
            {
                return portal.exit.Map;
            }

            return null;
        }
    }
}
