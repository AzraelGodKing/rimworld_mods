using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Azrael
{
    public class IncidentWeight
    {
        public string incident;
        public float factor = 1f;
    }

    /// <summary>
    /// Multiplies the pick weight of listed incidents while <see cref="storyteller"/> runs.
    /// Incidents are defNames so entries for series mods that are not loaded are skipped.
    /// </summary>
    public class StorytellerIncidentWeightsDef : Def
    {
        public string storyteller;
        public List<IncidentWeight> weights = new List<IncidentWeight>();
    }

    public static class StorytellerIncidentWeights
    {
        private static Dictionary<string, Dictionary<IncidentDef, float>> byStoryteller;

        public static float FactorFor(StorytellerDef teller, IncidentDef incident)
        {
            if (teller == null || incident == null)
            {
                return 1f;
            }
            if (byStoryteller == null)
            {
                Build();
            }
            if (!byStoryteller.TryGetValue(teller.defName, out Dictionary<IncidentDef, float> table))
            {
                return 1f;
            }
            return table.TryGetValue(incident, out float factor) ? factor : 1f;
        }

        private static void Build()
        {
            var built = new Dictionary<string, Dictionary<IncidentDef, float>>();
            foreach (StorytellerIncidentWeightsDef def in DefDatabase<StorytellerIncidentWeightsDef>.AllDefsListForReading)
            {
                if (def.storyteller.NullOrEmpty() || def.weights == null)
                {
                    continue;
                }
                if (!built.TryGetValue(def.storyteller, out Dictionary<IncidentDef, float> table))
                {
                    table = new Dictionary<IncidentDef, float>();
                    built[def.storyteller] = table;
                }
                foreach (IncidentWeight w in def.weights)
                {
                    if (w == null || w.incident.NullOrEmpty())
                    {
                        continue;
                    }
                    IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail(w.incident);
                    if (incident != null)
                    {
                        table[incident] = Mathf.Max(0f, w.factor);
                    }
                }
            }
            byStoryteller = built;
        }
    }

    // Every vanilla comp picks within a category by IncidentChanceFinal weight.
    [HarmonyPatch(typeof(StorytellerComp), "IncidentChanceFinal")]
    public static class Patch_StorytellerComp_IncidentChanceFinal
    {
        public static void Postfix(IncidentDef def, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }
            float factor = StorytellerIncidentWeights.FactorFor(Find.Storyteller?.def, def);
            if (factor != 1f)
            {
                __result *= factor;
            }
        }
    }
}
