using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    /// <summary>
    /// Locks the scenario to the Wave Survival storyteller after world generation and at
    /// game start, keeping the difficulty the player picked. Same idea as Azrael's
    /// ScenPart_ForcedStoryteller, without depending on that mod.
    /// </summary>
    public class ScenPart_WaveSurvivalStoryteller : ScenPart
    {
        public override void PostWorldGenerate()
        {
            Apply();
        }

        public override void PostGameStart()
        {
            Apply();
        }

        public override string Summary(Scenario scen)
        {
            return "WSR_ScenPartForcedSummary".Translate(WSR_DefOf.WSR_WaveSurvival.LabelCap);
        }

        private static void Apply()
        {
            Storyteller old = Current.Game?.storyteller;
            if (old == null || old.def == WSR_DefOf.WSR_WaveSurvival)
                return;
            Current.Game.storyteller = new Storyteller(WSR_DefOf.WSR_WaveSurvival, old.difficultyDef, old.difficulty);
        }
    }
}
