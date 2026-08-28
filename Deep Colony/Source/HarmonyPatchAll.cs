using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DeepColony
{
    internal static class HarmonyPatchAll
    {
        internal static void Apply(Harmony harmony, string logPrefix)
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Log.Error(logPrefix + " Harmony patch class " + type.Name + " failed: " + e.Message);
                }
            }
        }
    }
}
