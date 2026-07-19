using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Projects gravship substructure from the host deck onto linked A+/B+ pocket maps.
    public static class GravshipDeckUtility
    {
        public const string DeckDefName = "Strata_GravshipDeck";
        public const string HullDefName = "Strata_GravshipHull";

        private static TerrainDef deckTerrain;
        private static TerrainDef hullTerrain;

        public static TerrainDef DeckTerrain =>
            deckTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail(DeckDefName) ?? TerrainDefOf.Concrete;

        public static TerrainDef HullTerrain =>
            hullTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail(HullDefName) ?? TerrainDefOf.Gravel;

        public static bool IsManagedDeckTerrain(TerrainDef terrain)
        {
            return terrain != null
                && (terrain.defName == DeckDefName || terrain.defName == HullDefName);
        }

        public static bool IsWalkableDeckCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }
            if (StrataMapUtility.IsUpperLevel(map))
            {
                return UpperDeckUtility.SourceSupportsUpperDeck(map, cell);
            }
            TerrainDef terrain = cell.GetTerrain(map);
            return terrain?.defName == DeckDefName;
        }

        public static void PaintUnderdeck(Map under, IntVec3 landingSpot)
        {
            if (under == null || !StrataMapUtility.IsUnderground(under))
            {
                return;
            }
            Map host = UpperDeckUtility.SourceMapFor(under);
            PaintSubstructureFootprint(
                under,
                host,
                DeckTerrain,
                HullTerrain,
                ceilingOnDeck: true);
            // Exact ship silhouette — no radial plaza beyond ValidSubstructure.
            SeedStarterOxygen(under, landingSpot);
            TrySyncProjectedSubstructure(under);
        }

        public static void SyncUnderdecksFromHost(Map host)
        {
            if (host == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            var seen = new HashSet<Map>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                Map pocket = portal.PocketMap;
                if (!StrataMapUtility.IsUnderground(pocket) || !seen.Add(pocket))
                {
                    continue;
                }
                SyncOneUnderdeck(pocket, host);
            }
            // Orphan travelling underdeck (unwired after a bad land) still needs heal.
            Map orphan = StrataGravshipOrphanLevels.FindAdoptableUnderdeck(host);
            if (orphan != null && seen.Add(orphan))
            {
                SyncOneUnderdeck(orphan, host);
            }
        }

        private static void SyncOneUnderdeck(Map pocket, Map host)
        {
            // Grow-only: never rip travelling deck into impassable hull islands.
            PaintSubstructureFootprint(
                pocket,
                host,
                DeckTerrain,
                HullTerrain,
                ceilingOnDeck: true,
                preserveExistingDeck: true);
            RestoreDeckUnderBuildings(pocket);
        }

        public static void PaintSubstructureFootprint(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool ceilingOnDeck,
            bool preserveExistingDeck = false)
        {
            if (pocket == null)
            {
                return;
            }

            bool gravshipLinked = IsGravshipLinkedGeneration(pocket);
            // After ArriveNewMap, ValidSubstructure is often still empty. Painting
            // then treats every cell as off-deck and wipes the travelling footprint.
            // Use FindGravEngine (not OnMap-only) so Paint and SyncMap agree —
            // otherwise Sync alone seeds empty "New" silhouette islands.
            if (gravshipLinked && host != null
                && !StrataGravshipUtility.EngineHasSubstructure(
                    StrataGravshipUtility.FindGravEngine(host)
                    ?? StrataGravshipUtility.FindGravEngineOnMap(host)))
            {
                return;
            }
            if (host != null && pocket.Size == host.Size)
            {
                PaintSameSize(
                    pocket, host, deck, offDeck, gravshipLinked, ceilingOnDeck, preserveExistingDeck);
            }
            else if (host != null)
            {
                PaintProportional(
                    pocket, host, deck, offDeck, gravshipLinked, ceilingOnDeck, preserveExistingDeck);
            }
            else
            {
                foreach (IntVec3 cell in pocket.AllCells)
                {
                    pocket.terrainGrid.SetTerrain(cell, offDeck);
                    pocket.roofGrid.SetRoof(cell, null);
                }
            }
        }

        // Land misalignment can leave shelves/beds on impassable hull — heal those
        // cells back to walkable deck so the travelling room stays usable.
        public static void RestoreDeckUnderBuildings(Map pocket)
        {
            if (pocket == null || !StrataMapUtility.IsUnderground(pocket))
            {
                return;
            }
            TerrainDef deck = DeckTerrain;
            int restored = 0;
            List<Thing> things = pocket.listerThings?.AllThings;
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
                    if (!cell.InBounds(pocket))
                    {
                        continue;
                    }
                    TerrainDef terrain = cell.GetTerrain(pocket);
                    if (terrain?.defName != HullDefName)
                    {
                        continue;
                    }
                    pocket.terrainGrid.SetTerrain(cell, deck);
                    pocket.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                    restored++;
                }
            }
            if (restored > 0)
            {
                Log.Message("[Strata] Gravship underdeck: restored walkable deck under "
                    + restored + " cell(s) that were impassable hull.");
            }
        }

        private static void PaintSameSize(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool gravshipLinked,
            bool ceilingOnDeck,
            bool preserveExistingDeck)
        {
            foreach (IntVec3 cell in pocket.AllCells)
            {
                bool onShip = gravshipLinked && CellProjectsFromSubstructure(host, cell);
                ApplyDeckCell(
                    pocket, cell, onShip, deck, offDeck, ceilingOnDeck, preserveExistingDeck);
            }
        }

        private static void PaintProportional(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool gravshipLinked,
            bool ceilingOnDeck,
            bool preserveExistingDeck)
        {
            int hostCells = host.cellIndices.NumGridCells;
            var onShip = gravshipLinked ? new bool[hostCells] : null;
            if (onShip != null)
            {
                foreach (IntVec3 below in host.AllCells)
                {
                    if (CellProjectsFromSubstructure(host, below))
                    {
                        onShip[host.cellIndices.CellToIndex(below)] = true;
                    }
                }
            }

            foreach (IntVec3 cell in pocket.AllCells)
            {
                IntVec3 below = StrataMapUtility.ProportionalCell(cell, pocket, host);
                bool supported = onShip != null
                    && below.InBounds(host)
                    && onShip[host.cellIndices.CellToIndex(below)];
                ApplyDeckCell(
                    pocket, cell, supported, deck, offDeck, ceilingOnDeck, preserveExistingDeck);
            }
        }

        private static void ApplyDeckCell(
            Map pocket,
            IntVec3 cell,
            bool onShip,
            TerrainDef deck,
            TerrainDef offDeck,
            bool ceilingOnDeck,
            bool preserveExistingDeck)
        {
            cell.GetFirstMineable(pocket)?.Destroy(DestroyMode.Vanish);
            if (onShip)
            {
                pocket.terrainGrid.SetTerrain(cell, deck);
                if (ceilingOnDeck)
                {
                    pocket.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                }
                else
                {
                    pocket.roofGrid.SetRoof(cell, null);
                }
                return;
            }

            // Land/sync: keep existing walkable deck (and anything with stuff on it)
            // so a shifted/fragmented ValidSubstructure cannot strand rooms on hull.
            if (preserveExistingDeck)
            {
                TerrainDef current = cell.GetTerrain(pocket);
                if (current?.defName == DeckDefName || CellHasPreservableStuff(pocket, cell))
                {
                    if (current?.defName != DeckDefName)
                    {
                        pocket.terrainGrid.SetTerrain(cell, deck);
                    }
                    if (ceilingOnDeck)
                    {
                        pocket.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                    }
                    return;
                }
            }

            pocket.terrainGrid.SetTerrain(cell, offDeck);
            if (ceilingOnDeck)
            {
                pocket.roofGrid.SetRoof(cell, null);
            }
            else
            {
                pocket.roofGrid.SetRoof(cell, null);
            }
        }

        private static bool CellHasPreservableStuff(Map map, IntVec3 cell)
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

        private static bool CellProjectsFromSubstructure(Map host, IntVec3 cell)
        {
            return StrataGravshipUtility.CellOnGravship(host, cell);
        }

        private static bool IsGravshipLinkedGeneration(Map pocket)
        {
            if (pocket != null && StrataGravshipUtility.IsInGravshipStack(pocket))
            {
                return true;
            }
            return PocketMapUtility.currentlyGeneratingPortal is IStrataGravshipPortal;
        }

        private static void SeedStarterOxygen(Map map, IntVec3 spot)
        {
            if (!StrataDepth.IsStarterLevel(map))
            {
                return;
            }
            StrataGasDef oxygen = DefDatabase<StrataGasDef>.GetNamedSilentFail("Strata_Oxygen");
            if (oxygen != null)
            {
                AtmosphereMapComponent.QueueSeed(map, spot, oxygen, AtmosphereMapComponent.AmbientOxygen);
            }
        }

        private static void TrySyncProjectedSubstructure(Map pocket)
        {
            Map host = UpperDeckUtility.SourceMapFor(pocket);
            Building_GravEngine engine = host != null
                ? StrataGravshipUtility.FindGravEngineOnMap(host)
                : null;
            if (engine != null)
            {
                StrataGravshipSubstructureSync.SyncMap(pocket, host, engine);
            }
        }

        // Empty Gravship deck pads left after a misaligned land (no furniture/landing).
        public static void CleanupEmptySilhouetteIslands(Map under)
        {
            if (under == null || !StrataMapUtility.IsUnderground(under)
                || !StrataGravshipUtility.IsGravshipLinkedLevel(under))
            {
                return;
            }
            var keep = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();

            void Seed(IntVec3 cell)
            {
                if (!cell.InBounds(under) || !keep.Add(cell))
                {
                    return;
                }
                if (cell.GetTerrain(under)?.defName == DeckDefName)
                {
                    queue.Enqueue(cell);
                }
            }

            List<Thing> things = under.listerThings?.AllThings;
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
                    if (!next.InBounds(under) || keep.Contains(next))
                    {
                        continue;
                    }
                    if (next.GetTerrain(under)?.defName != DeckDefName)
                    {
                        continue;
                    }
                    keep.Add(next);
                    queue.Enqueue(next);
                }
            }

            int removed = 0;
            TerrainDef hull = HullTerrain;
            foreach (IntVec3 cell in under.AllCells)
            {
                if (cell.GetTerrain(under)?.defName != DeckDefName || keep.Contains(cell))
                {
                    continue;
                }
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(under, cell);
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                under.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(cell);
                under.terrainGrid.SetTerrain(cell, hull);
                under.roofGrid.SetRoof(cell, null);
                removed++;
            }
            if (removed > 0)
            {
                Log.Message("[Strata] Gravship underdeck: cleared " + removed
                    + " empty silhouette cell(s) not connected to the travelling room.");
            }
        }
    }
}
