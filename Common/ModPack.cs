using System.Reflection;
using Verse;

namespace AzraelCommon
{
    /// <summary>
    /// Resolve the running pack that loaded this DLL.
    /// </summary>
    internal static class ModPack
    {
        /// <summary>
        /// Always uses this DLL's ModContentPack so a Workshop copy of the same
        /// packageId cannot supply an older version.
        /// </summary>
        internal static ModContentPack ForExecutingAssembly()
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
                    return pack;
                }
            }

            return null;
        }
    }
}
