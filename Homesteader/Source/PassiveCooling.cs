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
            if (parent.Map != null)
                CoolingLookup.For(parent.Map)?.MarkDirty();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (map != null)
                CoolingLookup.For(map)?.MarkDirty();
        }

        public override string CompInspectStringExtra()
        {
            return "Homesteader_PassiveCoolStorage".Translate(Props.maxTemperature.ToStringTemperature());
        }
    }

    /// <summary>
    /// Tracks cells chilled by any passive cooler (root cellar, icehouse, springhouse).
    /// When several overlap, the coolest ceiling wins.
    /// </summary>
    public class RootCellarCoolingMapComponent : MapComponent
    {
        private readonly Dictionary<IntVec3, float> cooledCellTemps = new Dictionary<IntVec3, float>();

        private bool dirty = true;

        public RootCellarCoolingMapComponent(Map map) : base(map)
        {
            CoolingLookup.Register(this);
        }

        public override void MapRemoved()
        {
            CoolingLookup.Unregister(map);
        }

        public bool HasAnyCooling => cooledCellTemps.Count > 0;

        public bool TryGetCoolingCeiling(IntVec3 cell, out float maxTemperature) =>
            cooledCellTemps.TryGetValue(cell, out maxTemperature);

        public void MarkDirty()
        {
            dirty = true;
        }

        public override void MapComponentTick()
        {
            if (!dirty)
            {
                return;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            dirty = false;
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

    internal static class CoolingLookup
    {
        private static readonly Dictionary<int, RootCellarCoolingMapComponent> byMapId =
            new Dictionary<int, RootCellarCoolingMapComponent>();

        internal static void Register(RootCellarCoolingMapComponent comp)
        {
            if (comp?.map == null)
            {
                return;
            }

            byMapId[comp.map.uniqueID] = comp;
        }

        internal static void Unregister(Map map)
        {
            if (map == null)
            {
                return;
            }

            byMapId.Remove(map.uniqueID);
        }

        internal static RootCellarCoolingMapComponent For(Map map)
        {
            if (map == null)
            {
                return null;
            }

            if (byMapId.TryGetValue(map.uniqueID, out RootCellarCoolingMapComponent cached))
            {
                return cached;
            }

            RootCellarCoolingMapComponent found = map.GetComponent<RootCellarCoolingMapComponent>();
            if (found != null)
            {
                byMapId[map.uniqueID] = found;
            }

            return found;
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

            RootCellarCoolingMapComponent comp = CoolingLookup.For(thing.Map);
            if (comp == null || !comp.HasAnyCooling)
            {
                return false;
            }

            return comp.TryGetCoolingCeiling(thing.Position, out maxTemperature);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
    public static class Patch_RootCellarAmbientTemperature
    {
        public static void Postfix(Thing __instance, ref float __result)
        {
            Map map = __instance.Map;
            if (map == null || !__instance.Spawned)
            {
                return;
            }

            RootCellarCoolingMapComponent cooling = CoolingLookup.For(map);
            if (cooling == null || !cooling.HasAnyCooling)
            {
                return;
            }

            if (!HomesteaderHarmony.RottableDefs.Contains(__instance.def))
            {
                return;
            }

            if (!cooling.TryGetCoolingCeiling(__instance.Position, out float ceiling))
            {
                return;
            }

            __result = Mathf.Min(__result, ceiling);
        }
    }
}
