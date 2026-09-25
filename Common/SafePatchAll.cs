using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AzraelCommon
{
    /// <summary>
    /// Per-class PatchAll so one bad patch cannot take the whole mod down.
    /// Failures go to Player.log in full, then to the Azrael hub if that mod loaded.
    /// </summary>
    internal static class SafePatchAll
    {
        private static readonly List<PendingFail> pending = new List<PendingFail>();

        private struct PendingFail
        {
            public string Prefix;
            public string ClassName;
            public string Reason;
        }

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
                    string reason = ShortReason(e);
                    pending.Add(new PendingFail
                    {
                        Prefix = logPrefix ?? "",
                        ClassName = type.Name,
                        Reason = reason
                    });
                    Log.Error(logPrefix + " Harmony patch class " + type.Name + " failed: " + e);
                }
            }
            NotifyIfFailed(logPrefix, failed);
        }

        private static string ShortReason(Exception e)
        {
            if (e == null)
            {
                return "(unknown)";
            }

            string text = e.GetType().Name + ": " + e.Message;
            if (e.InnerException != null)
            {
                text += " | " + e.InnerException.GetType().Name + ": " + e.InnerException.Message;
            }
            return text;
        }

        private static void NotifyIfFailed(string logPrefix, int failed)
        {
            if (failed <= 0)
            {
                return;
            }

            LongEventHandler.ExecuteWhenFinished(FlushToHub);
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                // Early load can run before the game/world exists — LetterStack getter NREs. AZR-345.
                try
                {
                    if (Current.Game == null || Find.LetterStack == null)
                    {
                        return;
                    }

                    Find.LetterStack.ReceiveLetter(
                        (logPrefix + " Harmony").Trim(),
                        logPrefix + " " + failed + " Harmony patch(es) failed. The rest of the mod still loaded. See Player.log or Mod Options → Azrael.",
                        LetterDefOf.NegativeEvent);
                }
                catch (NullReferenceException)
                {
                    // Still too early; Player.log already has the patch failure.
                }
            });
        }

        private static void FlushToHub()
        {
            if (pending.Count == 0)
            {
                return;
            }

            try
            {
                Type sink = AccessTools.TypeByName("Azrael.PatchFailureSink");
                MethodInfo record = sink?.GetMethod("Record", BindingFlags.Public | BindingFlags.Static);
                if (record == null)
                {
                    return;
                }

                for (int i = 0; i < pending.Count; i++)
                {
                    PendingFail fail = pending[i];
                    record.Invoke(null, new object[] { fail.Prefix, fail.ClassName, fail.Reason });
                }
            }
            catch
            {
                // Azrael not loaded, or hub type renamed — log already has the trace.
            }
            finally
            {
                pending.Clear();
            }
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
