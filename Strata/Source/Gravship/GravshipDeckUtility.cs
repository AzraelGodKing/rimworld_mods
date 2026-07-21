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
        private static TerrainDef voidTerrain;

        public static TerrainDef DeckTerrain =>
            deckTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail(DeckDefName) ?? TerrainDefOf.Concrete;

        public static TerrainDef HullTerrain =>
            hullTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail(HullDefName) ?? TerrainDefOf.Gravel;

        // MultiFloors-style: far off-pad is not GravshipHull (that looked like a second
        // ghost pad). Prefer Odyssey Space, else impassable rock.
        public static TerrainDef VoidTerrain =>
            voidTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("Space")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("Marble")
                ?? TerrainDefOf.Gravel;

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
            RestoreDeckUnderBuildings(pocket, host);
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
        // Only on the live host footprint — restoring under left-behind walls
        // recreates the orphan "ghost pad" beside the real deck.
        public static void RestoreDeckUnderBuildings(Map pocket, Map host = null)
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
            bool sameSize = host != null && pocket.Size == host.Size;
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
                    if (sameSize && !StrataGravshipUtility.CellOnGravship(host, cell))
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

        // Walls/items that failed the land shift sit on the old hull silhouette —
        // pull them onto the live pad so Restore/Cleanup cannot keep a ghost island.
        public static int PullStragglersOntoFootprint(Map pocket, Map host)
        {
            if (pocket == null || host == null || pocket.Size != host.Size
                || !StrataGravshipUtility.EngineHasSubstructure(
                    StrataGravshipUtility.FindGravEngineOnMap(host)))
            {
                return 0;
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
            HashSet<IntVec3> deck = engine.ValidSubstructure;
            if (deck == null || deck.Count == 0)
            {
                deck = engine.AllConnectedSubstructure;
            }
            if (deck == null || deck.Count == 0)
            {
                return 0;
            }

            IntVec3 anchor = IntVec3.Invalid;
            foreach (Thing t in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (StrataGravshipUtility.IsGravshipLanding(t))
                {
                    anchor = t.Position;
                    break;
                }
            }
            if (!anchor.IsValid)
            {
                foreach (IntVec3 cell in deck)
                {
                    anchor = cell;
                    break;
                }
            }

            var all = new List<Thing>(pocket.listerThings.AllThings);
            int moved = 0;
            for (int i = 0; i < all.Count; i++)
            {
                Thing thing = all[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing is Pawn)
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                if (thing.def.category != ThingCategory.Building
                    && thing.def.category != ThingCategory.Item)
                {
                    continue;
                }
                if (StrataGravshipUtility.CellOnGravship(host, thing.Position))
                {
                    continue;
                }

                IntVec3 dest = NearestDeckCell(thing.Position, deck, anchor);
                if (!dest.IsValid)
                {
                    continue;
                }

                Rot4 rot = thing.Rotation;
                thing.DeSpawn(DestroyMode.WillReplace);
                if (GenSpawn.Spawn(thing, dest, pocket, rot, WipeMode.Vanish) != null)
                {
                    moved++;
                    continue;
                }
                if (CellFinder.TryFindRandomCellNear(
                        dest,
                        pocket,
                        8,
                        c => c.InBounds(pocket) && deck.Contains(c)
                            && GenConstruct.CanPlaceBlueprintAt(thing.def, c, rot, pocket).Accepted,
                        out IntVec3 near)
                    && GenSpawn.Spawn(thing, near, pocket, rot, WipeMode.Vanish) != null)
                {
                    moved++;
                    continue;
                }
                if (!thing.Spawned)
                {
                    // Do not leave unspawned things; prefer vanish over a ghost pad.
                    if (thing.def.category == ThingCategory.Building)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        GenSpawn.Spawn(thing, dest.ClampInsideMap(pocket), pocket, rot, WipeMode.Vanish);
                    }
                }
            }

            if (moved > 0)
            {
                Log.Message("[Strata] Gravship underdeck: pulled " + moved
                    + " straggler thing(s) onto the live ship footprint.");
            }
            return moved;
        }

        private static IntVec3 NearestDeckCell(IntVec3 from, HashSet<IntVec3> deck, IntVec3 prefer)
        {
            IntVec3 best = IntVec3.Invalid;
            int bestScore = int.MaxValue;
            foreach (IntVec3 cell in deck)
            {
                int score = (cell - from).LengthHorizontalSquared;
                if (prefer.IsValid)
                {
                    score += (cell - prefer).LengthHorizontalSquared / 4;
                }
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }
            return best;
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
            // MultiFloors lesson: footprint is engine ValidSubstructure only — deck on
            // the pad, optional 1-cell hull rim, void elsewhere (no ghost hull island).
            if (gravshipLinked && host != null)
            {
                PaintGravshipSilhouette(
                    pocket, host, deck, offDeck, ceilingOnDeck, preserveExistingDeck);
                return;
            }

            foreach (IntVec3 cell in pocket.AllCells)
            {
                bool onShip = CellProjectsFromSubstructure(host, cell);
                ApplyDeckCell(
                    pocket, cell, onShip, deck, offDeck, ceilingOnDeck, preserveExistingDeck);
            }
        }

        private static void PaintGravshipSilhouette(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool ceilingOnDeck,
            bool preserveExistingDeck)
        {
            var onShip = new HashSet<IntVec3>();
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host)
                ?? StrataGravshipUtility.FindGravEngine(host);
            HashSet<IntVec3> sub = engine?.ValidSubstructure;
            if (sub == null || sub.Count == 0)
            {
                sub = engine?.AllConnectedSubstructure;
            }
            if (sub != null)
            {
                foreach (IntVec3 hostCell in sub)
                {
                    if (hostCell.InBounds(pocket))
                    {
                        onShip.Add(hostCell);
                    }
                }
            }
            else
            {
                foreach (IntVec3 cell in pocket.AllCells)
                {
                    if (CellProjectsFromSubstructure(host, cell))
                    {
                        onShip.Add(cell);
                    }
                }
            }

            var rim = new HashSet<IntVec3>();
            foreach (IntVec3 cell in onShip)
            {
                for (int i = 0; i < 4; i++)
                {
                    IntVec3 adj = cell + GenAdj.CardinalDirections[i];
                    if (adj.InBounds(pocket) && !onShip.Contains(adj))
                    {
                        rim.Add(adj);
                    }
                }
            }

            TerrainDef voidT = VoidTerrain;
            TerrainDef hull = offDeck ?? HullTerrain;

            if (!preserveExistingDeck)
            {
                foreach (IntVec3 cell in pocket.AllCells)
                {
                    TerrainDef cur = cell.GetTerrain(pocket);
                    if (!IsManagedDeckTerrain(cur) && cur != voidT)
                    {
                        continue;
                    }
                    if (onShip.Contains(cell) || rim.Contains(cell))
                    {
                        continue;
                    }
                    // Strip abandoned pad (MF DirtyDeckCells equivalent).
                    Thing subThing = StrataGravshipSubstructureSync.SubstructureAt(pocket, cell);
                    if (subThing != null && !subThing.Destroyed)
                    {
                        subThing.Destroy(DestroyMode.Vanish);
                    }
                    pocket.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(cell);
                    pocket.terrainGrid.SetTerrain(cell, voidT);
                    pocket.roofGrid.SetRoof(cell, null);
                }
            }

            foreach (IntVec3 cell in onShip)
            {
                cell.GetFirstMineable(pocket)?.Destroy(DestroyMode.Vanish);
                pocket.terrainGrid.SetTerrain(cell, deck);
                if (ceilingOnDeck)
                {
                    pocket.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                }
            }
            foreach (IntVec3 cell in rim)
            {
                if (preserveExistingDeck && CellHasPreservableStuff(pocket, cell))
                {
                    continue;
                }
                cell.GetFirstMineable(pocket)?.Destroy(DestroyMode.Vanish);
                pocket.terrainGrid.SetTerrain(cell, hull);
                pocket.roofGrid.SetRoof(cell, null);
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
        // When host is set, only seed from things on the live ship — left-behind
        // wall fragments must not preserve an orphan pad.
        public static void CleanupEmptySilhouetteIslands(Map under, Map host = null)
        {
            if (under == null || !StrataMapUtility.IsUnderground(under)
                || !StrataGravshipUtility.IsGravshipLinkedLevel(under))
            {
                return;
            }
            var keep = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();
            bool sameSize = host != null && under.Size == host.Size;

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
                    if (sameSize && !StrataGravshipUtility.CellOnGravship(host, thing.Position))
                    {
                        continue;
                    }
                    foreach (IntVec3 cell in thing.OccupiedRect())
                    {
                        Seed(cell);
                    }
                }
            }

            // Always keep the live host silhouette even if temporarily empty.
            if (sameSize)
            {
                Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
                HashSet<IntVec3> sub = engine?.ValidSubstructure;
                if (sub == null || sub.Count == 0)
                {
                    sub = engine?.AllConnectedSubstructure;
                }
                if (sub != null)
                {
                    foreach (IntVec3 cell in sub)
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
            TerrainDef voidT = VoidTerrain;
            foreach (IntVec3 cell in under.AllCells)
            {
                if (keep.Contains(cell))
                {
                    continue;
                }
                TerrainDef terrain = cell.GetTerrain(under);
                bool managed = terrain?.defName == DeckDefName || terrain?.defName == HullDefName;
                if (!managed)
                {
                    continue;
                }
                // Drop empty deck islands, and any managed ghost pad outside the live ship.
                bool offPad = sameSize && !StrataGravshipUtility.CellOnGravship(host, cell);
                if (!offPad && terrain?.defName != DeckDefName)
                {
                    continue;
                }
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(under, cell);
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                under.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(cell);
                under.terrainGrid.SetTerrain(cell, voidT);
                under.roofGrid.SetRoof(cell, null);
                removed++;
            }
            if (removed > 0)
            {
                Log.Message("[Strata] Gravship underdeck: cleared " + removed
                    + " empty/orphan silhouette cell(s) (MultiFloors-style pad only).");
            }
        }
    }
}
