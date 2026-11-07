#nullable enable
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
                CoolingLookup.For(parent.Map)?.AddCooler(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (map != null)
                CoolingLookup.For(map)?.RemoveCooler(this);
        }

        public override string? CompInspectStringExtra()
        {
            if (!HomesteaderMod.Settings.showCoolingInspect)
            {
                return null;
            }

            return "Homesteader_PassiveCoolStorage".Translate(Props.maxTemperature.ToStringTemperature());
        }
    }

    /// <summary>
    /// Tracks cells chilled by any passive cooler (root cellar, icehouse, springhouse).
    /// When several overlap, the coolest ceiling wins.
    /// </summary>
    public class RootCellarCoolingMapComponent : MapComponent
    {
        private const int FallbackRescanTicks = 250;

        // Coolers on this map, kept by the comp's spawn/despawn so a rebuild never
        // scans every building (AZR-679).
        private readonly HashSet<CompRootCellarCooling> coolers = new HashSet<CompRootCellarCooling>();

        // Cooling ceiling per cell by CellIndices; NaN = not cooled (AZR-561).
        private float[]? ceilings;
        private readonly List<int> cooledCells = new List<int>();

        private bool dirty = true;

        public RootCellarCoolingMapComponent(Map map) : base(map)
        {
            CoolingLookup.Register(this);
        }

        public override void MapRemoved()
        {
            CoolingLookup.Unregister(map);
        }

        public bool HasAnyCooling => cooledCells.Count > 0;

        /// <summary>Cooled cells by CellIndices, for the overlay. Valid until the next rebuild.</summary>
        internal IReadOnlyList<int> CooledCells => cooledCells;

        public bool TryGetCoolingCeiling(IntVec3 cell, out float maxTemperature)
        {
            maxTemperature = 0f;
            if (ceilings == null || cooledCells.Count == 0 || !cell.InBounds(map))
            {
                return false;
            }

            float v = ceilings[map.cellIndices.CellToIndex(cell)];
            if (float.IsNaN(v))
            {
                return false;
            }

            maxTemperature = v;
            return true;
        }

        internal void AddCooler(CompRootCellarCooling comp)
        {
            coolers.Add(comp);
            dirty = true;
        }

        internal void RemoveCooler(CompRootCellarCooling comp)
        {
            coolers.Remove(comp);
            dirty = true;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public override void MapComponentTick()
        {
            if (!PatchHealth.RoomRebuildHooked
                && (Find.TickManager.TicksGame + map.uniqueID) % FallbackRescanTicks == 0)
            {
                dirty = true;
            }

            if (!dirty)
            {
                return;
            }

            Rebuild();
        }

        public void Rebuild()
        {
            dirty = false;
            if (ceilings == null || ceilings.Length != map.cellIndices.NumGridCells)
            {
                ceilings = new float[map.cellIndices.NumGridCells];
                for (int i = 0; i < ceilings.Length; i++)
                {
                    ceilings[i] = float.NaN;
                }
            }
            else
            {
                for (int i = 0; i < cooledCells.Count; i++)
                {
                    ceilings[cooledCells[i]] = float.NaN;
                }
            }

            cooledCells.Clear();
            foreach (CompRootCellarCooling comp in coolers)
            {
                Thing t = comp.parent;
                if (!t.Spawned || t.Map != map)
                {
                    continue;
                }

                float ceiling = comp.Props.maxTemperature;
                Room? room = t.GetRoom();
                bool indoor = room != null && !room.PsychologicallyOutdoors;

                foreach (IntVec3 cell in t.OccupiedRect())
                {
                    AddCell(cell, ceiling);
                    if (!indoor)
                    {
                        continue;
                    }

                    for (int d = 0; d < 8; d++)
                    {
                        IntVec3 adj = cell + GenAdj.AdjacentCells[d];
                        if (adj.InBounds(map) && adj.GetRoom(map) == room)
                        {
                            AddCell(adj, ceiling);
                        }
                    }
                }
            }
        }

        private void AddCell(IntVec3 cell, float maxTemperature)
        {
            if (!cell.InBounds(map))
            {
                return;
            }

            int index = map.cellIndices.CellToIndex(cell);
            float existing = ceilings![index];
            if (float.IsNaN(existing))
            {
                ceilings[index] = maxTemperature;
                cooledCells.Add(index);
            }
            else if (maxTemperature < existing)
            {
                ceilings[index] = maxTemperature;
            }
        }
    }

    internal static class CoolingLookup
    {
        private static readonly Dictionary<int, RootCellarCoolingMapComponent> byMapId =
            new Dictionary<int, RootCellarCoolingMapComponent>();

        internal static void Clear()
        {
            byMapId.Clear();
        }

        internal static void Register(RootCellarCoolingMapComponent? comp)
        {
            if (comp?.map == null)
            {
                return;
            }

            byMapId[comp.map.uniqueID] = comp;
        }

        internal static void Unregister(Map? map)
        {
            if (map == null)
            {
                return;
            }

            byMapId.Remove(map.uniqueID);
        }

        internal static RootCellarCoolingMapComponent? For(Map? map)
        {
            if (map == null)
            {
                return null;
            }

            if (byMapId.TryGetValue(map.uniqueID, out RootCellarCoolingMapComponent? cached))
            {
                return cached;
            }

            RootCellarCoolingMapComponent? found = map.GetComponent<RootCellarCoolingMapComponent>();
            if (found != null)
            {
                byMapId[map.uniqueID] = found;
            }

            return found;
        }
    }

    public static class RootCellarUtility
    {
        public static bool TryGetCoolingCeiling(Thing? thing, out float maxTemperature)
        {
            maxTemperature = 0f;
            if (thing?.Map == null || !thing.Spawned)
            {
                return false;
            }

            RootCellarCoolingMapComponent? comp = CoolingLookup.For(thing.Map);
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
            // Def check first: pawns and buildings leave before any map lookup (AZR-561).
            if (!HomesteaderHarmony.IsRottable(__instance.def) || !__instance.Spawned)
            {
                return;
            }

            RootCellarCoolingMapComponent? cooling = CoolingLookup.For(__instance.Map);
            if (cooling == null || !cooling.HasAnyCooling)
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

    /// <summary>
    /// Room splits/merges rebuild regions without spawning/despawning a cooler.
    /// Invalidate the cooled-cell cache whenever rooms actually rebuild.
    /// </summary>
    [HarmonyPatch(typeof(RegionAndRoomUpdater), nameof(RegionAndRoomUpdater.RebuildAllRegionsAndRooms))]
    public static class Patch_PassiveCooling_RebuildAllRegions
    {
        public static void Postfix(RegionAndRoomUpdater __instance)
        {
            MarkCoolingDirty(__instance);
        }

        internal static void MarkCoolingDirty(RegionAndRoomUpdater? updater)
        {
            CoolingLookup.For(PrivateAccess.MapOf(updater))?.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(RegionAndRoomUpdater), nameof(RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms))]
    public static class Patch_PassiveCooling_TryRebuildDirtyRegions
    {
        public static void Prefix(RegionAndRoomUpdater __instance, ref bool __state)
        {
            Map? map = PrivateAccess.MapOf(__instance);
            __state = map?.regionDirtyer != null && map.regionDirtyer.AnyDirty;
        }

        public static void Postfix(RegionAndRoomUpdater __instance, bool __state)
        {
            if (!__state)
            {
                return;
            }

            Patch_PassiveCooling_RebuildAllRegions.MarkCoolingDirty(__instance);
        }
    }
}
