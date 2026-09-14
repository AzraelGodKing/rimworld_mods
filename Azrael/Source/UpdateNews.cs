using Verse;

namespace Azrael
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
                "azraelgodking.Azrael",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/azrael.md",
                "Azrael_UpdateLetterLabel",
                "Azrael_UpdateLetterFallback",
                "Azrael_UpdateLetterFooter");
        }
    }
}
