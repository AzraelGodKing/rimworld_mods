namespace Nemesis
{
    internal static class UpdateNewsLetter
    {
        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            AzraelCommon.UpdateNews.TrySend(
                ref lastAnnouncedVersion,
                "AzraelGodKing.Nemesis",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/nemesis.md",
                "Nemesis_UpdateLetterLabel",
                "Nemesis_UpdateLetterFallback",
                "Nemesis_UpdateLetterFooter");
        }
    }
}
