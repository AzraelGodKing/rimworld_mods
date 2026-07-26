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
        public override int SeedPart => 762303921;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            if (entrance is IStrataGravshipPortal)
            {
                // Gravship underdecks use Strata_GravshipUnderdeckLevel instead.
                return;
            }
            IntVec3 spot = entrance?.Map != null
                ? StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map)
                : map.Center;

            if (BiomesCavernsUtility.ShouldGenerateCavernLayout(map)
                && BiomesCavernsUtility.PickProfileBiome(map) is BiomeDef profile)
            {
                if (BiomesCavernsUtility.TryGenerateCavernLayout(map, parms, profile))
                {
                    ArrivalZoneUtility.PrepareLandingZone(map, spot, clearRoof: true);
                }
                else
                {
                    ClearGeneratedMap(map);
                    FillSolidRock(map);
                    ArrivalZoneUtility.PrepareLandingZone(map, spot, clearRoof: false);
                }
            }
            else
            {
                bool nativeCavern = StrataCavernUtility.ShouldGenerateNativeCavernLayout(map);
                if (nativeCavern)
                {
                    // Shell + rock first; GenStep_CarveWarren empties chambers afterward.
                    FillShell(map);
                    SpawnMineables(map);
                }
                else
                {
                    FillSolidRock(map);
                }
                ArrivalZoneUtility.PrepareLandingZone(map, spot, clearRoof: false);
            }

            MapGenerator.PlayerStartSpot = spot;
            SeedStarterOxygen(map, spot);
        }

        private static void FillSolidRock(Map map)
        {
            FillShell(map);
            SpawnMineables(map);
        }

        // Terrain + thick roof without mineables — used before native cavern
        // carving so we do not spawn rock that is immediately destroyed.
        internal static void FillShell(Map map)
        {
            ThingDef rockDef = RockForMap(map);
            TerrainDef floor = rockDef.building?.naturalTerrain ?? TerrainDefOf.Gravel;

            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, floor);
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
            }
        }

        // Full-map GenSpawn is the dig/ancient stair freeze with large mod lists
        // (tower decks skip this). Disable region rebuilds and use load-style
        // SpawnSetup so Harmony postfixes on live spawns do not run per cell.
        internal static void SpawnMineables(Map map)
        {
            ThingDef rockDef = RockForMap(map);
            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            try
            {
                foreach (IntVec3 cell in map.AllCells)
                {
                    if (cell.GetEdifice(map) != null)
                    {
                        continue;
                    }
                    Thing rock = ThingMaker.MakeThing(rockDef);
                    // respawningAfterLoad:true skips live-spawn side effects that
                    // modded GenSpawn prefixes/postfixes amplify across 50k+ cells.
                    GenSpawn.Spawn(rock, cell, map, Rot4.North, WipeMode.Vanish,
                        respawningAfterLoad: true);
                }
            }
            finally
            {
                map.regionAndRoomUpdater.Enabled = regionsWereEnabled;
            }
        }

        private static void ClearGeneratedMap(Map map)
        {
            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            try
            {
                foreach (IntVec3 cell in map.AllCells)
                {
                    List<Thing> things = cell.GetThingList(map);
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        things[i].Destroy(DestroyMode.Vanish);
                    }
                    map.roofGrid.SetRoof(cell, null);
                }
            }
            finally
            {
                map.regionAndRoomUpdater.Enabled = regionsWereEnabled;
            }
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
