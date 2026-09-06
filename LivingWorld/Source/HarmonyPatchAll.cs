using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LivingWorld
{
    internal static class HarmonyPatchAll
    {
        internal static void Apply(Harmony harmony, string logPrefix)
        {
            int failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
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
    }
}
