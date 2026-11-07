#nullable enable
using RimWorld;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Forces <see cref="storyteller"/> after world gen / game start.
    /// Vanilla has no ScenPart_ForcedStoryteller — Deep Homestead needs this so the
    /// showcase always runs Azrael even if the player picked another teller on the page.
    /// </summary>
    public class ScenPart_ForcedStoryteller : ScenPart
    {
        public StorytellerDef? storyteller;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref storyteller, "storyteller");
        }

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
            string name = storyteller != null ? storyteller.LabelCap : "Azrael";
            return "Azrael_ScenPart_ForcedStorytellerSummary".Translate(name);
        }

        private void Apply()
        {
            if (storyteller == null || Current.Game?.storyteller == null)
            {
                return;
            }

            if (Current.Game.storyteller.def == storyteller)
            {
                return;
            }

            // Swap the def in place (same as the in-game storyteller change) so difficulty,
            // incident history and anything else the storyteller holds survive.
            Storyteller current = Current.Game.storyteller;
            current.def = storyteller;
            current.Notify_DefChanged();
        }
    }
}
