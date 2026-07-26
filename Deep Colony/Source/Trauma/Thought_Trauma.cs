using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Long-lasting trauma memory. Stage advances with age (Raw → Processing → Fading).
    /// Therapy increases age toward DurationTicks so the thought expires sooner.
    /// </summary>
    public class Thought_Trauma : Thought_Memory
    {
        public TraumaDef traumaDef;

        public override int CurStageIndex
        {
            get
            {
                if (DurationTicks <= 0) return 0;
                float progress = (float)age / DurationTicks;
                if (progress < 0.33f) return 0;
                if (progress < 0.66f) return 1;
                return 2;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref traumaDef, "traumaDef");
        }
    }
}
