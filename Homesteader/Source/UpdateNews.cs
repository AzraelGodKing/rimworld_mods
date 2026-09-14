using Verse;

namespace Homesteader
{
    public class GameComponent_HomesteaderNews : GameComponent
    {
        private string lastNewsVersion;

        public GameComponent_HomesteaderNews(Game game)
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
                "AzraelGodKing.Homesteader",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/homesteader.md",
                "Homesteader_UpdateLetterLabel",
                "Homesteader_UpdateLetterFallback",
                "Homesteader_UpdateLetterFooter");
        }
    }
}
