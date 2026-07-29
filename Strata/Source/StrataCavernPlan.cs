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
    internal static class StrataCavernPlan
    {
        public struct Result
        {
            public bool[] carveMask;
            public List<IntVec3> chambers;
        }

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

        // Organic cave network: trunk + branches + dead-end spurs, variable-width
        // tunnels, irregular chambers. ChambersVar keeps major rooms for flora/ore.
        public static Result PlanWarren(
            int width,
            int height,
            IntVec3 start,
            int targetChambers,
            float arrivalRadius,
            float minChamberRadius,
            float maxChamberRadius,
            int edgeMargin,
            int seed)
        {
            var rng = new System.Random(seed);
            var mask = new bool[width * height];
            var chambers = new List<IntVec3> { start };
            float mapScale = Mathf.Clamp(
                Mathf.Sqrt((width * height) / (250f * 250f)), 0.75f, 1.6f);

            MarkChamber(mask, width, height, start, arrivalRadius, rng, stretch: 0.15f);

            // Prefer one outward heading so the network snakes away from the stairs.
            float preferAngle = (float)(rng.NextDouble() * Math.PI * 2.0);
            int trunkTarget = Mathf.Max(3, Mathf.RoundToInt(targetChambers * 0.65f * mapScale));
            GrowChain(
                rng, mask, width, height, chambers, start, trunkTarget, preferAngle,
                edgeMargin, minSep: 11f, minDist: 14f * mapScale, maxDist: 28f * mapScale,
                minChamberRadius, maxChamberRadius, angleJitter: 55f, trunk: true);

            int branchTarget = Mathf.Max(1, Mathf.RoundToInt(targetChambers * 0.45f * mapScale));
            GrowBranches(
                rng, mask, width, height, chambers, branchTarget, edgeMargin,
                minChamberRadius, maxChamberRadius, mapScale);

            int spurCount = Mathf.Max(2, Mathf.RoundToInt((2 + targetChambers * 0.55f) * mapScale));
            GrowSpurs(rng, mask, width, height, chambers, spurCount, edgeMargin, mapScale);

            TryAddLoops(rng, mask, width, height, chambers, edgeMargin, mapScale);

            return new Result { carveMask = mask, chambers = chambers };
        }

        public static Result PlanVault(
            int width,
            int height,
            IntVec3 start,
            int targetChambers,
            float minChamberRadius,
            float maxChamberRadius,
            int edgeMargin,
            int seed)
        {
            var rng = new System.Random(seed);
            var mask = new bool[width * height];
            var chambers = new List<IntVec3> { start };
            MarkChamber(mask, width, height, start, minChamberRadius + 0.8f, rng, stretch: 0.08f);

            float preferAngle = (float)(rng.NextDouble() * Math.PI * 2.0);
            GrowChain(
                rng, mask, width, height, chambers, start, targetChambers, preferAngle,
                edgeMargin, minSep: 15f, minDist: 18f, maxDist: 34f,
                minChamberRadius, maxChamberRadius, angleJitter: 25f, trunk: true);

            // One optional spur so sealed vaults aren't a pure hallway.
            if (rng.NextDouble() < 0.55)
            {
                GrowSpurs(rng, mask, width, height, chambers, 1, edgeMargin, mapScale: 1f);
            }

            return new Result { carveMask = mask, chambers = chambers };
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

        private static void GrowChain(
            System.Random rng,
            bool[] mask,
            int width,
            int height,
            List<IntVec3> chambers,
            IntVec3 from,
            int targetCount,
            float preferAngleRad,
            int edgeMargin,
            float minSep,
            float minDist,
            float maxDist,
            float minChamberRadius,
            float maxChamberRadius,
            float angleJitter,
            bool trunk)
        {
            IntVec3 tip = from;
            int attempts = 0;
            int made = 0;
            while (made < targetCount && attempts++ < targetCount * 12)
            {
                float angle = preferAngleRad
                    + ((float)rng.NextDouble() * 2f - 1f) * (angleJitter * Mathf.Deg2Rad);
                float dist = Lerp(minDist, maxDist, (float)rng.NextDouble());
                IntVec3 next = tip + new IntVec3(
                    Mathf.RoundToInt(Mathf.Cos(angle) * dist),
                    0,
                    Mathf.RoundToInt(Mathf.Sin(angle) * dist));
                if (!IsValidChamberSpot(next, width, height, chambers, edgeMargin, minSep))
                {
                    // Nudge prefer angle and retry from tip.
                    preferAngleRad += ((float)rng.NextDouble() * 2f - 1f) * 0.7f;
                    continue;
                }

                MarkOrganicTunnel(mask, width, height, tip, next, rng, trunk);
                float radius = Lerp(minChamberRadius, maxChamberRadius, (float)rng.NextDouble());
                MarkChamber(mask, width, height, next, radius, rng, stretch: trunk ? 0.22f : 0.18f);
                chambers.Add(next);
                tip = next;
                preferAngleRad = angle;
                made++;
            }
        }

        private static void GrowBranches(
            System.Random rng,
            bool[] mask,
            int width,
            int height,
            List<IntVec3> chambers,
            int branchTarget,
            int edgeMargin,
            float minChamberRadius,
            float maxChamberRadius,
            float mapScale)
        {
            int made = 0;
            int attempts = 0;
            while (made < branchTarget && attempts++ < branchTarget * 10 && chambers.Count > 1)
            {
                IntVec3 anchor = chambers[rng.Next(1, chambers.Count)];
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float dist = Lerp(12f * mapScale, 22f * mapScale, (float)rng.NextDouble());
                IntVec3 next = anchor + new IntVec3(
                    Mathf.RoundToInt(Mathf.Cos(angle) * dist),
                    0,
                    Mathf.RoundToInt(Mathf.Sin(angle) * dist));
                if (!IsValidChamberSpot(next, width, height, chambers, edgeMargin, 10f))
                {
                    continue;
                }
                MarkOrganicTunnel(mask, width, height, anchor, next, rng, trunk: false);
                float radius = Lerp(minChamberRadius * 0.85f, maxChamberRadius * 0.95f, (float)rng.NextDouble());
                MarkChamber(mask, width, height, next, radius, rng, stretch: 0.2f);
                chambers.Add(next);

                // Sometimes a second hop on the same branch.
                if (rng.NextDouble() < 0.4)
                {
                    IntVec3 tip = next;
                    angle += ((float)rng.NextDouble() * 2f - 1f) * 0.9f;
                    dist = Lerp(10f * mapScale, 18f * mapScale, (float)rng.NextDouble());
                    next = tip + new IntVec3(
                        Mathf.RoundToInt(Mathf.Cos(angle) * dist),
                        0,
                        Mathf.RoundToInt(Mathf.Sin(angle) * dist));
                    if (IsValidChamberSpot(next, width, height, chambers, edgeMargin, 10f))
                    {
                        MarkOrganicTunnel(mask, width, height, tip, next, rng, trunk: false);
                        radius = Lerp(minChamberRadius * 0.8f, maxChamberRadius * 0.9f, (float)rng.NextDouble());
                        MarkChamber(mask, width, height, next, radius, rng, stretch: 0.18f);
                        chambers.Add(next);
                    }
                }
                made++;
            }
        }

        private static void GrowSpurs(
            System.Random rng,
            bool[] mask,
            int width,
            int height,
            List<IntVec3> chambers,
            int spurCount,
            int edgeMargin,
            float mapScale)
        {
            int made = 0;
            int attempts = 0;
            while (made < spurCount && attempts++ < spurCount * 14 && chambers.Count > 0)
            {
                IntVec3 anchor = chambers[rng.Next(chambers.Count)];
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float dist = Lerp(6f * mapScale, 12f * mapScale, (float)rng.NextDouble());
                IntVec3 tip = anchor + new IntVec3(
                    Mathf.RoundToInt(Mathf.Cos(angle) * dist),
                    0,
                    Mathf.RoundToInt(Mathf.Sin(angle) * dist));
                if (tip.x < edgeMargin || tip.x >= width - edgeMargin
                    || tip.z < edgeMargin || tip.z >= height - edgeMargin)
                {
                    continue;
                }
                // Dead-end corridor — pinch then small alcove (not a full chamber listing).
                MarkOrganicTunnel(mask, width, height, anchor, tip, rng, trunk: false, minRadius: 1.1f, maxRadius: 1.7f);
                float alcove = Lerp(2.2f, 3.4f, (float)rng.NextDouble());
                MarkChamber(mask, width, height, tip, alcove, rng, stretch: 0.25f);
                made++;
            }
        }

        private static void TryAddLoops(
            System.Random rng,
            bool[] mask,
            int width,
            int height,
            List<IntVec3> chambers,
            int edgeMargin,
            float mapScale)
        {
            if (chambers.Count < 4 || rng.NextDouble() > 0.45)
            {
                return;
            }
            int loops = rng.NextDouble() < 0.3 ? 2 : 1;
            for (int n = 0; n < loops; n++)
            {
                IntVec3 a = chambers[rng.Next(chambers.Count)];
                IntVec3 best = IntVec3.Invalid;
                float bestDist = float.MaxValue;
                for (int i = 0; i < chambers.Count; i++)
                {
                    IntVec3 b = chambers[i];
                    if (b == a)
                    {
                        continue;
                    }
                    float d = a.DistanceTo(b);
                    if (d < 14f * mapScale || d > 32f * mapScale)
                    {
                        continue;
                    }
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = b;
                    }
                }
                if (best.IsValid)
                {
                    MarkOrganicTunnel(mask, width, height, a, best, rng, trunk: false, minRadius: 1.2f, maxRadius: 1.9f);
                }
            }
        }

        private static bool IsValidChamberSpot(
            IntVec3 candidate,
            int width,
            int height,
            List<IntVec3> existing,
            int edgeMargin,
            float minSeparation)
        {
            if (candidate.x < 0 || candidate.x >= width
                || candidate.z < 0 || candidate.z >= height)
            {
                return false;
            }
            if (DistanceToEdge(candidate, width, height) < edgeMargin)
            {
                return false;
            }
            for (int j = 0; j < existing.Count; j++)
            {
                if (candidate.DistanceTo(existing[j]) < minSeparation)
                {
                    return false;
                }
            }
            return true;
        }

        private static int DistanceToEdge(IntVec3 cell, int width, int height)
        {
            return Mathf.Min(
                Mathf.Min(cell.x, width - 1 - cell.x),
                Mathf.Min(cell.z, height - 1 - cell.z));
        }

        // Multi-bend corridor with width that breathes along the path.
        private static void MarkOrganicTunnel(
            bool[] mask,
            int width,
            int height,
            IntVec3 from,
            IntVec3 to,
            System.Random rng,
            bool trunk,
            float minRadius = -1f,
            float maxRadius = -1f)
        {
            if (minRadius < 0f)
            {
                minRadius = trunk ? 1.35f : 1.15f;
            }
            if (maxRadius < 0f)
            {
                maxRadius = trunk ? 2.45f : 1.95f;
            }

            int bends = trunk ? rng.Next(2, 4) : rng.Next(1, 3);
            var points = new List<IntVec3>(bends + 2) { from };
            for (int i = 1; i <= bends; i++)
            {
                float t = i / (float)(bends + 1);
                IntVec3 lerp = LerpCell(from, to, t);
                int jitter = trunk ? 7 : 5;
                var mid = new IntVec3(
                    lerp.x + rng.Next(-jitter, jitter + 1),
                    0,
                    lerp.z + rng.Next(-jitter, jitter + 1));
                if (mid.x < 1)
                {
                    mid.x = 1;
                }
                if (mid.z < 1)
                {
                    mid.z = 1;
                }
                if (mid.x >= width - 1)
                {
                    mid.x = width - 2;
                }
                if (mid.z >= height - 1)
                {
                    mid.z = height - 2;
                }
                points.Add(mid);
            }
            points.Add(to);

            float phase = (float)(rng.NextDouble() * Math.PI * 2.0);
            int stepIndex = 0;
            for (int s = 0; s < points.Count - 1; s++)
            {
                foreach (IntVec3 cell in PointsOnLine(points[s], points[s + 1]))
                {
                    float wave = 0.5f + 0.5f * Mathf.Sin(stepIndex * 0.35f + phase);
                    float noise = (float)rng.NextDouble() * 0.35f;
                    float radius = Lerp(minRadius, maxRadius, Mathf.Clamp01(wave * 0.75f + noise));
                    // Occasional pinch / flare.
                    if (rng.NextDouble() < 0.08)
                    {
                        radius *= rng.NextDouble() < 0.5 ? 0.7f : 1.35f;
                    }
                    MarkCircle(mask, width, height, cell, radius);
                    stepIndex++;
                }
            }
        }

        private static IntVec3 LerpCell(IntVec3 a, IntVec3 b, float t)
        {
            return new IntVec3(
                Mathf.RoundToInt(a.x + (b.x - a.x) * t),
                0,
                Mathf.RoundToInt(a.z + (b.z - a.z) * t));
        }

        // Irregular room: elliptical base + radial noise (not a perfect disc).
        private static void MarkChamber(
            bool[] mask,
            int width,
            int height,
            IntVec3 center,
            float radius,
            System.Random rng,
            float stretch)
        {
            float rx = radius * (1f + stretch * ((float)rng.NextDouble() * 2f - 1f));
            float rz = radius * (1f + stretch * ((float)rng.NextDouble() * 2f - 1f));
            rx = Mathf.Max(2.2f, rx);
            rz = Mathf.Max(2.2f, rz);
            float phase = (float)(rng.NextDouble() * Math.PI * 2.0);
            int rMax = Mathf.CeilToInt(Mathf.Max(rx, rz) * 1.35f);
            for (int dz = -rMax; dz <= rMax; dz++)
            {
                for (int dx = -rMax; dx <= rMax; dx++)
                {
                    float angle = Mathf.Atan2(dz, dx);
                    float edge = 1f + 0.18f * Mathf.Sin(angle * 3f + phase)
                        + 0.1f * Mathf.Sin(angle * 5f - phase);
                    float nx = dx / (rx * edge);
                    float nz = dz / (rz * edge);
                    if (nx * nx + nz * nz > 1f)
                    {
                        continue;
                    }
                    int x = center.x + dx;
                    int z = center.z + dz;
                    if (x < 0 || x >= width || z < 0 || z >= height)
                    {
                        continue;
                    }
                    mask[z * width + x] = true;
                }
            }
        }

        private static void MarkCircle(bool[] mask, int width, int height, IntVec3 center, float radius)
        {
            int r = Mathf.CeilToInt(radius);
            float rSq = radius * radius;
            for (int dz = -r; dz <= r; dz++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dz * dz > rSq)
                    {
                        continue;
                    }
                    int x = center.x + dx;
                    int z = center.z + dz;
                    if (x < 0 || x >= width || z < 0 || z >= height)
                    {
                        continue;
                    }
                    mask[z * width + x] = true;
                }
            }
        }

        private static IEnumerable<IntVec3> PointsOnLine(IntVec3 from, IntVec3 to)
        {
            int x0 = from.x;
            int z0 = from.z;
            int x1 = to.x;
            int z1 = to.z;
            int dx = Mathf.Abs(x1 - x0);
            int dz = Mathf.Abs(z1 - z0);
            int sx = x0 < x1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;
            int err = dx - dz;
            while (true)
            {
                yield return new IntVec3(x0, 0, z0);
                if (x0 == x1 && z0 == z1)
                {
                    yield break;
                }
                int e2 = 2 * err;
                if (e2 > -dz)
                {
                    err -= dz;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    z0 += sz;
                }
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
