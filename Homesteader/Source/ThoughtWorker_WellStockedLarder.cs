using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Tiered mood from distinct preserved-food kinds in Homesteader storage.
    /// Stages: 3 / 6 / 9 kinds via PantryUtility.Snapshot().preserveKinds.
    /// </summary>
    public class ThoughtWorker_WellStockedLarder : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null || !p.IsColonist || p.Dead || p.needs?.mood == null)
            {
                return ThoughtState.Inactive;
            }

            int kinds = PantryUtility.Snapshot().preserveKinds;
            if (kinds >= 9)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            if (kinds >= 6)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (kinds >= 3)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }
}
