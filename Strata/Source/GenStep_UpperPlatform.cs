using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Fills a freshly opened upper level with an empty outdoor build pad: open
    // sky, walkable floor, no rock roof — solar and weather work like surface.
    public class GenStep_UpperPlatform : GenStep
    {
        public override int SeedPart => 591837264;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            TerrainDef floor = TerrainDefOf.Concrete;
            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, floor);
                map.roofGrid.SetRoof(cell, null);
            }

            // Soft edge: gravel rim so the pad reads as a built platform, not an
            // infinite concrete world. Landing stays concrete.
            int edge = 3;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.x < edge || cell.z < edge
                    || cell.x >= map.Size.x - edge || cell.z >= map.Size.z - edge)
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Gravel);
                }
            }

            MapGenerator.PlayerStartSpot = spot;
        }
    }
}
