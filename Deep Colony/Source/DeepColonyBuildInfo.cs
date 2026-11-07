using AzraelCommon;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "deepcolony2-net10";

        public static void LogStartup()
        {
            ModVersionLog.Write("[DeepColony]", extra: BuildStamp);
        }
    }
}
