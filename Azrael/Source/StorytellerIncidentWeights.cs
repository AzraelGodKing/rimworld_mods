#nullable enable
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Azrael
{
    public class IncidentWeight
    {
        public string? incident;
        public float factor = 1f;
    }

    /// <summary>
    /// Multiplies the pick weight of listed incidents while <see cref="storyteller"/> runs.
    /// Incidents are defNames so entries for series mods that are not loaded are skipped.
    /// </summary>
    public class StorytellerIncidentWeightsDef : Def
    {
        public string? storyteller;
        public List<IncidentWeight> weights = new List<IncidentWeight>();
    }

    public static class StorytellerIncidentWeights
    {
        // Keyed by def reference: no string hashing on the incident-pick path.
        private static Dictionary<StorytellerDef, Dictionary<IncidentDef, float>>? byStoryteller;

        public static float FactorFor(StorytellerDef? teller, IncidentDef? incident)
        {
            if (teller == null || incident == null)
            {
                return 1f;
            }
            byStoryteller ??= Build();
            return byStoryteller.TryGetValue(teller, out Dictionary<IncidentDef, float>? table)
                && table.TryGetValue(incident, out float factor)
                ? factor
                : 1f;
        }

        private static Dictionary<StorytellerDef, Dictionary<IncidentDef, float>> Build()
        {
            var built = new Dictionary<StorytellerDef, Dictionary<IncidentDef, float>>();
            foreach (StorytellerIncidentWeightsDef def in DefDatabase<StorytellerIncidentWeightsDef>.AllDefsListForReading)
            {
                StorytellerDef? teller = def.storyteller.NullOrEmpty()
                    ? null
                    : DefDatabase<StorytellerDef>.GetNamedSilentFail(def.storyteller);
                if (teller == null || def.weights == null)
                {
                    continue;
                }
                if (!built.TryGetValue(teller, out Dictionary<IncidentDef, float>? table))
                {
                    table = new Dictionary<IncidentDef, float>();
                    built[teller] = table;
                }
                foreach (IncidentWeight w in def.weights)
                {
                    if (w == null || w.incident.NullOrEmpty())
                    {
                        continue;
                    }
                    IncidentDef? incident = DefDatabase<IncidentDef>.GetNamedSilentFail(w.incident);
                    if (incident != null)
                    {
                        table[incident] = Mathf.Max(0f, w.factor);
                    }
                }
            }
            return built;
        }
    }

    // Every vanilla comp picks within a category by IncidentChanceFinal weight.
    // Argument types are pinned and the incident is bound by position (__0), so an added
    // overload or a parameter rename fails loudly in the hub instead of binding wrong.
    [HarmonyPatch(typeof(StorytellerComp), "IncidentChanceFinal", new[] { typeof(IncidentDef), typeof(IIncidentTarget) })]
    public static class Patch_StorytellerComp_IncidentChanceFinal
    {
        public static void Postfix(IncidentDef __0, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }
            float factor = StorytellerIncidentWeights.FactorFor(Find.Storyteller?.def, __0);
            if (factor != 1f)
            {
                __result *= factor;
            }
        }
    }
}
