using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Carves a vault layout: fewer, larger chambers than a warren, linked by
    // straight-ish tunnels. Reuses the same MapGenerator var as CarveWarren so
    // downstream gensteps can populate either layout.
    public class GenStep_CarveVault : GenStep
    {
        private const float MinChamberRadius = 5.4f;

        private const float MaxChamberRadius = 7.4f;

        private const int EdgeMargin = 14;

        public override int SeedPart => 1937420572;

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 start = MapGenerator.PlayerStartSpot;
            if (!start.IsValid || !start.InBounds(map))
            {
                start = map.Center;
            }

            var chambers = new List<IntVec3> { start };
            int target = Rand.RangeInclusive(3, 5);
            int attempts = 0;
            while (chambers.Count <= target && attempts++ < 30)
            {
                IntVec3 anchor = chambers[chambers.Count - 1];
                IntVec3 next = NextChamberSpot(map, anchor, chambers);
                if (!next.IsValid)
                {
                    continue;
                }
                CarveTunnel(map, anchor, next);
                CarveCircle(map, next, Rand.Range(MinChamberRadius, MaxChamberRadius));
                chambers.Add(next);
            }

            MapGenerator.SetVar(GenStep_CarveWarren.ChambersVar, chambers);
        }

        private static IntVec3 NextChamberSpot(Map map, IntVec3 anchor, List<IntVec3> existing)
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = Rand.Range(0f, 360f);
                float dist = Rand.Range(18f, 32f);
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
                    if (candidate.DistanceTo(existing[j]) < 16f)
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

        private static void CarveTunnel(Map map, IntVec3 from, IntVec3 to)
        {
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(from, to))
            {
                CarveCircle(map, cell, 2f);
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
