using Verse;

namespace Niceties
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

        public override void GameComponentTick()
        {
            OpenBuilds.TickDeferred();
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
                "AzraelGodKing.Niceties",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/niceties.md",
                "Niceties_UpdateLetterLabel",
                "Niceties_UpdateLetterFallback",
                "Niceties_UpdateLetterFooter");
        }
    }
}
