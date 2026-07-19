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
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                Map pocket = portal.PocketMap;
                if (!StrataMapUtility.IsUnderground(pocket))
                {
                    continue;
                }
                PaintSubstructureFootprint(
                    pocket,
                    host,
                    DeckTerrain,
                    HullTerrain,
                    ceilingOnDeck: true);
            }
        }

        public static void PaintSubstructureFootprint(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool ceilingOnDeck)
        {
            if (pocket == null)
            {
                return;
            }

            bool gravshipLinked = IsGravshipLinkedGeneration(pocket);
            if (host != null && pocket.Size == host.Size)
            {
                PaintSameSize(pocket, host, deck, offDeck, gravshipLinked, ceilingOnDeck);
            }
            else if (host != null)
            {
                PaintProportional(pocket, host, deck, offDeck, gravshipLinked, ceilingOnDeck);
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

        private static void PaintSameSize(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool gravshipLinked,
            bool ceilingOnDeck)
        {
            foreach (IntVec3 cell in pocket.AllCells)
            {
                bool onShip = gravshipLinked && CellProjectsFromSubstructure(host, cell);
                ApplyDeckCell(pocket, cell, onShip, deck, offDeck, ceilingOnDeck);
            }
        }

        private static void PaintProportional(
            Map pocket,
            Map host,
            TerrainDef deck,
            TerrainDef offDeck,
            bool gravshipLinked,
            bool ceilingOnDeck)
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
                ApplyDeckCell(pocket, cell, supported, deck, offDeck, ceilingOnDeck);
            }
        }

        private static void ApplyDeckCell(
            Map pocket,
            IntVec3 cell,
            bool onShip,
            TerrainDef deck,
            TerrainDef offDeck,
            bool ceilingOnDeck)
        {
            cell.GetFirstMineable(pocket)?.Destroy(DestroyMode.Vanish);
            pocket.terrainGrid.SetTerrain(cell, onShip ? deck : offDeck);
            if (ceilingOnDeck)
            {
                pocket.roofGrid.SetRoof(cell, onShip ? RoofDefOf.RoofConstructed : null);
            }
            else
            {
                pocket.roofGrid.SetRoof(cell, null);
            }
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
    }
}
