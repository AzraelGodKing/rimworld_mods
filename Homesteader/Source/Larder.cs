#nullable enable
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Well-stocked larder (HS-A02, AZR-576): colonists feel better when their own
    /// map's cellars and pantries hold 3, 6 or 9 distinct preserves.
    /// </summary>
    public class ThoughtWorker_WellStockedLarder : ThoughtWorker
    {
        internal static int StageFor(int preserveKinds) => HomesteaderPure.LarderStage(preserveKinds);

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!HomesteaderGameplay.Active.larderMood || !p.Spawned || p.Map == null || !p.IsColonist
                || !p.Map.IsPlayerHome)
            {
                return ThoughtState.Inactive;
            }

            int stage = StageFor(PantryUtility.ForMap(p.Map).preserveKinds);
            return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }
    }
}
