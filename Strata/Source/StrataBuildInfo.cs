using System;
using System.IO;
using System.Reflection;
using Verse;

namespace Strata
{
    // Visible in Player.log so we can confirm which assembly RimWorld loaded.
    public static class StrataBuildInfo
    {
        public const string BuildStamp = "g8-mainthread-load-repair-v6";

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

            Log.Message("[Strata] Soft-compat build " + BuildStamp + " loaded from "
                + path + " (modified " + writeTime + "); " + relaySummary + ".");
            StrataOffThreadWork.LogStartup();
        }
    }
}
