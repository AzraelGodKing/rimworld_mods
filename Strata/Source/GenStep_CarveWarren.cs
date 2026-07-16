using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Carves an organic warren out of the solid stratum GenStep_SolidRock laid
    // down: a chain of chambers linked by winding tunnels, growing outward from
    // the arrival chamber at the stairwell landing. Chamber centers are stored
    // in a MapGenerator var so the infestation genstep can populate them.
    public class GenStep_CarveWarren : GenStep
    {
        public const string ChambersVar = "Strata_WarrenChambers";

        // Chambers must stay narrower than vanilla's thick-roof support reach
        // (6.9 cells) or their centers cave in the moment anything nearby moves.
        private const float MinChamberRadius = 3.4f;

        private const float MaxChamberRadius = 5.4f;

        private const int EdgeMargin = 12;

        public override int SeedPart => 1937420571;

        public override void Generate(Map map, GenStepParams parms)
        {
            bool playerLevel = map.generatorDef?.defName == "Strata_UndergroundLevel";
            if (playerLevel && !StrataCavernUtility.ShouldGenerateNativeCavernLayout(map))
            {
                return;
            }

            IntVec3 start = MapGenerator.PlayerStartSpot;
            if (!start.IsValid || !start.InBounds(map))
            {
                start = map.Center;
            }

            var chambers = new List<IntVec3> { start };
            int target = playerLevel
                ? TargetChamberCount(StrataDepth.CountLevelsBelowSurface(map))
                : Rand.RangeInclusive(5, 8);
            int attempts = 0;
            while (chambers.Count <= target && attempts++ < 40)
            {
                // Mostly grow from the tip so the warren snakes away from the
                // stairs, with the odd branch back off an earlier chamber.
                IntVec3 anchor = chambers.Count > 1 && Rand.Chance(0.3f)
                    ? chambers.RandomElement()
                    : chambers[chambers.Count - 1];
                IntVec3 next = NextChamberSpot(map, anchor, chambers);
                if (!next.IsValid)
                {
                    continue;
                }
                CarveTunnel(map, anchor, next);
                CarveCircle(map, next, Rand.Range(MinChamberRadius, MaxChamberRadius));
                chambers.Add(next);
            }

            MapGenerator.SetVar(ChambersVar, chambers);
            if (playerLevel && StrataCavernUtility.ShouldGenerateNativeCavernLayout(map))
            {
                GenStep_SolidRock.SpawnMineables(map);
            }
        }

        private static int TargetChamberCount(int depth)
        {
            if (depth <= 2)
            {
                return Rand.RangeInclusive(4, 6);
            }
            if (depth <= 4)
            {
                return Rand.RangeInclusive(5, 8);
            }
            return Rand.RangeInclusive(6, 10);
        }

        private static IntVec3 NextChamberSpot(Map map, IntVec3 anchor, List<IntVec3> existing)
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = Rand.Range(0f, 360f);
                float dist = Rand.Range(15f, 26f);
                var offset = new IntVec3(
                    Mathf.RoundToInt(Mathf.Cos(angle * Mathf.Deg2Rad) * dist), 0,
                    Mathf.RoundToInt(Mathf.Sin(angle * Mathf.Deg2Rad) * dist));
                IntVec3 candidate = anchor + offset;
                if (!candidate.InBounds(map) || candidate.DistanceToEdge(map) < EdgeMargin)
                {
                    continue;
                }
                bool tooClose = false;
                for (int j = 0; j < existing.Count; j++)
                {
                    if (candidate.DistanceTo(existing[j]) < 12f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    return candidate;
                }
            }
            return IntVec3.Invalid;
        }

        // Two straight legs through a jittered midpoint read as one crooked,
        // dug-by-something tunnel rather than a surveyor's corridor.
        private static void CarveTunnel(Map map, IntVec3 from, IntVec3 to)
        {
            IntVec3 mid = (from + to) / 2 + new IntVec3(Rand.RangeInclusive(-5, 5), 0, Rand.RangeInclusive(-5, 5));
            if (!mid.InBounds(map))
            {
                mid = (from + to) / 2;
            }
            CarveLeg(map, from, mid);
            CarveLeg(map, mid, to);
        }

        private static void CarveLeg(Map map, IntVec3 from, IntVec3 to)
        {
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(from, to))
            {
                CarveCircle(map, cell, 1.5f);
            }
        }

        private static void CarveCircle(Map map, IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (cell.InBounds(map))
                {
                    cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
