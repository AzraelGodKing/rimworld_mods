using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // B1/B2: Perlin rock-index grid on worker threads. Main thread resolves
    // ThingDef/TerrainDef and writes map state.
    internal static class StrataRockPlan
    {
        private const float PatchScale = 0.07f;

        // byte rock index per cell (into RocksForMap list).
        public static byte[] BuildRockIndices(Map map, List<ThingDef> rocks)
        {
            if (map == null)
            {
                return Array.Empty<byte>();
            }
            rocks ??= StrataRockUtility.RocksForMap(map);
            int width = map.Size.x;
            int height = map.Size.z;
            int cells = width * height;
            var indices = new byte[cells];
            if (rocks.Count <= 1 || StrataRockUtility.UseSimpleRockFill)
            {
                return indices;
            }

            int mapId = map.ConstantRandSeed;
            int rockCount = rocks.Count;
            Parallel.For(0, height, z =>
            {
                int row = z * width;
                for (int x = 0; x < width; x++)
                {
                    float n = Mathf.PerlinNoise(
                        (x + mapId * 0.17f) * PatchScale,
                        (z + mapId * 0.31f) * PatchScale);
                    int idx = Mathf.Clamp(Mathf.FloorToInt(n * rockCount), 0, rockCount - 1);
                    indices[row + x] = (byte)idx;
                }
            });
            return indices;
        }

        public static ThingDef RockAtIndex(List<ThingDef> rocks, byte index)
        {
            if (rocks == null || rocks.Count == 0)
            {
                return ThingDefOf.Granite;
            }
            if (index >= rocks.Count)
            {
                return rocks[0];
            }
            return rocks[index];
        }
    }
}
