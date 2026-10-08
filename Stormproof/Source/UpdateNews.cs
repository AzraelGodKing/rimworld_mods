using Verse;

namespace Stormproof
{
    public class GameComponent_UpdateNews : GameComponent
    {
        private string lastNewsVersion;

        public GameComponent_UpdateNews(Game game)
        {
            // New Game object: drop stale registry refs left from a previous
            // session that exited to the main menu without despawning (AZR-322).
            // Must run in the ctor (before map PostSpawnSetup), not FinalizeInit.
            StormproofRegistry.Clear();
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
                "AzraelGodKing.Stormproof",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/stormproof.md",
                "Stormproof_UpdateLetterLabel",
                "Stormproof_UpdateLetterFallback",
                "Stormproof_UpdateLetterFooter");
        }
    }
}
