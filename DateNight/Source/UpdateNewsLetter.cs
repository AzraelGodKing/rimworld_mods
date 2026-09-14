namespace DateNight
{
    internal static class UpdateNewsLetter
    {
        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            AzraelCommon.UpdateNews.TrySend(
                ref lastAnnouncedVersion,
                "azraelgodking.DateNight",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/date-night.md",
                "DateNight_UpdateLetterLabel",
                "DateNight_UpdateLetterFallback",
                "DateNight_UpdateLetterFooter");
        }
    }
}
