using System.Collections.Generic;
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
        public const string DeferMineablesVar = "Strata_DeferMineables";

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

            // Priority: Biomes! Caverns (when loaded) > Strata native caves > solid rock.
            if (BiomesCavernsUtility.ShouldGenerateCavernLayout(map))
            {
                BiomeDef profile = BiomesCavernsUtility.PickProfileBiome(map);
                if (profile != null && BiomesCavernsUtility.TryGenerateCavernLayout(map, parms, profile))
                {
                    ArrivalZoneUtility.PrepareLandingZone(map, spot, clearRoof: true);
                    BiomesCavernsUtility.LogLayoutChoice(map, usedBiomes: true, profile);
                }
                else
                {
                    // Biomes failed or missing profile — fall back to Strata caves.
                    ClearGeneratedMap(map);
                    FillShell(map);
                    MapGenerator.SetVar(DeferMineablesVar, true);
                    ArrivalZoneUtility.PrepareLandingZone(map, spot, clearRoof: false);
                    MapGenerator.SetVar(StrataNativeCavernUtility.ForceNativeWarrenVar, true);
                    BiomesCavernsUtility.LogLayoutChoice(map, usedBiomes: false, profile);
                }
            }
            else
            {
                bool nativeCavern = StrataCavernUtility.ShouldGenerateNativeCavernLayout(map);
                if (nativeCavern)
                {
                    // Shell only; GenStep_CarveWarren spawns host rock outside the
                    // carve mask so chambers never pay GenSpawn + Destroy.
                    FillShell(map);
                    MapGenerator.SetVar(DeferMineablesVar, true);
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
            // Shell only during GetOtherMap — full GenSpawn is drained afterward
            // by StrataPocketMapOpen in a yielding LongEvent (275² freezes otherwise).
            FillShell(map);
            MapGenerator.SetVar(DeferMineablesVar, true);
            StrataRockFillScheduler.Schedule(map, skipCarveMask: null);
        }

        // Terrain + thick roof without mineables — used before native cavern
        // carving so we do not spawn rock that is immediately destroyed.
        // B2: rock indices via Parallel.For, then main-thread grid writes.
        internal static void FillShell(Map map)
        {
            List<ThingDef> rocks = StrataRockUtility.RocksForMap(map);
            byte[] rockIndices = StrataRockPlan.BuildRockIndices(map, rocks);
            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            try
            {
                int width = map.Size.x;
                foreach (IntVec3 cell in map.AllCells)
                {
                    int index = cell.z * width + cell.x;
                    ThingDef rockDef = StrataRockPlan.RockAtIndex(
                        rocks,
                        index >= 0 && index < rockIndices.Length ? rockIndices[index] : (byte)0);
                    TerrainDef floor = StrataRockUtility.NaturalFloorFor(rockDef);
                    map.terrainGrid.SetTerrain(cell, floor);
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
                }
            }
            finally
            {
                map.regionAndRoomUpdater.Enabled = regionsWereEnabled;
            }
        }

        // Full-map GenSpawn is the dig/ancient stair freeze with large mod lists
        // (tower decks skip this). Disable region rebuilds and use load-style
        // SpawnSetup so Harmony postfixes on live spawns do not run per cell.
        // skipCarveMask: when set, leave warren cells empty (already carved).
        // Prefer StrataRockFillScheduler.SpawnMineablesChunked from LongEvents.
        internal static void SpawnMineables(Map map, BoolGrid skipCarveMask = null)
        {
            List<ThingDef> rocks = StrataRockUtility.RocksForMap(map);
            byte[] rockIndices = StrataRockPlan.BuildRockIndices(map, rocks);
            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            try
            {
                int width = map.Size.x;
                foreach (IntVec3 cell in map.AllCells)
                {
                    if (skipCarveMask != null && skipCarveMask[cell])
                    {
                        continue;
                    }
                    if (cell.GetEdifice(map) != null)
                    {
                        continue;
                    }
                    int index = cell.z * width + cell.x;
                    ThingDef rockDef = StrataRockPlan.RockAtIndex(
                        rocks,
                        index >= 0 && index < rockIndices.Length ? rockIndices[index] : (byte)0);
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
    }
}
