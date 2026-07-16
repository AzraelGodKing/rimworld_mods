using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Fills a freshly opened upper level with an empty outdoor build pad: open
    // sky, walkable floor, no rock roof — solar and weather work like surface.
    public class GenStep_UpperPlatform : GenStep
    {
        private const int OuterRim = 5;
        private const int DeckRing = 10;
        private const float PlazaRadius = 8f;

        public override int SeedPart => 591837264;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            TerrainDef pad = TerrainDefOf.Concrete;
            TerrainDef deck = DefDatabase<TerrainDef>.GetNamedSilentFail("WoodPlankFloor")
                ?? TerrainDefOf.PavedTile;
            TerrainDef rim = TerrainDefOf.Gravel;

            foreach (IntVec3 cell in map.AllCells)
            {
                map.roofGrid.SetRoof(cell, null);
                int distEdge = DistanceToEdge(cell, map);
                if (distEdge < OuterRim)
                {
                    map.terrainGrid.SetTerrain(cell, rim);
                }
                else if (distEdge < DeckRing)
                {
                    map.terrainGrid.SetTerrain(cell, deck);
                }
                else
                {
                    map.terrainGrid.SetTerrain(cell, pad);
                }
            }

            // Clear plaza around the shaft landing so the arrival reads as a pad.
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(spot, PlazaRadius, useCenter: true))
            {
                if (cell.InBounds(map))
                {
                    map.terrainGrid.SetTerrain(cell, pad);
                }
            }

            MapGenerator.PlayerStartSpot = spot;
        }

        private static int DistanceToEdge(IntVec3 cell, Map map)
        {
            int dx = MathfMin(cell.x, map.Size.x - 1 - cell.x);
            int dz = MathfMin(cell.z, map.Size.z - 1 - cell.z);
            return dx < dz ? dx : dz;
        }

        private static int MathfMin(int a, int b) => a < b ? a : b;
    }
}
