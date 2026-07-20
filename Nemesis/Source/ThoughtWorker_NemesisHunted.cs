using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Mild ongoing mood while this pawn is the active fixation target of a hunt.</summary>
    public class ThoughtWorker_NemesisHunted : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null || p.Dead) return ThoughtState.Inactive;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.Data == null || !comp.Data.active) return ThoughtState.Inactive;
            if (comp.Data.targetMode != NemesisTargetMode.Pawn) return ThoughtState.Inactive;
            if (!comp.IsTargetPawn(p)) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(0);
        }
    }
}
