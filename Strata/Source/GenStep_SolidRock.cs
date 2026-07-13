using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Fills a freshly opened underground level with solid mineable rock under a
    // thick rock roof, then carves a small arrival chamber in the middle for the
    // stairwell landing (GenStep_PlaceLevelExit spawns the stairs there afterwards).
    public class GenStep_SolidRock : GenStep
    {
        private const float ChamberRadius = 8f;

        public override int SeedPart => 762303921;

        public override void Generate(Map map, GenStepParams parms)
        {
            ThingDef rockDef = RockForMap(map);
            TerrainDef floor = rockDef.building?.naturalTerrain ?? TerrainDefOf.Gravel;

            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, floor);
                GenSpawn.Spawn(rockDef, cell, map);
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
            }

            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(spot, ChamberRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
            }

            MapGenerator.PlayerStartSpot = spot;
        }

        private static ThingDef RockForMap(Map map)
        {
            // Walk up the pocket map chain to the real surface map so deeper
            // levels keep the same rock as the tile they sit under.
            Map surface = map;
            int guard = 0;
            while (surface.Parent is PocketMapParent pocket && pocket.sourceMap != null && guard++ < 32)
            {
                surface = pocket.sourceMap;
            }

            List<ThingDef> rocks = Find.World.NaturalRockTypesIn(surface.Tile).ToList();
            if (!rocks.NullOrEmpty())
            {
                return rocks.RandomElement();
            }
            return ThingDefOf.Granite;
        }
    }
}
