using AzraelCommon;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "colony-overview-v1";

        public static void LogStartup()
        {
            ModVersionLog.Write("[DeepColony]", extra: BuildStamp);
        }
    }
}
