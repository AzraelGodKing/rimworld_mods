using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    public class ThoughtWorker_StillAlive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!WaveSurvivalComponent.ModeActive || !p.IsColonist)
                return false;
            RunSettings run = WaveSurvivalComponent.Instance?.Run;
            return run == null || run.moodBuff > 0f;
        }
    }
}
