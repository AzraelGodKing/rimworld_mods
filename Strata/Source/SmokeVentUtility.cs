using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class SmokeVentUtility
    {
        // High enough that one open exterior door genuinely clears a worked
        // bench's smoke - "open the door" must be a real answer.
        public const float DoorVentPerOpenExterior = 0.2f;
        public const float MaxDoorVentBonus = 0.5f;

        // Soft-compat: Vanilla Temperature Expanded wall-mounted vent (and
        // similarly named wall vents). No hard dependency on VTE.
        public const string VteWallMountedVentDefName = "VTE_WallMountedVent";

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

        // A room that directly touches open air: open roof, an open exterior
        // door, or a wall vent facing outdoors. Seeds the outdoor-vent cluster.
        public static bool RoomHasDirectOutdoorOpening(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors || room.UsesOutdoorTemperature)
            {
                return false;
            }
            if (room.OpenRoofCount > 0 || DoorVentBonus(room) > 0f)
            {
                return true;
            }
            Map map = room.Map;
            foreach (IntVec3 cell in room.BorderCellsCached)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Building vent = FindOpenVentAt(map, cell);
                if (vent != null && VentLeadsOutdoors(vent, room))
                {
                    return true;
                }
            }
            return false;
        }

        // Rooms reachable through open doors from any direct outdoor opening.
        // One open exterior door vents the whole connected building.
        public static void CollectOutdoorVentCluster(Map map, HashSet<int> roomIds, Func<IntVec3, bool> borderIsSealed = null)
        {
            roomIds.Clear();
            if (map == null)
            {
                return;
            }
            var queue = new Queue<Room>();
            var visited = new HashSet<int>();
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room == null || room.IsDoorway || room.PsychologicallyOutdoors
                    || room.UsesOutdoorTemperature || !RoomHasDirectOutdoorOpening(room))
                {
                    continue;
                }
                if (visited.Add(room.ID))
                {
                    queue.Enqueue(room);
                }
            }
            while (queue.Count > 0)
            {
                Room room = queue.Dequeue();
                roomIds.Add(room.ID);
                foreach (Room neighbor in OpenDoorNeighbors(room, map, borderIsSealed))
                {
                    if (neighbor != null && visited.Add(neighbor.ID))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
                foreach (Room neighbor in OpenVentInteriorNeighbors(room, map, borderIsSealed))
                {
                    if (neighbor != null && visited.Add(neighbor.ID))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Interior rooms reachable through open wall vents (kitchen → corridor
        // → outdoor vent chains count as ventilated even without a door path).
        public static IEnumerable<Room> OpenVentInteriorNeighbors(
            Room room,
            Map map,
            Func<IntVec3, bool> borderIsSealed = null)
        {
            if (room == null || map == null)
            {
                yield break;
            }
            HashSet<Building> counted = new HashSet<Building>();
            foreach (IntVec3 borderCell in room.BorderCellsCached)
            {
                if (!borderCell.InBounds(map) || borderIsSealed?.Invoke(borderCell) == true)
                {
                    continue;
                }
                Building vent = FindOpenVentAt(map, borderCell);
                if (vent == null || !counted.Add(vent) || VentLeadsOutdoors(vent, room))
                {
                    continue;
                }
                Room exhaustRoom = ExhaustRoom(vent);
                if (exhaustRoom != null && exhaustRoom != room && !exhaustRoom.IsDoorway
                    && !exhaustRoom.UsesOutdoorTemperature && exhaustRoom.CellCount > 0)
                {
                    yield return exhaustRoom;
                }
            }
        }

        // Interior rooms in the cluster (connected to outdoors but no direct opening).
        public static IEnumerable<Room> OpenDoorNeighbors(Room room, Map map, Func<IntVec3, bool> borderIsSealed = null)
        {
            if (room == null || map == null)
            {
                yield break;
            }
            HashSet<Building> counted = new HashSet<Building>();
            foreach (IntVec3 borderCell in room.BorderCellsCached)
            {
                if (!borderCell.InBounds(map) || borderIsSealed?.Invoke(borderCell) == true)
                {
                    continue;
                }
                Building_Door door = borderCell.GetDoor(map);
                if (door == null || !door.Open || !counted.Add(door))
                {
                    continue;
                }
                Room neighbor = null;
                bool exterior = false;
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    IntVec3 beyond = door.Position + dir;
                    if (!beyond.InBounds(map))
                    {
                        continue;
                    }
                    Room other = beyond.GetRoom(map);
                    if (other == null || other == room || other.IsDoorway)
                    {
                        continue;
                    }
                    if (other.PsychologicallyOutdoors || other.UsesOutdoorTemperature)
                    {
                        exterior = true;
                    }
                    else
                    {
                        neighbor = other;
                    }
                }
                if (!exterior && neighbor != null)
                {
                    yield return neighbor;
                }
            }
        }

        // Doors form their own one-cell "doorway room" in RimWorld, so they
        // never appear in a room's own Regions - find them by scanning the
        // room's border cells instead.
        public static float DoorVentBonus(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors)
            {
                return 0f;
            }
            Map map = room.Map;
            int doors = 0;
            HashSet<Building> counted = new HashSet<Building>();
            foreach (IntVec3 cell in room.BorderCellsCached)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Building_Door door = cell.GetDoor(map);
                if (door == null || !door.Open || !counted.Add(door))
                {
                    continue;
                }
                if (DoorLeadsOutdoors(door, room))
                {
                    doors++;
                }
            }
            return Mathf.Min(doors * DoorVentPerOpenExterior, MaxDoorVentBonus);
        }

        public static bool DoorLeadsOutdoors(Building_Door door, Room from)
        {
            return OpeningLeadsOutdoors(door, from);
        }

        // Whether the far side of a wall opening (door or vent) is open air.
        public static bool OpeningLeadsOutdoors(Building opening, Room from)
        {
            Map map = opening.Map;
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 cell = opening.Position + dir;
                if (!cell.InBounds(map))
                {
                    continue;
                }
                if (CellIsOutdoor(map, cell))
                {
                    return true;
                }
                Room other = cell.GetRoom(map);
                if (other == null || other == from || other.IsDoorway)
                {
                    continue;
                }
                if (other.PsychologicallyOutdoors || other.UsesOutdoorTemperature)
                {
                    return true;
                }
            }
            return false;
        }

        // Wall vents bridge through the wall: the exhaust side (+FacingCell) may
        // be open air even when the room footprint only touches the intake cell.
        public static bool VentLeadsOutdoors(Building vent, Room from)
        {
            if (!IsOpenVent(vent))
            {
                return false;
            }
            Map map = vent.Map;
            if (CellIsOutdoor(map, ExhaustCell(vent)))
            {
                return true;
            }
            Room exhaustRoom = ExhaustRoom(vent);
            return exhaustRoom != null && exhaustRoom != from && !exhaustRoom.IsDoorway
                && (exhaustRoom.PsychologicallyOutdoors || exhaustRoom.UsesOutdoorTemperature);
        }

        // A vanilla wall vent, VTE-style wall-mounted vent, or similarly named
        // outdoor-facing wall vent that isn't flicked off. Smoke follows the
        // same path temperature does.
        public static bool IsOpenVent(Building building)
        {
            if (building == null || building.TryGetComp<CompFlickable>()?.SwitchIsOn == false)
            {
                return false;
            }
            return building is Building_Vent || LooksLikeWallVent(building.def);
        }

        // Wall-mounted vents (VTE_WallMountedVent) sit on a wall with
        // isEdifice=false, so GetEdifice returns the Wall. Scan the cell's
        // things as well so surface rooms vented that way count as ventilated.
        public static Building FindOpenVentAt(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return null;
            }
            Building edifice = cell.GetEdifice(map);
            if (IsOpenVent(edifice))
            {
                return edifice;
            }
            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building building && building != edifice && IsOpenVent(building))
                {
                    return building;
                }
            }
            return null;
        }

        public static bool LooksLikeWallVent(ThingDef def)
        {
            if (def?.defName == null)
            {
                return false;
            }
            string name = def.defName;
            if (name == VteWallMountedVentDefName)
            {
                return true;
            }
            // Soft patterns for VTE / Simple Utilities: Wall / similar mounts.
            // Avoid matching heaters, coolers, or valves.
            if (name.IndexOf("Heater", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cooler", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Valve", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
            return name.IndexOf("WallMountedVent", StringComparison.OrdinalIgnoreCase) >= 0
                || name.EndsWith("WallVent", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("_WallVent", StringComparison.OrdinalIgnoreCase);
        }

        public static void CollectDuctNetwork(Map map, IntVec3 start, HashSet<IntVec3> network)
        {
            GasPipeUtility.CollectNetwork(map, start, network);
        }

        public static bool DuctNetworkReachesOutdoor(Map map, HashSet<IntVec3> network)
        {
            return GasPipeUtility.NetworkReachesOutdoor(map, network);
        }

        public static bool ExhaustOpensIntoDuct(Thing vent, out HashSet<IntVec3> network)
        {
            return GasPipeUtility.ExhaustOpensIntoPipe(vent, out network);
        }
    }
}
