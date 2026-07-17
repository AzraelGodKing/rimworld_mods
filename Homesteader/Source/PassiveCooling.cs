using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class CompProperties_RootCellarCooling : CompProperties
    {
        /// <summary>Stored items never report ambient temperature above this (°C).</summary>
        public float maxTemperature = 5f;

        public CompProperties_RootCellarCooling()
        {
            compClass = typeof(CompRootCellarCooling);
        }
    }

    public class CompRootCellarCooling : ThingComp
    {
        public CompProperties_RootCellarCooling Props => (CompProperties_RootCellarCooling)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map?.GetComponent<RootCellarCoolingMapComponent>()?.Rebuild();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            map?.GetComponent<RootCellarCoolingMapComponent>()?.Rebuild();
        }

        public override string CompInspectStringExtra()
        {
            return "Passive cool storage: items (and adjacent indoor cells) stay at or below "
                + Props.maxTemperature.ToStringTemperature();
        }
    }

    /// <summary>
    /// Tracks cells chilled by any passive cooler (root cellar, icehouse, springhouse).
    /// When several overlap, the coolest ceiling wins.
    /// </summary>
    public class RootCellarCoolingMapComponent : MapComponent
    {
        private readonly Dictionary<IntVec3, float> cooledCellTemps = new Dictionary<IntVec3, float>();

        private int lastRebuildTick = -99999;

        public RootCellarCoolingMapComponent(Map map) : base(map)
        {
        }

        public bool TryGetCoolingCeiling(IntVec3 cell, out float maxTemperature) =>
            cooledCellTemps.TryGetValue(cell, out maxTemperature);

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame - lastRebuildTick < 250)
            {
                return;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            lastRebuildTick = Find.TickManager.TicksGame;
            cooledCellTemps.Clear();

            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing t = buildings[i];
                CompRootCellarCooling comp = t.TryGetComp<CompRootCellarCooling>();
                if (comp == null)
                {
                    continue;
                }

                float ceiling = comp.Props.maxTemperature;
                Room room = t.GetRoom();
                bool indoor = room != null && !room.PsychologicallyOutdoors;

                foreach (IntVec3 cell in t.OccupiedRect())
                {
                    AddCell(cell, ceiling);
                    if (!indoor)
                    {
                        continue;
                    }

                    foreach (IntVec3 adj in GenAdj.CellsAdjacent8Way(new TargetInfo(cell, map)))
                    {
                        if (!adj.InBounds(map))
                        {
                            continue;
                        }

                        Room adjRoom = adj.GetRoom(map);
                        if (adjRoom == room)
                        {
                            AddCell(adj, ceiling);
                        }
                    }
                }
            }
        }

        private void AddCell(IntVec3 cell, float maxTemperature)
        {
            if (cooledCellTemps.TryGetValue(cell, out float existing))
            {
                cooledCellTemps[cell] = Mathf.Min(existing, maxTemperature);
            }
            else
            {
                cooledCellTemps[cell] = maxTemperature;
            }
        }
    }

    public static class RootCellarUtility
    {
        public static bool TryGetCoolingCeiling(Thing thing, out float maxTemperature)
        {
            maxTemperature = 0f;
            if (thing?.Map == null || !thing.Spawned)
            {
                return false;
            }

            RootCellarCoolingMapComponent comp = thing.Map.GetComponent<RootCellarCoolingMapComponent>();
            return comp != null && comp.TryGetCoolingCeiling(thing.Position, out maxTemperature);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
    public static class Patch_RootCellarAmbientTemperature
    {
        public static void Postfix(Thing __instance, ref float __result)
        {
            if (__instance.TryGetComp<CompRottable>() == null || __instance.Map == null || !__instance.Spawned)
            {
                return;
            }

            if (!RootCellarUtility.TryGetCoolingCeiling(__instance, out float ceiling))
            {
                return;
            }

            __result = Mathf.Min(__result, ceiling);
        }
    }
}
