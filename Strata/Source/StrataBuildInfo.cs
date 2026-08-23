using System;
using System.IO;
using System.Reflection;
using Verse;

namespace Strata
{
    // Visible in Player.log so we can confirm which assembly RimWorld loaded.
    public static class StrataBuildInfo
    {
        public const string BuildStamp = "shaft-temp-map-guard-v1";

        public static void LogStartup()
        {
            Assembly asm = typeof(StrataBuildInfo).Assembly;
            string path = asm.Location;
            string writeTime = "unknown";
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                writeTime = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            }

            StrataSettings settings = StrataMod.Settings;
            string relaySummary = settings == null
                ? "settings=n/a"
                : "workRelay=" + settings.workRelayEnabled
                    + " robotSoftCompat=" + settings.robotSoftCompatEnabled
                    + " robotWorkRelay=" + settings.robotWorkRelayEnabled
                    + " performanceMode=" + settings.performanceModeEnabled
                    + " settingsVersion=" + settings.settingsVersion;

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

            Log.Message("[Strata] v" + version + " Soft-compat build " + BuildStamp + " loaded from "
                + path + " (modified " + writeTime + "); " + relaySummary + ".");
            StrataOffThreadWork.LogStartup();
        }
    }
}
