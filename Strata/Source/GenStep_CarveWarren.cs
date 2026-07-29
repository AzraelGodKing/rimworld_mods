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

            bool deferMineables = MapGenerator.TryGetVar(GenStep_SolidRock.DeferMineablesVar, out bool defer)
                && defer;

            int target = playerLevel
                ? TargetChamberCount(StrataDepth.CountLevelsBelowSurface(map))
                : Rand.RangeInclusive(4, 7);
            // Seed-stable cave system from map.ConstantRandSeed (+ SeedPart / depth / size).
            int seed = StrataCavernPlan.MixCaveSeed(map, SeedPart, start);
            StrataCavernPlan.Result plan = StrataCavernPlan.RunOnWorker(() =>
                StrataCavernPlan.PlanWarren(
                    map.Size.x,
                    map.Size.z,
                    start,
                    target,
                    ArrivalZoneUtility.ChamberRadius,
                    MinChamberRadius,
                    MaxChamberRadius,
                    EdgeMargin,
                    seed));

            var carveMask = new BoolGrid(map);
            StrataCavernPlan.CopyMaskToBoolGrid(plan.carveMask, carveMask, map);
            if (!deferMineables)
            {
                StrataCavernPlan.ApplyMineableDestroy(plan.carveMask, map);
            }

            MapGenerator.SetVar(ChambersVar, plan.chambers ?? new List<IntVec3> { start });

            if (playerLevel
                || (MapGenerator.TryGetVar(StrataNativeCavernUtility.ForceNativeWarrenVar, out bool force) && force))
            {
                StrataNativeCavernUtility.PaintCarvedFloors(map, carveMask);
            }

            if (deferMineables)
            {
                StrataRockFillScheduler.Schedule(map, skipCarveMask: carveMask);
            }
        }

        private static int TargetChamberCount(int depth)
        {
            // Major rooms only — branches/spurs add extra volume beyond this count.
            bool perf = StrataMod.Settings?.performanceModeEnabled == true;
            if (perf)
            {
                return depth <= 2 ? 4 : 5;
            }
            if (depth <= 2)
            {
                return Rand.RangeInclusive(4, 6);
            }
            if (depth <= 4)
            {
                return Rand.RangeInclusive(5, 7);
            }
            return Rand.RangeInclusive(6, 8);
        }
    }
}
