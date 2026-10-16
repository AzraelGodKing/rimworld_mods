using AzraelCommon;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "odyssey-save-growth-v1";

        public static void LogStartup()
        {
            ModVersionLog.Write("[DeepColony]", extra: BuildStamp);
        }
    }
}
