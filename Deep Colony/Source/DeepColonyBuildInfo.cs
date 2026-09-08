using System;
using System.IO;
using System.Reflection;
using DeepColony.Patches;
using Verse;

namespace DeepColony
{
    public static class DeepColonyBuildInfo
    {
        public const string BuildStamp = "labor-wedge-v1";
        public const string SourceRevision = "b22c43a";

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

            string asmVer = asm.GetName().Version?.ToString() ?? "unknown";
            string infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? asmVer;
            bool prefix = BirthSafetyNet.PrefixApplied();
            bool setting = DeepColonySettings.Get.enableBirthSafetyNet;
            Log.Message("[DeepColony] v" + version
                + " asm=" + asmVer
                + " info=" + infoVer
                + " sha=" + SourceRevision
                + " build " + BuildStamp
                + " | birthSafetyNet=" + (prefix ? "applied" : "MISSING")
                + " setting=" + (setting ? "on" : "off")
                + " | " + writeTime
                + " | " + path);
        }
    }
}
