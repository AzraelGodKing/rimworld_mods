using Verse;

namespace Strata
{
    public class GameComponent_UpdateNews : GameComponent
    {
        private string lastNewsVersion;

        public GameComponent_UpdateNews(Game game)
        {
        }

        public override void FinalizeInit()
        {
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
        }
    }

    internal static class UpdateNewsLetter
    {
        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            AzraelCommon.UpdateNews.TrySend(
                ref lastAnnouncedVersion,
                "AzraelGodKing.Strata",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/strata.md",
                "Strata_UpdateLetterLabel",
                "Strata_UpdateLetterFallback",
                "Strata_UpdateLetterFooter");
        }
    }
}
