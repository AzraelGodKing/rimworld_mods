using RimWorld;

namespace WaveSurvivalReforged
{
    // The "still alive" buff, sized by the run's mood setting instead of the def's stage.
    public class Thought_StillAlive : Thought_Situational
    {
        public override float MoodOffset()
        {
            WaveSurvivalComponent waves = WaveSurvivalComponent.Instance;
            return waves?.Run != null ? waves.Run.moodBuff : base.MoodOffset();
        }
    }
}
