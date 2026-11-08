using AzraelCommon;
using Verse;

namespace WaveSurvivalReforged
{
    // Read by the Azrael hub through the series-wide <Namespace>.<Namespace>BuildInfo.BuildStamp convention.
    public static class WaveSurvivalReforgedBuildInfo
    {
        public const string BuildStamp = "wsr2-package";

        internal static void LogLoaded()
        {
            ModVersionLog.Write("[WaveSurvivalReforged]", extra: BuildStamp);
        }
    }
}
