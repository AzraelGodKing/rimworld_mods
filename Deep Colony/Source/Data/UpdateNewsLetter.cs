namespace DeepColony
{
    internal static class UpdateNewsLetter
    {
        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            AzraelCommon.UpdateNews.TrySend(
                ref lastAnnouncedVersion,
                "azraelgodking.DeepColony",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/deep-colony.md",
                "DC_UpdateLetterLabel",
                "DC_UpdateLetterFallback",
                "DC_UpdateLetterFooter");
        }
    }
}
