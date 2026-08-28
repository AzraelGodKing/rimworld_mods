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
