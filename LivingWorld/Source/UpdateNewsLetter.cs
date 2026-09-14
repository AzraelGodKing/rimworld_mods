namespace LivingWorld
{
    internal static class UpdateNewsLetter
    {
        internal static void TrySend(ref string lastAnnouncedVersion)
        {
            AzraelCommon.UpdateNews.TrySend(
                ref lastAnnouncedVersion,
                "azraelgodking.livingworld",
                "https://github.com/AzraelGodKing/rimworld_mods/blob/main/site/src/data/changelogs/living-world.md",
                "LivingWorld_UpdateLetterLabel",
                "LivingWorld_UpdateLetterFallback",
                "LivingWorld_UpdateLetterFooter");
        }
    }
}
