using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public static class GasPipeUtility
    {
        public const float PipeInteriorFlow = 0.3f;
        public const float PipeOutdoorDrain = 0.35f;

        public static void CollectNetwork(Map map, IntVec3 start, HashSet<IntVec3> network)
        {
            if (!start.InBounds(map) || !network.Add(start))
            {
                return;
            }
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 next = start + dir;
                if (Building_GasPipe.At(map, next) != null)
                {
                    CollectNetwork(map, next, network);
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

        public static void ForEachNetwork(Map map, System.Action<HashSet<IntVec3>> worker)
        {
            var visited = new HashSet<IntVec3>();
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not Building_GasPipe || !thing.Spawned)
                {
                    continue;
                }
                IntVec3 start = thing.Position;
                if (!visited.Add(start))
                {
                    continue;
                }
                var network = new HashSet<IntVec3>();
                CollectNetwork(map, start, network);
                visited.UnionWith(network);
                worker(network);
            }
        }
    }
}
