using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>AZR-65 — noisy bedroom warning (stage 0) then sleep penalty (stage 1).</summary>
    public class ThoughtWorker_NoisyRoom : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            int stage = QuietHoursUtility.ThoughtStage(p);
            if (stage < 0) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
