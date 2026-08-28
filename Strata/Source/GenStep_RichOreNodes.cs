using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Seeds each underground level with a few rare, large ore nodes buried in
    // the solid rock. Count stays low; size and richness grow with depth so B1
    // finds the occasional fat seam and deeper strata get motherlodes.
    public class GenStep_RichOreNodes : GenStep
    {
        private const int EdgeMargin = 10;
        private const float MinDistFromLanding = 18f;
        private const float MinDistBetween = 22f;
        private const int PlacementTries = 80;

        public override int SeedPart => 447291563;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!OreReveal.HasOres)
            {
                return;
            }

            int depth = Mathf.Max(1, StrataDepth.CountLevelsBelowSurface(map));
            int nodeCount = NodeCountForDepth(depth);
            var placed = new List<IntVec3>();
            int totalCells = 0;

            for (int i = 0; i < nodeCount; i++)
            {
                if (!TryFindNodeSpot(map, placed, out IntVec3 root))
                {
                    break;
                }
                int target = TargetCellsForDepth(depth);
                ThingDef ore = OreReveal.PickOreForDepth(depth);
                int n = OreReveal.PlaceRichNode(map, root, ore, target);
                if (n <= 0)
                {
                    continue;
                }
                placed.Add(root);
                totalCells += n;
            }

            if (Prefs.DevMode && placed.Count > 0)
            {
                StrataLog.Verbose($"[Strata] Rich ore nodes: {placed.Count} seams / {totalCells} cells at depth {depth}.");
            }
        }

        private static int NodeCountForDepth(int depth)
        {
            // Rare: usually one fat node on B1, a couple deeper.
            if (depth <= 1)
            {
                return Rand.Chance(0.62f) ? 1 : 2;
            }
            if (depth == 2)
            {
                return Rand.RangeInclusive(1, 3);
            }
            return Rand.RangeInclusive(2, Mathf.Min(2 + depth / 2, 4));
        }

        private static int TargetCellsForDepth(int depth)
        {
            // Large vs the incident's 6–14 cell veins.
            int min;
            int max;
            if (depth <= 1)
            {
                min = 18;
                max = 32;
            }
            else if (depth == 2)
            {
                min = 22;
                max = 40;
            }
            else
            {
                min = 26;
                max = 44 + Mathf.Min(depth * 3, 18);
            }

            int size = Rand.RangeInclusive(min, max);
            // Occasional motherlode — rarer on B1, more likely deeper.
            float motherChance = Mathf.Min(0.08f + 0.04f * depth, 0.28f);
            if (Rand.Chance(motherChance))
            {
                size = Rand.RangeInclusive(Mathf.Max(size, 40), 55 + Mathf.Min(depth * 4, 20));
            }
            return size;
        }

        private static bool TryFindNodeSpot(Map map, List<IntVec3> placed, out IntVec3 spot)
        {
            IntVec3 landing = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            for (int i = 0; i < PlacementTries; i++)
            {
                var candidate = new IntVec3(
                    Rand.RangeInclusive(EdgeMargin, map.Size.x - 1 - EdgeMargin), 0,
                    Rand.RangeInclusive(EdgeMargin, map.Size.z - 1 - EdgeMargin));
                if (candidate.DistanceTo(landing) < MinDistFromLanding)
                {
                    continue;
                }
                bool tooClose = false;
                foreach (IntVec3 other in placed)
                {
                    if (candidate.DistanceTo(other) < MinDistBetween)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                {
                    continue;
                }
                if (!OreReveal.IsHostRock(candidate, map, allowFogged: true))
                {
                    continue;
                }
                // Prefer a seed with enough contiguous host rock for a fat node.
                if (CountNearbyHost(map, candidate, 4.2f) < 12)
                {
                    continue;
                }
                spot = candidate;
                return true;
            }
            spot = IntVec3.Invalid;
            return false;
        }

        private static int CountNearbyHost(Map map, IntVec3 root, float radius)
        {
            int n = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, radius, useCenter: true))
            {
                if (cell.InBounds(map) && OreReveal.IsHostRock(cell, map, allowFogged: true))
                {
                    n++;
                }
            }
            return n;
        }
    }
}
