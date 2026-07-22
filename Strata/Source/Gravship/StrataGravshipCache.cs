using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Perf: gravship stack lookups (engine-on-map, portal host, stack root) were
    // recomputed with all-maps × all-portals scans on every call. Vanilla calls
    // IsOnboardGravship / ValidSubstructureAt per cell in region sweeps during
    // launch (JobGiver_BoardOrLeaveGravship, substructure overlay, mask regen),
    // which multiplied those scans into a hard stall from "Launch" until takeoff.
    // Short-TTL cache + explicit invalidation on any stack topology change.
    public static class StrataGravshipCache
    {
        private const int TtlTicks = 60;

        private struct EngineEntry
        {
            public int tick;
            public Building_GravEngine engine;
        }

        private struct StackEntry
        {
            public int tick;
            // -1 = none.
            public int portalHostMapId;
            public int stackRootMapId;
        }

        private static readonly Dictionary<int, EngineEntry> engineByMap =
            new Dictionary<int, EngineEntry>();

        private static readonly Dictionary<int, StackEntry> stackByMap =
            new Dictionary<int, StackEntry>();

        // Find.World.GetComponent / Map.GetComponent are linear scans — too slow
        // for per-cell callers.
        private static RimWorld.Planet.World stacksWorld;
        private static WorldComponent_StrataGravshipStacks stacksComp;

        private static readonly Dictionary<int, MapComponent_StrataProjectedSubstructure> trackerByMap =
            new Dictionary<int, MapComponent_StrataProjectedSubstructure>();

        public static void Invalidate()
        {
            engineByMap.Clear();
            stackByMap.Clear();
            trackerByMap.Clear();
        }

        public static WorldComponent_StrataGravshipStacks StacksComp
        {
            get
            {
                RimWorld.Planet.World world = Find.World;
                if (world == null)
                {
                    return null;
                }
                if (!ReferenceEquals(world, stacksWorld) || stacksComp == null)
                {
                    stacksWorld = world;
                    stacksComp = world.GetComponent<WorldComponent_StrataGravshipStacks>();
                }
                return stacksComp;
            }
        }

        public static MapComponent_StrataProjectedSubstructure TrackerOf(Map map)
        {
            if (map == null)
            {
                return null;
            }
            int id = map.uniqueID;
            // Negative results cached too (most maps have no tracker); adding a
            // tracker invalidates via GetOrAddTracker.
            if (trackerByMap.TryGetValue(id, out var tracker)
                && (tracker == null || tracker.map == map))
            {
                return tracker;
            }
            tracker = map.GetComponent<MapComponent_StrataProjectedSubstructure>();
            trackerByMap[id] = tracker;
            return tracker;
        }

        private static int Now => Find.TickManager?.TicksGame ?? 0;

        private static bool Fresh(int tick) => Now - tick <= TtlTicks;

        public static Building_GravEngine EngineOnMap(Map map)
        {
            if (map == null)
            {
                return null;
            }
            int id = map.uniqueID;
            if (engineByMap.TryGetValue(id, out EngineEntry entry) && Fresh(entry.tick))
            {
                Building_GravEngine cached = entry.engine;
                if (cached == null)
                {
                    return null;
                }
                if (cached.Spawned && !cached.Destroyed && cached.Map == map)
                {
                    return cached;
                }
            }
            Building_GravEngine engine = ComputeEngineOnMap(map);
            engineByMap[id] = new EngineEntry { tick = Now, engine = engine };
            return engine;
        }

        private static Building_GravEngine ComputeEngineOnMap(Map map)
        {
            if (map?.listerBuildings == null)
            {
                return null;
            }
            if (StrataVgeCompat.Active)
            {
                return StrataVgeCompat.PreferBestEngine(
                    map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>());
            }
            foreach (Building_GravEngine engine
                in map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>())
            {
                if (engine != null && engine.Spawned)
                {
                    return engine;
                }
            }
            return null;
        }

        public static Map PortalHostOf(Map level)
        {
            if (level == null)
            {
                return null;
            }
            StackEntry entry = GetStackEntry(level);
            return FindMapById(entry.portalHostMapId);
        }

        public static Map StackRootOf(Map map)
        {
            if (map == null)
            {
                return null;
            }
            StackEntry entry = GetStackEntry(map);
            return FindMapById(entry.stackRootMapId);
        }

        private static StackEntry GetStackEntry(Map map)
        {
            int id = map.uniqueID;
            if (stackByMap.TryGetValue(id, out StackEntry entry) && Fresh(entry.tick))
            {
                return entry;
            }
            Map portalHost = StrataGravshipUtility.ComputeDirectGravshipPortalHost(map);
            Map root = StrataGravshipUtility.ComputeGravshipStackRoot(map, portalHost);
            entry = new StackEntry
            {
                tick = Now,
                portalHostMapId = portalHost?.uniqueID ?? -1,
                stackRootMapId = root?.uniqueID ?? -1,
            };
            stackByMap[id] = entry;
            return entry;
        }

        private static Map FindMapById(int uniqueId)
        {
            return StrataGravshipOrphanLevels.FindMapById(uniqueId);
        }
    }
}
