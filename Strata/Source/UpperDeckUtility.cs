using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // A+ floors are roof decks: walkable/buildable only where the map below has
    // a roof (or inside the shaft plaza). Everything else is open sky.
    public static class UpperDeckUtility
    {
        public const string RoofDeckDefName = "Strata_RoofDeck";
        public const string OpenSkyDefName = "Strata_OpenSky";
        public const float DefaultPlazaRadius = 6.5f;

        // Set while painting a whole upper map so per-cell SetRoof noise skips sync.
        public static bool SuspendRoofSync;

        private static TerrainDef roofDeck;
        private static TerrainDef openSky;

        public static TerrainDef RoofDeck =>
            roofDeck ??= DefDatabase<TerrainDef>.GetNamedSilentFail(RoofDeckDefName) ?? TerrainDefOf.Concrete;

        public static TerrainDef OpenSky =>
            openSky ??= DefDatabase<TerrainDef>.GetNamedSilentFail(OpenSkyDefName) ?? TerrainDefOf.WaterDeep;

        public static Map SourceMapFor(Map upper)
        {
            if (upper?.Parent is PocketMapParent pocket && pocket.sourceMap != null)
            {
                return pocket.sourceMap;
            }
            return PocketMapUtility.currentlyGeneratingPortal?.Map;
        }

        public static bool IsManagedUpperTerrain(TerrainDef terrain)
        {
            return terrain != null
                && (terrain.defName == RoofDeckDefName || terrain.defName == OpenSkyDefName);
        }

        public static bool SourceHasRoofUnder(Map upper, IntVec3 upperCell, Map source = null)
        {
            return SourceSupportsUpperDeck(upper, upperCell, source);
        }

        public static bool SourceSupportsUpperDeck(Map upper, IntVec3 upperCell, Map source = null)
        {
            source ??= SourceMapFor(upper);
            if (source == null || !upperCell.InBounds(upper))
            {
                return false;
            }
            IntVec3 below = StrataMapUtility.ProportionalCell(upperCell, upper, source);
            return SourceCellSupportsDeck(source, below, IsGravshipLinkedUpper(upper));
        }

        private static bool IsGravshipLinkedUpper(Map upper)
        {
            if (!StrataGravshipUtility.OdysseyActive)
            {
                return false;
            }
            if (upper != null && StrataGravshipUtility.IsInGravshipStack(upper))
            {
                return true;
            }
            return PocketMapUtility.currentlyGeneratingPortal is IStrataGravshipPortal;
        }

        private static bool SourceCellSupportsDeck(Map source, IntVec3 below, bool gravshipLinkedUpper)
        {
            if (!below.InBounds(source))
            {
                return false;
            }
            if (source.roofGrid.Roofed(below))
            {
                return true;
            }
            return gravshipLinkedUpper && StrataGravshipUtility.CellOnGravship(source, below);
        }

        // Full paint used by map gen (and rare rebuilds).
        public static void PaintFromSourceRoofs(Map upper, IntVec3 landingSpot, float plazaRadius = DefaultPlazaRadius)
        {
            if (upper == null || !StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }
            Map source = SourceMapFor(upper);
            TerrainDef deck = RoofDeck;
            TerrainDef sky = OpenSky;
            bool gravshipLinked = IsGravshipLinkedUpper(upper);

            SuspendRoofSync = true;
            try
            {
                if (source != null && source.Size == upper.Size)
                {
                    PaintSameSize(upper, source, deck, sky, gravshipLinked);
                }
                else if (source != null)
                {
                    PaintProportional(upper, source, deck, sky, gravshipLinked);
                }
                else
                {
                    foreach (IntVec3 cell in upper.AllCells)
                    {
                        upper.roofGrid.SetRoof(cell, null);
                        upper.terrainGrid.SetTerrain(cell, sky);
                    }
                }

                EnsurePlaza(upper, landingSpot, plazaRadius);
            }
            finally
            {
                SuspendRoofSync = false;
            }
        }

        private static void PaintSameSize(Map upper, Map source, TerrainDef deck, TerrainDef sky, bool gravshipLinked)
        {
            foreach (IntVec3 cell in upper.AllCells)
            {
                upper.roofGrid.SetRoof(cell, null);
                bool supported = SourceCellSupportsDeck(source, cell, gravshipLinked);
                upper.terrainGrid.SetTerrain(cell, supported ? deck : sky);
            }
        }

        private static void PaintProportional(Map upper, Map source, TerrainDef deck, TerrainDef sky, bool gravshipLinked)
        {
            int sourceCells = source.cellIndices.NumGridCells;
            var sourceRoofs = new bool[sourceCells];
            var sourceSubstructure = gravshipLinked ? new bool[sourceCells] : null;
            foreach (IntVec3 below in source.AllCells)
            {
                int index = source.cellIndices.CellToIndex(below);
                if (source.roofGrid.Roofed(below))
                {
                    sourceRoofs[index] = true;
                }
                else if (sourceSubstructure != null && StrataGravshipUtility.CellOnGravship(source, below))
                {
                    sourceSubstructure[index] = true;
                }
            }

            foreach (IntVec3 cell in upper.AllCells)
            {
                upper.roofGrid.SetRoof(cell, null);
                IntVec3 below = StrataMapUtility.ProportionalCell(cell, upper, source);
                bool supported = below.InBounds(source)
                    && (sourceRoofs[source.cellIndices.CellToIndex(below)]
                        || (sourceSubstructure != null && sourceSubstructure[source.cellIndices.CellToIndex(below)]));
                upper.terrainGrid.SetTerrain(cell, supported ? deck : sky);
            }
        }

        public static void EnsurePlaza(Map upper, IntVec3 spot, float radius = DefaultPlazaRadius)
        {
            if (upper == null || !spot.IsValid)
            {
                return;
            }
            TerrainDef deck = RoofDeck;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(spot, radius, useCenter: true))
            {
                if (cell.InBounds(upper))
                {
                    upper.terrainGrid.SetTerrain(cell, deck);
                    upper.roofGrid.SetRoof(cell, null);
                }
            }
        }

        // Live sync when a roof is added/removed on the floor below.
        public static void SyncCell(Map upper, IntVec3 upperCell)
        {
            if (upper == null || !upperCell.InBounds(upper) || !StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }
            TerrainDef current = upperCell.GetTerrain(upper);
            bool supported = SourceSupportsUpperDeck(upper, upperCell);

            if (supported)
            {
                if (current == OpenSky || current == null)
                {
                    upper.terrainGrid.SetTerrain(upperCell, RoofDeck);
                }
                return;
            }

            // Only reclaim managed deck tiles that are empty — never rip out
            // player floors or buildings when a roof downstairs comes off.
            if (current?.defName != RoofDeckDefName)
            {
                return;
            }
            if (CellHasBlockingThing(upperCell, upper))
            {
                return;
            }
            upper.terrainGrid.SetTerrain(upperCell, OpenSky);
        }

        public static void SyncAllFromSource(Map upper)
        {
            if (upper == null || !StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }
            foreach (IntVec3 cell in upper.AllCells)
            {
                SyncCell(upper, cell);
            }
        }

        public static void SyncGravshipUpperDecksFromSource(Map source)
        {
            if (source == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            foreach (Thing thing in source.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                Map upper = portal.PocketMap;
                if (StrataMapUtility.IsUpperLevel(upper))
                {
                    SyncAllFromSource(upper);
                }
            }
        }

        private static bool CellHasBlockingThing(IntVec3 cell, Map map)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Building
                    || thing.def.IsBlueprint
                    || thing.def.IsFrame)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // When a roof is built or removed downstairs, grow/shrink the A+ deck above.
    [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    public static class Patch_RoofGrid_SyncUpperDeck
    {
        private static readonly AccessTools.FieldRef<RoofGrid, Map> MapRef =
            AccessTools.FieldRefAccess<RoofGrid, Map>("map");

        public static void Postfix(RoofGrid __instance, IntVec3 c)
        {
            if (UpperDeckUtility.SuspendRoofSync
                || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                return;
            }
            Map source = MapRef(__instance);
            if (source == null || !c.InBounds(source))
            {
                return;
            }
            // Avoid work during world gen / when no upper floors exist yet.
            List<Map> maps = Find.Maps;
            if (maps == null || maps.Count == 0)
            {
                return;
            }
            for (int i = 0; i < maps.Count; i++)
            {
                Map upper = maps[i];
                if (!StrataMapUtility.IsUpperLevel(upper))
                {
                    continue;
                }
                if ((upper.Parent as PocketMapParent)?.sourceMap != source)
                {
                    continue;
                }
                IntVec3 upperCell = StrataMapUtility.ProportionalCell(c, source, upper);
                UpperDeckUtility.SyncCell(upper, upperCell);
            }
        }
    }
}
