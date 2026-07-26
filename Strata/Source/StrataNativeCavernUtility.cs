using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Native cavern dressing when Biomes! Caverns is not driving the dig level:
    // mixed chamber floors + deferred flora / chunk scatter.
    public static class StrataNativeCavernUtility
    {
        public const string ForceNativeWarrenVar = "Strata_ForceNativeWarren";

        private static readonly string[] PlantCandidates =
        {
            "Plant_Bryolux",
            "Plant_Agarilux",
            "Plant_Glowstool",
        };

        private static readonly string[] ChunkSuffixes =
        {
            "ChunkMarble",
            "ChunkSlate",
            "ChunkGranite",
            "ChunkLimestone",
            "ChunkSandstone",
        };

        public static void PaintCarvedFloors(Map map, BoolGrid carveMask = null)
        {
            if (map == null)
            {
                return;
            }

            int depth = StrataDepth.CountLevelsBelowSurface(map);
            List<ThingDef> rocks = StrataRockUtility.RocksForMap(map);
            IntVec3 start = MapGenerator.PlayerStartSpot;

            if (carveMask != null)
            {
                // Fast path: only warren cells (not the full rock mass).
                for (int i = 0; i < map.cellIndices.NumGridCells; i++)
                {
                    if (!carveMask[i])
                    {
                        continue;
                    }
                    IntVec3 cell = map.cellIndices.IndexToCell(i);
                    if (cell.GetEdifice(map) != null || cell.GetFirstMineable(map) != null)
                    {
                        continue;
                    }
                    TerrainDef floor = PickCarvedFloor(cell, depth, rocks, start, map);
                    if (floor != null)
                    {
                        map.terrainGrid.SetTerrain(cell, floor);
                    }
                }
                return;
            }

            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.GetEdifice(map) != null || cell.GetFirstMineable(map) != null)
                {
                    continue;
                }

                TerrainDef floor = PickCarvedFloor(cell, depth, rocks, start, map);
                if (floor != null)
                {
                    map.terrainGrid.SetTerrain(cell, floor);
                }
            }
        }

        private static TerrainDef PickCarvedFloor(
            IntVec3 cell,
            int depth,
            List<ThingDef> rocks,
            IntVec3 start,
            Map map)
        {
            if (start.IsValid && cell.DistanceTo(start) <= ArrivalZoneUtility.ChamberRadius)
            {
                return StrataRockUtility.NaturalFloorFor(StrataRockUtility.RockAt(map, cell, rocks));
            }

            float roll = Rand.Value;
            if (depth >= 3 && roll < 0.04f)
            {
                return TerrainDefOf.WaterShallow;
            }
            if (roll < 0.18f)
            {
                return TerrainDefOf.Gravel;
            }
            if (roll < 0.32f)
            {
                return TerrainDefOf.Soil;
            }
            return StrataRockUtility.NaturalFloorFor(StrataRockUtility.RockAt(map, cell, rocks));
        }

        public static void ScatterFeatures(Map map)
        {
            if (map == null || map.Disposed)
            {
                return;
            }
            // BMT path owns flora when its biome is active.
            if (BiomesCavernsUtility.IsActive
                && BiomesCavernsUtility.IsStrataCavernBiome(map.Biome))
            {
                return;
            }

            int depth = Mathf.Max(1, StrataDepth.CountLevelsBelowSurface(map));
            int plantBudget = 8 + depth * 4;
            int chunkBudget = 6 + depth * 2;

            List<ThingDef> plants = ResolveDefs(PlantCandidates, ThingCategory.Plant);
            if (plants.Count > 0)
            {
                ScatterNearChambers(map, plants, plantBudget);
            }

            List<ThingDef> chunks = ResolveChunkDefs(map);
            if (chunks.Count > 0)
            {
                ScatterNearChambers(map, chunks, chunkBudget);
            }
        }

        private static List<ThingDef> ResolveDefs(string[] names, ThingCategory category)
        {
            var list = new List<ThingDef>();
            for (int i = 0; i < names.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(names[i]);
                if (def != null && def.category == category)
                {
                    list.Add(def);
                }
            }
            return list;
        }

        private static List<ThingDef> ResolveChunkDefs(Map map)
        {
            var chunks = new List<ThingDef>();
            List<ThingDef> rocks = StrataRockUtility.RocksForMap(map);
            for (int i = 0; i < rocks.Count; i++)
            {
                ThingDef rock = rocks[i];
                ThingDef chunk = rock.building?.mineableThing;
                if (chunk != null && chunk.category == ThingCategory.Item && !chunks.Contains(chunk))
                {
                    chunks.Add(chunk);
                }
            }
            if (chunks.Count == 0)
            {
                for (int i = 0; i < ChunkSuffixes.Length; i++)
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ChunkSuffixes[i]);
                    if (def != null)
                    {
                        chunks.Add(def);
                    }
                }
            }
            return chunks;
        }

        private static void ScatterNearChambers(Map map, List<ThingDef> defs, int budget)
        {
            MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers);
            int placed = 0;
            int attempts = 0;
            int maxAttempts = budget * 14;
            while (placed < budget && attempts++ < maxAttempts)
            {
                IntVec3 cell;
                if (chambers != null && chambers.Count > 0)
                {
                    IntVec3 center = chambers.RandomElement();
                    cell = center + new IntVec3(Rand.RangeInclusive(-6, 6), 0, Rand.RangeInclusive(-6, 6));
                }
                else
                {
                    cell = CellFinder.RandomCell(map);
                }

                if (!cell.InBounds(map)
                    || cell.GetEdifice(map) != null
                    || cell.GetFirstMineable(map) != null
                    || cell.GetPlant(map) != null)
                {
                    continue;
                }

                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain == null || terrain.IsWater || terrain.passability == Traversability.Impassable)
                {
                    continue;
                }

                ThingDef def = defs.RandomElement();
                try
                {
                    if (def.category == ThingCategory.Plant)
                    {
                        // Cave fungi often need darkness; skip if the plant refuses the cell.
                        if (def.plant != null && !def.CanEverPlantAt(cell, map))
                        {
                            continue;
                        }
                    }
                    Thing thing = ThingMaker.MakeThing(def);
                    GenSpawn.Spawn(thing, cell, map, WipeMode.Vanish);
                    placed++;
                }
                catch
                {
                    // Soft-fail exotic plant / chunk rules.
                }
            }
        }
    }
}
