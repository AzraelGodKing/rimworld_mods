using System.Collections.Generic;
using Verse;

namespace Strata
{
    public static class GasPipeUtility
    {
        public const float PipeInteriorFlow = 0.3f;
        public const float PipeOutdoorDrain = 0.50f;

        // Networks per map, rebuilt only when a gas pipe spawns or despawns.
        private sealed class NetworkCache
        {
            public int version = -1;
            public readonly List<HashSet<IntVec3>> networks = new List<HashSet<IntVec3>>();
        }

        private static readonly Dictionary<int, int> pipeVersionByMap = new Dictionary<int, int>();
        private static readonly Dictionary<int, NetworkCache> networksByMap = new Dictionary<int, NetworkCache>();
        private static readonly Queue<IntVec3> floodQueue = new Queue<IntVec3>();

        public static void ResetSession()
        {
            pipeVersionByMap.Clear();
            networksByMap.Clear();
        }

        public static void Notify_PipesChanged(Map map)
        {
            if (map == null)
            {
                return;
            }
            pipeVersionByMap.TryGetValue(map.uniqueID, out int version);
            pipeVersionByMap[map.uniqueID] = version + 1;
        }

        // Breadth-first, so a long pipe run cannot overflow the stack.
        public static void CollectNetwork(Map map, IntVec3 start, HashSet<IntVec3> network)
        {
            if (!start.InBounds(map) || !network.Add(start))
            {
                return;
            }
            floodQueue.Clear();
            floodQueue.Enqueue(start);
            while (floodQueue.Count > 0)
            {
                IntVec3 cell = floodQueue.Dequeue();
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    IntVec3 next = cell + dir;
                    if (Building_GasPipe.At(map, next) != null && network.Add(next))
                    {
                        floodQueue.Enqueue(next);
                    }
                }
            }
        }

        public static bool NetworkReachesOutdoor(Map map, HashSet<IntVec3> network)
        {
            foreach (IntVec3 cell in network)
            {
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    if (SmokeVentUtility.CellIsOutdoor(map, cell + dir))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static HashSet<Room> RoomsOnNetwork(Map map, HashSet<IntVec3> network)
        {
            var rooms = new HashSet<Room>();
            foreach (IntVec3 cell in network)
            {
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    IntVec3 adj = cell + dir;
                    if (network.Contains(adj) || !adj.InBounds(map))
                    {
                        continue;
                    }
                    Room room = adj.GetRoom(map);
                    if (room != null && !room.IsDoorway && !room.UsesOutdoorTemperature)
                    {
                        rooms.Add(room);
                    }
                }
            }
            return rooms;
        }

        public static bool ExhaustOpensIntoPipe(Thing vent, out HashSet<IntVec3> network)
        {
            network = null;
            IntVec3 exhaust = SmokeVentUtility.ExhaustCell(vent);
            if (!exhaust.InBounds(vent.Map) || Building_GasPipe.At(vent.Map, exhaust) == null)
            {
                return false;
            }
            network = new HashSet<IntVec3>();
            CollectNetwork(vent.Map, exhaust, network);
            return network.Count > 0;
        }

        // The sets are shared with the cache: read them, never change them.
        public static void ForEachNetwork(Map map, System.Action<HashSet<IntVec3>> worker)
        {
            List<HashSet<IntVec3>> networks = NetworksOn(map);
            for (int i = 0; i < networks.Count; i++)
            {
                worker(networks[i]);
            }
        }

        private static List<HashSet<IntVec3>> NetworksOn(Map map)
        {
            if (!networksByMap.TryGetValue(map.uniqueID, out NetworkCache cache))
            {
                cache = new NetworkCache();
                networksByMap[map.uniqueID] = cache;
            }
            pipeVersionByMap.TryGetValue(map.uniqueID, out int version);
            if (cache.version == version)
            {
                return cache.networks;
            }
            cache.version = version;
            cache.networks.Clear();
            var visited = new HashSet<IntVec3>();
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not Building_GasPipe || !thing.Spawned || visited.Contains(thing.Position))
                {
                    continue;
                }
                var network = new HashSet<IntVec3>();
                CollectNetwork(map, thing.Position, network);
                visited.UnionWith(network);
                cache.networks.Add(network);
            }
            return cache.networks;
        }
    }
}
