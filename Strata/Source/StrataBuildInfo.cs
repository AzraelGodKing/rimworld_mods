using AzraelCommon;

namespace Strata
{
    // Visible in Player.log so we can confirm which assembly RimWorld loaded.
    public static class StrataBuildInfo
    {
        public const string BuildStamp = "upper-floor-v1";

        public static void LogStartup()
        {
            ModVersionLog.Write("[Strata]", extra: BuildStamp);
            StrataOffThreadWork.LogStartup();
        }
    }
}
