using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Tile natural rocks + per-cell patch selection for dig levels (marble/slate/…).
    public static class StrataRockUtility
    {
        private const float PatchScale = 0.07f;

        // Performance mode: one rock type, no Perlin — FillShell/SpawnMineables
        // stay O(cells) but drop the noise cost on large maps.
        public static bool UseSimpleRockFill =>
            StrataMod.Settings?.performanceModeEnabled == true;

        public static Map SurfaceMapFor(Map map)
        {
            Map surface = map;
            int guard = 0;
            while (surface?.Parent is PocketMapParent pocket && pocket.sourceMap != null && guard++ < 32)
            {
                surface = pocket.sourceMap;
            }
            return surface;
        }

        public static List<ThingDef> RocksForMap(Map map)
        {
            Map surface = SurfaceMapFor(map);
            if (surface == null)
            {
                return new List<ThingDef> { ThingDefOf.Granite };
            }

            List<ThingDef> rocks = Find.World.NaturalRockTypesIn(surface.Tile).ToList();
            if (rocks.NullOrEmpty())
            {
                rocks = new List<ThingDef> { ThingDefOf.Granite };
            }
            return rocks;
        }

        public static ThingDef PrimaryRock(Map map)
        {
            List<ThingDef> rocks = RocksForMap(map);
            return rocks[0];
        }

        // Stable per-cell rock from the tile list — patches of marble/slate/etc.
        public static ThingDef RockAt(Map map, IntVec3 cell, List<ThingDef> rocks = null)
        {
            rocks ??= RocksForMap(map);
            if (rocks.Count == 1 || UseSimpleRockFill)
            {
                return rocks[0];
            }

            // ConstantRandSeed so rock patches match the map seed (not uniqueID).
            float n = Mathf.PerlinNoise(
                (cell.x + map.ConstantRandSeed * 0.17f) * PatchScale,
                (cell.z + map.ConstantRandSeed * 0.31f) * PatchScale);
            int idx = Mathf.Clamp(Mathf.FloorToInt(n * rocks.Count), 0, rocks.Count - 1);
            return rocks[idx];
        }

        public static TerrainDef NaturalFloorFor(ThingDef rock)
        {
            return rock?.building?.naturalTerrain ?? TerrainDefOf.Gravel;
        }
    }
}
