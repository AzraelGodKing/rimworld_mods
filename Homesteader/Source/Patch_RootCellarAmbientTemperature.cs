using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    // Spoilables on a root cellar — or in its cool adjacent indoor cells — use the
    // cellar temperature ceiling for AmbientTemperature / CompRottable.
    [HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
    public static class Patch_RootCellarAmbientTemperature
    {
        public static void Postfix(Thing __instance, ref float __result)
        {
            if (__instance.TryGetComp<CompRottable>() == null || __instance.Map == null || !__instance.Spawned)
            {
                return;
            }

            CompRootCellarCooling cellar = RootCellarUtility.GetCoolingAffecting(__instance);
            if (cellar == null)
            {
                return;
            }

            __result = Mathf.Min(__result, cellar.Props.maxTemperature);
        }
    }

    public static class RootCellarUtility
    {
        public static CompRootCellarCooling GetCoolingAffecting(Thing thing)
        {
            if (thing?.Map == null || !thing.Spawned)
            {
                return null;
            }

            // Directly on the cellar building cells.
            CompRootCellarCooling direct = GetCoolingCellarAt(thing.Position, thing.Map);
            if (direct != null)
            {
                return direct;
            }

            // Indoor cells next to a root cellar (room chill).
            RootCellarCoolingMapComponent comp = thing.Map.GetComponent<RootCellarCoolingMapComponent>();
            if (comp == null)
            {
                return null;
            }

            if (!comp.IsCooledCell(thing.Position))
            {
                return null;
            }

            // Find any cellar on the map to read maxTemperature (all share the same props).
            List<Thing> buildings = thing.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                CompRootCellarCooling found = buildings[i].TryGetComp<CompRootCellarCooling>();
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static CompRootCellarCooling GetCoolingCellarAt(IntVec3 cell, Map map)
        {
            Building edifice = cell.GetEdifice(map);
            CompRootCellarCooling comp = edifice?.TryGetComp<CompRootCellarCooling>();
            if (comp != null)
            {
                return comp;
            }

            foreach (Thing t in cell.GetThingList(map))
            {
                if (t is Building building)
                {
                    CompRootCellarCooling found = building.TryGetComp<CompRootCellarCooling>();
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }
    }
}
