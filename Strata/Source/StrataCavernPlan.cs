using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;

namespace Strata
{
    // Cave-system planner for native warren/vault digs. Geometry runs on a
    // worker from a deterministic mix of map.ConstantRandSeed + GenStep SeedPart
    // (same world/map seed → same cave). Main thread applies Destroy / floors.
    internal static partial class StrataCavernPlan
    {
        public static int MixCaveSeed(Map map, int seedPart, IntVec3 start)
        {
            if (map == null)
            {
                return seedPart;
            }
            int seed = map.ConstantRandSeed;
            seed = Gen.HashCombineInt(seed, seedPart);
            seed = Gen.HashCombineInt(seed, map.Size.x);
            seed = Gen.HashCombineInt(seed, map.Size.z);
            seed = Gen.HashCombineInt(seed, start.x);
            seed = Gen.HashCombineInt(seed, start.z);
            seed = Gen.HashCombineInt(seed, StrataDepth.CountLevelsBelowSurface(map));
            return seed;
        }

        public static Result RunOnWorker(Func<Result> plan)
        {
            Result result = default;
            Exception error = null;
            using (var done = new ManualResetEventSlim(false))
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        result = plan();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        done.Set();
                    }
                });
                done.Wait();
            }
            if (error != null)
            {
                throw error;
            }
            return result;
        }

        public static void CopyMaskToBoolGrid(bool[] mask, BoolGrid grid, Map map)
        {
            if (mask == null || grid == null || map == null)
            {
                return;
            }
            int width = map.Size.x;
            int height = map.Size.z;
            int len = Math.Min(mask.Length, width * height);
            for (int i = 0; i < len; i++)
            {
                if (!mask[i])
                {
                    continue;
                }
                int x = i % width;
                int z = i / width;
                var cell = new IntVec3(x, 0, z);
                if (cell.InBounds(map))
                {
                    grid[cell] = true;
                }
            }
        }

        public static void ApplyMineableDestroy(bool[] mask, Map map)
        {
            if (mask == null || map == null)
            {
                return;
            }
            int width = map.Size.x;
            int len = Math.Min(mask.Length, map.Size.x * map.Size.z);
            for (int i = 0; i < len; i++)
            {
                if (!mask[i])
                {
                    continue;
                }
                var cell = new IntVec3(i % width, 0, i / width);
                if (cell.InBounds(map))
                {
                    cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
