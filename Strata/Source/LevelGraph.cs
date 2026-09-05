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

        // ReachableLevels BFS scratch. The public API returns a fresh list so
        // nested callers (e.g. LevelDemand.Build → AddStorageUpgradePulls while
        // HaulAcrossLevels is still walking links) cannot clear a live enumerator.
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

        // AnyLinkFrom is hit from many Harmony postfixes; cache per map until
        // InvalidateCache (portal spawn/despawn) bumps graphEpoch.
        private static int anyLinkEpoch = -1;
        private static readonly Dictionary<int, bool> anyLinkByMapId = new Dictionary<int, bool>();

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
            anyLinkByMapId.Clear();
            anyLinkEpoch = graphEpoch;
        }

        // Only Strata shafts count as level links by default. Foreign portals
        // (Anomaly undercaves, Deep And Deeper caves, other pocket-map mods)
        // would otherwise pull relays/alerts into maps that are not base floors
        // — opt back in via mod settings for cross-mod relay coverage.
        public static bool IsLevelPortal(MapPortal portal)
        {
            if (portal == null)
            {
                return false;
            }
            if (CompStrataShaftLinkInjector.IsStrataPortalDef(portal.def))
            {
                return true;
            }
            return StrataMod.Settings != null && StrataMod.Settings.foreignPortalLevelsEnabled;
        }

        public static bool AnyLinkFrom(Map map)
        {
            if (map == null)
            {
                return false;
            }
            if (anyLinkEpoch != graphEpoch)
            {
                anyLinkByMapId.Clear();
                anyLinkEpoch = graphEpoch;
            }
            if (anyLinkByMapId.TryGetValue(map.uniqueID, out bool cached))
            {
                return cached;
            }
            bool result = ComputeAnyLinkFrom(map);
            anyLinkByMapId[map.uniqueID] = result;
            return result;
        }

        private static bool ComputeAnyLinkFrom(Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && IsLevelPortal(portal) && OtherMapSafe(portal) != null)
                {
                    return true;
                }
            }
            return false;
        }

        // All levels reachable from 'from', nearest first, each paired with the
        // first portal to walk into. Returns a new list each call (safe to nest
        // or store for the current tick); topology is cached until InvalidateCache.
        public static List<LevelLink> ReachableLevels(Map from)
        {
            if (from == null)
            {
                return new List<LevelLink>();
            }
            if (cachedReachableFrom == from && cachedReachableEpoch == graphEpoch)
            {
                return new List<LevelLink>(reachableCache);
            }

            resultBuffer.Clear();
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
                    if (!(thing is MapPortal portal) || !IsLevelPortal(portal))
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
            return new List<LevelLink>(resultBuffer);
        }

        // The best portal on 'from' for a pawn heading toward 'target': among
        // first steps whose subtree actually reaches the target, prefer the
        // shortest walk for that pawn, with a bonus for powered elevators so
        // haulers ride instead of taking the long stairs.
        // When 'pawn' is set, only portals that pawn can reach are returned
        // (sealed-off A1 stairs behind a closed room are skipped).
        // When preferArrivalNear is set on 'target', prefer stairs whose landing
        // can actually walk to that cell — otherwise a nearer entrance can dump
        // the pawn into a disconnected dig (empty "new" shaft vs the real room).
        // Falls back to any enterable shaft if none can reach the preferred cell.
        public static MapPortal BestFirstStep(
            Map from,
            Map target,
            IntVec3 pawnPos,
            Pawn pawn = null,
            IntVec3 preferArrivalNear = default)
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

            bool prefer = preferArrivalNear.IsValid && preferArrivalNear.InBounds(target);
            if (prefer)
            {
                MapPortal reachableLanding = PickBestFirstStep(
                    candidates, from, target, pawnPos, pawn, preferArrivalNear, requireArrivalReach: true);
                if (reachableLanding != null)
                {
                    return reachableLanding;
                }
            }

            return PickBestFirstStep(
                candidates, from, target, pawnPos, pawn, preferArrivalNear, requireArrivalReach: false);
        }

        // Like BestFirstStep, but never falls back to a shaft whose landing cannot
        // walk to preferArrivalNear (haul / force-build must not pick open ancient
        // stairs into a sealed rock bubble while the real stockpile is elsewhere).
        public static MapPortal BestFirstStepRequiringArrival(
            Map from,
            Map target,
            IntVec3 pawnPos,
            Pawn pawn,
            IntVec3 preferArrivalNear)
        {
            if (from == null || target == null
                || !preferArrivalNear.IsValid || !preferArrivalNear.InBounds(target))
            {
                return null;
            }
            List<MapPortal> candidates = GetRouteCandidates(from, target);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }
            return PickBestFirstStep(
                candidates, from, target, pawnPos, pawn, preferArrivalNear, requireArrivalReach: true);
        }

        private static MapPortal PickBestFirstStep(
            List<MapPortal> candidates,
            Map from,
            Map target,
            IntVec3 pawnPos,
            Pawn pawn,
            IntVec3 preferArrivalNear,
            bool requireArrivalReach)
        {
            bool prefer = preferArrivalNear.IsValid && preferArrivalNear.InBounds(target);
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

                CompElevatorControls controls = CompElevatorControls.On(portal);
                if (controls != null && controls.HoldAtLevel)
                {
                    Map heldOther = OtherMapSafe(portal);
                    if (heldOther != target)
                    {
                        continue;
                    }
                }

                float walk = pawnPos.IsValid ? pawnPos.DistanceTo(portal.Position) : 1f;
                if (IsPoweredElevator(portal))
                {
                    walk *= 0.6f;
                }
                if (controls != null)
                {
                    walk *= 1f - 0.08f * controls.FloorPriority;
                }

                float score = walk;
                Map other = OtherMapSafe(portal);
                if (prefer && other == target)
                {
                    IntVec3 arrival = portal.GetDestinationLocation();
                    if (!arrival.IsValid || !arrival.InBounds(target))
                    {
                        continue;
                    }

                    bool landsWithPath = target.reachability.CanReach(
                        arrival,
                        preferArrivalNear,
                        PathEndMode.OnCell,
                        TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, canBashDoors: false));
                    if (requireArrivalReach && !landsWithPath)
                    {
                        continue;
                    }

                    // Landing next to the real job site beats a short walk to the wrong shaft.
                    score = preferArrivalNear.DistanceTo(arrival) + walk * 0.25f;
                    if (!landsWithPath)
                    {
                        score += 10000f;
                    }
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
                if (!(thing is MapPortal portal) || !portal.Spawned || !IsLevelPortal(portal))
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
                if (thing is MapPortal portal && portal.Spawned && IsLevelPortal(portal))
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
                    Map other = thing is MapPortal portal && IsLevelPortal(portal)
                        ? OtherMapSafe(portal) : null;
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
