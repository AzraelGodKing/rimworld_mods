using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    public class ThoughtWorker_StillAlive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return WaveSurvivalComponent.ModeActive && p.IsColonist;
        }
    }
}
