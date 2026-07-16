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
    [HarmonyPatch(typeof(Room), "UsesOutdoorTemperature", MethodType.Getter)]
    public static class Patch_UndergroundUsesOutdoorTemperature
    {
        public static void Postfix(Room __instance, ref bool __result)
        {
            if (!__result || __instance?.Map == null)
            {
                return;
            }
            if (StrataRoomUtility.ShouldTreatAsEnclosedUnderground(__instance.Map))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Room), "PsychologicallyOutdoors", MethodType.Getter)]
    public static class Patch_UndergroundPsychologicallyOutdoors
    {
        public static void Postfix(Room __instance, ref bool __result)
        {
            if (!__result || __instance?.Map == null || __instance.CellCount <= 1)
            {
                return;
            }
            if (StrataRoomUtility.ShouldTreatAsEnclosedUnderground(__instance.Map))
            {
                __result = false;
            }
        }
    }

    internal static class StrataRoomUtility
    {
        // B1 and every level below (depth >= 1). Surface and non-Strata maps
        // are unchanged.
        public static bool ShouldTreatAsEnclosedUnderground(Map map)
        {
            return map != null && StrataMapUtility.IsUnderground(map) && StrataDepth.Of(map) >= 1;
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
