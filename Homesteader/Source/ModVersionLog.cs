using System;
using System.IO;
using System.Reflection;
using Verse;

namespace Homesteader
{
    internal static class ModVersionLog
    {
        internal static void Write(string logPrefix, ModContentPack content = null, string extra = null)
        {
            if (content == null)
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
                {
                    if (pack.assemblies?.loadedAssemblies == null)
                    {
                        continue;
                    }
                    if (pack.assemblies.loadedAssemblies.Contains(asm))
                    {
                        content = pack;
                        break;
                    }
                }
            }

            string version = content?.ModMetaData?.ModVersion;
            if (string.IsNullOrEmpty(version))
            {
                version = "unknown";
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = assembly.Location;
            string writeTime = "unknown";
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                writeTime = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            }

            string msg = logPrefix + " v" + version + " loaded from " + path + " (modified " + writeTime + ")";
            if (!string.IsNullOrEmpty(extra))
            {
                msg += "; " + extra;
            }
            Log.Message(msg + ".");
        }
    }
}
