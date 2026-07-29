using System.Collections.Generic;
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

            int target = Rand.RangeInclusive(3, 5);
            int seed = StrataCavernPlan.MixCaveSeed(map, SeedPart, start);
            StrataCavernPlan.Result plan = StrataCavernPlan.RunOnWorker(() =>
                StrataCavernPlan.PlanVault(
                    map.Size.x,
                    map.Size.z,
                    start,
                    target,
                    MinChamberRadius,
                    MaxChamberRadius,
                    EdgeMargin,
                    seed));

            StrataCavernPlan.ApplyMineableDestroy(plan.carveMask, map);
            MapGenerator.SetVar(GenStep_CarveWarren.ChambersVar, plan.chambers ?? new List<IntVec3> { start });
        }
    }
}
