using System;
using System.IO;
using System.Reflection;
using Verse;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "empty-nest-floors-v1";

        public static void LogStartup()
        {
            Assembly asm = typeof(DeepColonyBuildInfo).Assembly;
            string path = asm.Location;
            string writeTime = "unknown";
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                writeTime = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            }
            string version = "unknown";
            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                if (pack.assemblies?.loadedAssemblies == null
                    || !pack.assemblies.loadedAssemblies.Contains(asm))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(pack.ModMetaData?.ModVersion))
                {
                    version = pack.ModMetaData.ModVersion;
                }
                break;
            }
            Log.Message($"[DeepColony] v{version} build {BuildStamp} | {writeTime} | {path}");
        }
    }
}
