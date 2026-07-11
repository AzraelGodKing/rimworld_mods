using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class SmokeVentUtility
    {
        public const float DoorVentPerOpenExterior = 0.12f;
        public const float MaxDoorVentBonus = 0.24f;

        public static Room IntakeRoom(Thing thing)
        {
            IntVec3 cell = thing.Position - thing.Rotation.FacingCell;
            return cell.InBounds(thing.Map) ? cell.GetRoom(thing.Map) : null;
        }

        public static Room ExhaustRoom(Thing thing)
        {
            IntVec3 cell = thing.Position + thing.Rotation.FacingCell;
            return cell.InBounds(thing.Map) ? cell.GetRoom(thing.Map) : null;
        }

        public static IntVec3 IntakeCell(Thing thing) => thing.Position - thing.Rotation.FacingCell;

        public static IntVec3 ExhaustCell(Thing thing) => thing.Position + thing.Rotation.FacingCell;

        public static bool CellIsOutdoor(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            Room room = cell.GetRoom(map);
            return room == null || room.UsesOutdoorTemperature;
        }

        public static bool RoomHasOpenExteriorDoor(Room room)
        {
            return DoorVentBonus(room) > 0f;
        }

        public static float DoorVentBonus(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors)
            {
                return 0f;
            }
            int doors = 0;
            HashSet<Building> counted = new HashSet<Building>();
            foreach (Region region in room.Regions)
            {
                Building_Door door = region.door;
                if (door == null || !door.Open || !counted.Add(door))
                {
                    continue;
                }
                foreach (RegionLink link in region.links)
                {
                    Region other = link.GetOtherRegion(region);
                    if (other?.Room != null && other.Room.PsychologicallyOutdoors)
                    {
                        doors++;
                        break;
                    }
                }
            }
            return Mathf.Min(doors * DoorVentPerOpenExterior, MaxDoorVentBonus);
        }

        public static void CollectDuctNetwork(Map map, IntVec3 start, HashSet<IntVec3> network)
        {
            if (!start.InBounds(map) || !network.Add(start))
            {
                return;
            }
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 next = start + dir;
                if (!next.InBounds(map))
                {
                    continue;
                }
                if (next.GetFirstBuilding(map)?.TryGetComp<CompSmokeDuct>() != null)
                {
                    CollectDuctNetwork(map, next, network);
                }
            }
        }

        public static bool DuctNetworkReachesOutdoor(Map map, HashSet<IntVec3> network)
        {
            foreach (IntVec3 cell in network)
            {
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    if (CellIsOutdoor(map, cell + dir))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool ExhaustOpensIntoDuct(Thing vent, out HashSet<IntVec3> network)
        {
            network = null;
            IntVec3 exhaust = ExhaustCell(vent);
            if (!exhaust.InBounds(vent.Map) || exhaust.GetFirstBuilding(vent.Map)?.TryGetComp<CompSmokeDuct>() == null)
            {
                return false;
            }
            network = new HashSet<IntVec3>();
            CollectDuctNetwork(vent.Map, exhaust, network);
            return network.Count > 0;
        }
    }
}
