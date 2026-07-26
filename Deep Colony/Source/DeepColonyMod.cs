using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony
{
    [StaticConstructorOnStartup]
    public static class DeepColonyStartup
    {
        static DeepColonyStartup()
        {
            var harmony = new Harmony("azraelgodking.DeepColony");
            harmony.PatchAll();
            InjectComps();
            LongEventHandler.ExecuteWhenFinished(DeepColonyBuildInfo.LogStartup);
        }

        private static void InjectComps()
        {
            int injected = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race?.intelligence != Intelligence.Humanlike) continue;
                try
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (!def.comps.Any(c => c is CompProperties_DeepColony))
                    {
                        def.comps.Add(new CompProperties_DeepColony());
                        injected++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[DeepColony] Could not inject comp into " + def.defName + ": " + ex.Message);
                }
            }
            Log.Message("[DeepColony] Injected Comp_DeepColony into " + injected + " humanlike ThingDef(s).");
        }
    }
}
