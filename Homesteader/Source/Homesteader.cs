using System.Collections.Generic;
using AzraelCommon;
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
            SafePatchAll.Apply(new Harmony("azraelgodking.homesteader"), "[Homesteader]");
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.HasComp(typeof(CompRottable)))
                    RottableDefs.Add(def);
            }
        }
    }
}
