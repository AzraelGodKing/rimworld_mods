using System;
using System.IO;
using System.Reflection;
using Verse;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "batch-c-v1";

        public static void LogStartup()
        {
            Assembly asm = typeof(DeepColonyBuildInfo).Assembly;
            string path = asm.Location;
            string writeTime = "unknown";
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                writeTime = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            }
            Log.Message($"[DeepColony] Build {BuildStamp} | {writeTime} | {path}");
        }
    }
}
