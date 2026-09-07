using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AzraelCommon
{
    /// <summary>
    /// Per-class PatchAll so one bad patch cannot take the whole mod down
    /// (AZR-45). Skips JobDriver/LordJob-style types that only inherit Cleanup
    /// (AZR-95). Linked into each mod from Common/ — do not copy this file.
    /// </summary>
    internal static class SafePatchAll
    {
        internal static void Apply(Harmony harmony, string logPrefix)
        {
            int failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (!IsHarmonyPatchClass(type))
                {
                    continue;
                }

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error(logPrefix + " Harmony patch class " + type.Name + " failed: " + e.Message);
                }
            }
            NotifyIfFailed(logPrefix, failed);
        }

        private static void NotifyIfFailed(string logPrefix, int failed)
        {
            if (failed <= 0)
            {
                return;
            }

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Find.LetterStack == null)
                {
                    return;
                }

                Find.LetterStack.ReceiveLetter(
                    (logPrefix + " Harmony").Trim(),
                    logPrefix + " " + failed + " Harmony patch(es) failed. The rest of the mod still loaded. See Player.log.",
                    LetterDefOf.NegativeEvent);
            });
        }

        /// <summary>
        /// CreateClassProcessor treats any type with Prefix/Postfix/Cleanup as a patch
        /// class. JobDriver subclasses inherit Cleanup(JobCondition), which is not a
        /// Harmony auxiliary — skip them unless they actually have [HarmonyPatch].
        /// </summary>
        internal static bool IsHarmonyPatchClass(Type type)
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0)
            {
                return true;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
