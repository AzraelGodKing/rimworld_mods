using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    [StaticConstructorOnStartup]
    public static class HomesteaderHarmony
    {
        internal static HashSet<ThingDef> RottableDefs = new HashSet<ThingDef>();

        static HomesteaderHarmony()
        {
            HarmonyPatchAll.Apply(new Harmony("azraelgodking.homesteader"), "[Homesteader]");
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.HasComp(typeof(CompRottable)))
                    RottableDefs.Add(def);
            }
        }
    }

    internal static class HarmonyPatchAll
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

        private static bool IsHarmonyPatchClass(Type type)
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
