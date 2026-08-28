using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Biomes! cavern layouts can mark huge chambers as outdoor temperature even
    // on sealed B1+ pocket maps. For Strata gas, every underground level is
    // enclosed rock — ventilation comes from doors, vents, shafts, and pipes,
    // not from "open sky".
    //
    // Strata pocket maps (especially A+ roof decks) can leave room districts in
    // a bad state; vanilla PsychologicallyOutdoors / OpenRoofCount walk districts
    // and NRE in the temperature HUD. Prefixes below skip that path on Strata
    // levels and use roof/terrain heuristics instead.
    [HarmonyPatch(typeof(Room), "UsesOutdoorTemperature", MethodType.Getter)]
    public static class Patch_UndergroundUsesOutdoorTemperature
    {
        public static bool Prefix(Room __instance, ref bool __result)
        {
            Map map = __instance?.Map;
            if (map == null || !StrataRoomUtility.IsStrataPocketLevel(map))
            {
                return true;
            }
            __result = StrataRoomUtility.RoomUsesOutdoorTemperature(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Room), "PsychologicallyOutdoors", MethodType.Getter)]
    public static class Patch_UndergroundPsychologicallyOutdoors
    {
        public static bool Prefix(Room __instance, ref bool __result, ref bool __state)
        {
            Map map = __instance?.Map;
            __state = map != null && StrataRoomUtility.IsStrataPocketLevel(map);
            if (!__state)
            {
                return true;
            }
            __result = StrataRoomUtility.RoomPsychologicallyOutdoors(__instance);
            return false;
        }

        // Stormproof fallout scrubbers treat enclosed underground rooms as indoor.
        // Pocket levels already resolved in Prefix; this remains for Stormproof
        // when UsesOutdoorTemperature was already false on a non-pocket underground map.
        public static void Postfix(Room __instance, ref bool __result, bool __state)
        {
            if (__state || !__result || !SisterModBridges.StormproofLoaded || __instance?.Map == null)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(__instance.Map))
            {
                return;
            }
            if (!__instance.UsesOutdoorTemperature && __instance.CellCount > 1)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Room), "OpenRoofCount", MethodType.Getter)]
    public static class Patch_StrataRoomOpenRoofCount
    {
        public static bool Prefix(Room __instance, ref int __result)
        {
            Map map = __instance?.Map;
            if (map == null || !StrataRoomUtility.IsStrataPocketLevel(map))
            {
                return true;
            }
            __result = StrataRoomUtility.CountUnroofedCells(__instance);
            return false;
        }
    }

    internal static class StrataRoomUtility
    {
        private static readonly Dictionary<int, int> roofEpochByMap = new Dictionary<int, int>();
        private static readonly Dictionary<long, (int epoch, int count)> openRoofByRoom =
            new Dictionary<long, (int epoch, int count)>();

        [StrataSessionReset]
        public static void ResetSession()
        {
            roofEpochByMap.Clear();
            openRoofByRoom.Clear();
        }

        public static void NotifyRoofChanged(Map map)
        {
            if (map == null)
            {
                return;
            }
            roofEpochByMap.TryGetValue(map.uniqueID, out int epoch);
            roofEpochByMap[map.uniqueID] = epoch + 1;
        }

        // B1 and every level below (depth >= 1). Surface and non-Strata maps
        // are unchanged.
        public static bool ShouldTreatAsEnclosedUnderground(Map map)
        {
            return map != null && StrataMapUtility.IsUnderground(map) && StrataDepth.Of(map) >= 1;
        }

        public static bool IsStrataPocketLevel(Map map)
        {
            return ShouldTreatAsEnclosedUnderground(map) || StrataMapUtility.IsUpperLevel(map);
        }

        public static bool RoomUsesOutdoorTemperature(Room room)
        {
            if (room == null)
            {
                return false;
            }
            if (ShouldTreatAsEnclosedUnderground(room.Map))
            {
                return false;
            }
            return RoomPsychologicallyOutdoors(room);
        }

        public static bool RoomPsychologicallyOutdoors(Room room)
        {
            if (room == null)
            {
                return true;
            }
            if (room.CellCount <= 1 || !room.ProperRoom)
            {
                return true;
            }
            if (ShouldTreatAsEnclosedUnderground(room.Map))
            {
                return false;
            }
            if (StrataMapUtility.IsUpperLevel(room.Map))
            {
                return CountUnroofedCells(room) > 0;
            }
            return false;
        }

        public static int CountUnroofedCells(Room room)
        {
            if (room == null || room.Map == null)
            {
                return 0;
            }
            Map map = room.Map;
            long key = ((long)map.uniqueID << 32) ^ (uint)room.ID;
            roofEpochByMap.TryGetValue(map.uniqueID, out int epoch);
            if (openRoofByRoom.TryGetValue(key, out (int epoch, int count) cached) && cached.epoch == epoch)
            {
                return cached.count;
            }
            int open = 0;
            foreach (IntVec3 cell in room.Cells)
            {
                if (cell.InBounds(map) && !map.roofGrid.Roofed(cell))
                {
                    open++;
                }
            }
            openRoofByRoom[key] = (epoch, open);
            return open;
        }

        // Player-walled, fully roofed rooms (workshops, bedrooms) share a
        // uniform gas mix. Natural caverns use per-cell falloff instead.
        public static bool RoomIsColonyBuiltEnclosure(Room room)
        {
            if (room == null || !room.ProperRoom || room.IsDoorway || room.Map == null
                || !ShouldTreatAsEnclosedUnderground(room.Map))
            {
                return false;
            }
            Map map = room.Map;
            bool hasPlayerStructure = false;
            foreach (IntVec3 cell in room.Cells)
            {
                if (!cell.InBounds(map))
                {
                    return false;
                }
                if (!map.roofGrid.Roofed(cell))
                {
                    return false;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing.Faction != Faction.OfPlayer || thing.def?.building == null)
                    {
                        continue;
                    }
                    if (thing is MapPortal)
                    {
                        continue;
                    }
                    if (thing.def.building.isNaturalRock)
                    {
                        continue;
                    }
                    hasPlayerStructure = true;
                    break;
                }
            }
            return hasPlayerStructure;
        }
    }
}
