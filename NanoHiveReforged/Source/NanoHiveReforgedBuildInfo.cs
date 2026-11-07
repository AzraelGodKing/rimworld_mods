using AzraelCommon;
using Verse;

namespace NanoHiveReforged
{
    // Visible in Player.log so we can confirm which assembly RimWorld loaded.
    // The Azrael series hub reads BuildStamp by reflection.
    public static class NanoHiveReforgedBuildInfo
    {
        public const string BuildStamp = "nanohive2-net10";

        public static void LogStartup(ModContentPack content)
        {
            ModVersionLog.Write("[NanoHive Reforged]", content, BuildStamp);
        }
    }
}
