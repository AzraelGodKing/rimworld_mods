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
            bool gravship = IsGravshipLinkedUpper(upper);
            // Gravship decks are raw 1:1 with the host regardless of map size.
            IntVec3 below = gravship
                ? upperCell
                : StrataMapUtility.ProportionalCell(upperCell, upper, source);
            return SourceCellSupportsDeck(source, below, gravship);
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
            // Gravship A+: exact ship silhouette only — never colony roofs beyond the hull.
            if (gravshipLinkedUpper)
            {
                return StrataGravshipUtility.CellOnGravship(source, below);
            }
            return source.roofGrid.Roofed(below);
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
            if (gravshipLinked && source != null
                && !StrataGravshipUtility.EngineHasSubstructure(
                    StrataGravshipUtility.FindGravEngineOnMap(source)))
            {
                return;
            }

            SuspendRoofSync = true;
            try
            {
                // Gravship decks: raw 1:1 even when the host map size differs.
                if (source != null && (gravshipLinked || source.Size == upper.Size))
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

                // Colony towers get a plaza around the shaft. Gravship decks stay
                // exactly the ValidSubstructure footprint — no extra pad.
                if (!gravshipLinked)
                {
                    EnsurePlaza(upper, landingSpot, plazaRadius);
                }
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
            var sourceRoofs = gravshipLinked ? null : new bool[sourceCells];
            var sourceSubstructure = gravshipLinked ? new bool[sourceCells] : null;
            foreach (IntVec3 below in source.AllCells)
            {
                int index = source.cellIndices.CellToIndex(below);
                if (gravshipLinked)
                {
                    if (StrataGravshipUtility.CellOnGravship(source, below))
                    {
                        sourceSubstructure[index] = true;
                    }
                }
                else if (source.roofGrid.Roofed(below))
                {
                    sourceRoofs[index] = true;
                }
            }

            foreach (IntVec3 cell in upper.AllCells)
            {
                upper.roofGrid.SetRoof(cell, null);
                IntVec3 below = StrataMapUtility.ProportionalCell(cell, upper, source);
                bool supported = below.InBounds(source)
                    && (gravshipLinked
                        ? sourceSubstructure[source.cellIndices.CellToIndex(below)]
                        : sourceRoofs[source.cellIndices.CellToIndex(below)]);
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
        public static void SyncCell(Map upper, IntVec3 upperCell, bool allowShrink = true)
        {
            if (upper == null || !upperCell.InBounds(upper) || !StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }
            TerrainDef current = upperCell.GetTerrain(upper);
            bool supported = SourceSupportsUpperDeck(upper, upperCell);
            bool gravship = IsGravshipLinkedUpper(upper);

            if (supported)
            {
                if (current == OpenSky || current == null)
                {
                    // Land misalignment: don't seed empty silhouette islands far
                    // from the travelling room — only grow from existing deck.
                    if (gravship && !allowShrink && !AdjacentToRoofDeck(upper, upperCell)
                        && !CellHasPreservableStuff(upperCell, upper))
                    {
                        return;
                    }
                    upper.terrainGrid.SetTerrain(upperCell, RoofDeck);
                }
                return;
            }

            // Gravship land/sync: never convert travelling deck to open sky.
            if (!allowShrink || gravship)
            {
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
            bool gravship = IsGravshipLinkedUpper(upper);
            if (gravship)
            {
                Map source = SourceMapFor(upper);
                if (source != null
                    && !StrataGravshipUtility.EngineHasSubstructure(
                        StrataGravshipUtility.FindGravEngine(source)
                        ?? StrataGravshipUtility.FindGravEngineOnMap(source)))
                {
                    return;
                }
            }
            bool allowShrink = !gravship;
            foreach (IntVec3 cell in upper.AllCells)
            {
                SyncCell(upper, cell, allowShrink);
            }
            if (gravship)
            {
                RestoreRoofDeckUnderBuildings(upper);
                CleanupEmptySilhouetteIslands(upper);
            }
        }

        public static void SyncGravshipUpperDecksFromSource(Map source)
        {
            if (source == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            var seen = new HashSet<Map>();
            foreach (Thing thing in source.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                Map upper = portal.PocketMap;
                if (StrataMapUtility.IsUpperLevel(upper) && seen.Add(upper))
                {
                    SyncAllFromSource(upper);
                }
            }
            Map orphan = StrataGravshipOrphanLevels.FindAdoptableUpperDeck(source);
            if (orphan != null && seen.Add(orphan))
            {
                SyncAllFromSource(orphan);
            }
        }

        // Land paint can leave beds/shelves on OpenSky — restore walkable roof deck.
        public static void RestoreRoofDeckUnderBuildings(Map upper)
        {
            if (upper == null || !StrataMapUtility.IsUpperLevel(upper))
            {
                return;
            }
            TerrainDef deck = RoofDeck;
            int restored = 0;
            List<Thing> things = upper.listerThings?.AllThings;
            if (things == null)
            {
                return;
            }
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }
                if (thing.def.category != ThingCategory.Building
                    && thing.def.category != ThingCategory.Item
                    && thing is not Pawn)
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                foreach (IntVec3 cell in thing.OccupiedRect())
                {
                    if (!cell.InBounds(upper))
                    {
                        continue;
                    }
                    TerrainDef terrain = cell.GetTerrain(upper);
                    if (terrain?.defName == RoofDeckDefName)
                    {
                        continue;
                    }
                    // Heal open-sky / impassable gaps under travelling contents.
                    if (terrain != null
                        && terrain.defName != OpenSkyDefName
                        && terrain.passability != Traversability.Impassable)
                    {
                        continue;
                    }
                    upper.terrainGrid.SetTerrain(cell, deck);
                    restored++;
                }
            }
            if (restored > 0)
            {
                Log.Message("[Strata] Gravship upper deck: restored roof deck under "
                    + restored + " cell(s).");
            }
        }

        // Remove empty RoofDeck islands that aren't connected to landings/buildings
        // (leftover host-silhouette pads after a misaligned land).
        public static void CleanupEmptySilhouetteIslands(Map upper)
        {
            if (upper == null || !StrataMapUtility.IsUpperLevel(upper) || !IsGravshipLinkedUpper(upper))
            {
                return;
            }
            var keep = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();

            void Seed(IntVec3 cell)
            {
                if (!cell.InBounds(upper) || !keep.Add(cell))
                {
                    return;
                }
                if (cell.GetTerrain(upper)?.defName == RoofDeckDefName)
                {
                    queue.Enqueue(cell);
                }
            }

            List<Thing> things = upper.listerThings?.AllThings;
            if (things != null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || !thing.Spawned)
                    {
                        continue;
                    }
                    bool seed = thing is Pawn
                        || StrataGravshipUtility.IsGravshipLanding(thing)
                        || (thing.def.category == ThingCategory.Building
                            && thing.def.defName != StrataGravshipSubstructureSync.SubstructureDefName)
                        || thing.def.category == ThingCategory.Item;
                    if (!seed)
                    {
                        continue;
                    }
                    foreach (IntVec3 cell in thing.OccupiedRect())
                    {
                        Seed(cell);
                    }
                }
            }

            while (queue.Count > 0)
            {
                IntVec3 cell = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    IntVec3 next = cell + GenAdj.CardinalDirections[i];
                    if (!next.InBounds(upper) || keep.Contains(next))
                    {
                        continue;
                    }
                    if (next.GetTerrain(upper)?.defName != RoofDeckDefName)
                    {
                        continue;
                    }
                    keep.Add(next);
                    queue.Enqueue(next);
                }
            }

            int removed = 0;
            foreach (IntVec3 cell in upper.AllCells)
            {
                if (cell.GetTerrain(upper)?.defName != RoofDeckDefName || keep.Contains(cell))
                {
                    continue;
                }
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(upper, cell);
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                upper.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(cell);
                upper.terrainGrid.SetTerrain(cell, OpenSky);
                removed++;
            }
            if (removed > 0)
            {
                Log.Message("[Strata] Gravship upper deck: cleared " + removed
                    + " empty silhouette cell(s) not connected to the travelling room.");
            }
        }

        private static bool AdjacentToRoofDeck(Map map, IntVec3 cell)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 next = cell + GenAdj.CardinalDirections[i];
                if (next.InBounds(map) && next.GetTerrain(map)?.defName == RoofDeckDefName)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CellHasPreservableStuff(IntVec3 cell, Map map)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                if (thing.def.category == ThingCategory.Building
                    || thing.def.category == ThingCategory.Item
                    || thing is Pawn)
                {
                    return true;
                }
            }
            return false;
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
                IntVec3 upperCell = StrataGravshipUtility.IsGravshipLinkedLevel(upper)
                    ? c
                    : StrataMapUtility.ProportionalCell(c, source, upper);
                if (!upperCell.InBounds(upper))
                {
                    continue;
                }
                UpperDeckUtility.SyncCell(upper, upperCell);
            }
        }
    }
}
