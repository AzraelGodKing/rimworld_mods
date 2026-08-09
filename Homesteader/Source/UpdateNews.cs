using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>One-shot update mail when Homesteader adds content the player should notice.</summary>
    public class GameComponent_HomesteaderNews : GameComponent
    {
        private bool littleGuyTraitLetterSent;

        public GameComponent_HomesteaderNews(Game game)
        {
        }

        public override void FinalizeInit()
        {
            TrySendLittleGuyTraitLetter();
        }

        private void TrySendLittleGuyTraitLetter()
        {
            if (littleGuyTraitLetterSent) return;
            if (Current.Game == null || Find.LetterStack == null) return;

            // Mark first so a letter exception cannot spam every load.
            littleGuyTraitLetterSent = true;

            Find.LetterStack.ReceiveLetter(
                "Homesteader_LittleGuyLetterLabel".Translate(),
                "Homesteader_LittleGuyLetterText".Translate(),
                LetterDefOf.PositiveEvent);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref littleGuyTraitLetterSent, "littleGuyTraitLetterSent", false);
        }
    }
}
