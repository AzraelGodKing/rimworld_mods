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
            HarmonyPatchAll.Apply(harmony, "[DeepColony]");
            InjectComps();
            InjectFamilyTab();
            LongEventHandler.ExecuteWhenFinished(DeepColonyBuildInfo.LogStartup);
        }

        private static void InjectFamilyTab()
        {
            int injected = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race?.intelligence != Intelligence.Humanlike) continue;
                try
                {
                    if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                    if (!def.inspectorTabs.Contains(typeof(ITab_Pawn_FamilyTree)))
                    {
                        int idx = def.inspectorTabs.FindIndex(t => t.Name == "ITab_Pawn_Character");
                        if (idx >= 0) def.inspectorTabs.Insert(idx + 1, typeof(ITab_Pawn_FamilyTree));
                        else def.inspectorTabs.Add(typeof(ITab_Pawn_FamilyTree));
                    }

                    if (def.inspectorTabsResolved == null)
                        def.inspectorTabsResolved = new List<InspectTabBase>();
                    bool already = false;
                    for (int i = 0; i < def.inspectorTabsResolved.Count; i++)
                    {
                        if (def.inspectorTabsResolved[i] is ITab_Pawn_FamilyTree)
                        {
                            already = true;
                            break;
                        }
                    }
                    if (!already)
                    {
                        def.inspectorTabsResolved.Add(
                            InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_FamilyTree)));
                        injected++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[DeepColony] Could not inject Family tab into " + def.defName + ": " + ex.Message);
                }
            }
            Log.Message("[DeepColony] Injected Family inspect tab into " + injected + " humanlike ThingDef(s).");
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
