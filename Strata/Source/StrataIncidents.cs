using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataIncidentDefOf
    {
        public static IncidentDef Strata_CaveIn;

        public static IncidentDef Strata_GasPocket;

        public static IncidentDef Strata_DeepVein;

        public static IncidentDef Strata_ProspectorTip;

        public static IncidentDef Strata_Tremor;

        static StrataIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataIncidentDefOf));
        }

        public static bool IsStrataUndergroundEvent(IncidentDef def)
        {
            return def == Strata_CaveIn || def == Strata_GasPocket || def == Strata_DeepVein;
        }
    }

    public static class StrataMapUtility
    {
        public const string UndergroundBiome = "Strata_Underground";

        public static bool IsUnderground(Map map)
        {
            return map?.Biome != null && map.Biome.defName == UndergroundBiome;
        }

        public static bool IsSurfacePlayerHome(Map map)
        {
            return map != null && map.IsPlayerHome && !IsUnderground(map);
        }
    }

    // Enforces the "underground is sealed rock" fiction: sky and weather
    // conditions and raids/threats that can't physically reach a buried level
    // never fire down there. Infestations (which erupt from within) and
    // Strata's own underground events are always allowed. Vanilla already
    // treats a pocket map as a player home, so without this the storyteller
    // would happily drop solar flares and mech clusters on a rock vault.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    public static class Patch_UndergroundIncidents
    {
        // Sky / climate game conditions that make no sense underground.
        private static readonly HashSet<string> BlockedConditions = new HashSet<string>
        {
            "Eclipse", "SolarFlare", "ToxicFallout", "Aurora", "Flashstorm",
            "HeatWave", "ColdSnap", "VolcanicWinter", "NoxiousHaze", "BloodRain",
            "Drought", "LavaFlow", "GrayPall", "DeathPall", "UnnaturalHeat",
            "UnnaturalDarkness",
        };

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result || !(parms.target is Map map) || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            IncidentDef def = __instance.def;

            // Always allow: infestations (the signature underground threat) and
            // Strata's own cave-in / gas / deep-vein events.
            if (def.defName.Contains("Infestation") || StrataIncidentDefOf.IsStrataUndergroundEvent(def))
            {
                return;
            }

            // Raids, sieges, manhunter packs, mech clusters - nothing can reach a
            // sealed rock level from the sky or a map edge.
            if (def.category == IncidentCategoryDefOf.ThreatBig
                || def.category == IncidentCategoryDefOf.ThreatSmall)
            {
                __result = false;
                return;
            }

            // Sky and weather conditions.
            if (def.gameCondition != null && BlockedConditions.Contains(def.gameCondition.defName))
            {
                __result = false;
            }
        }
    }

    // With every other threat suppressed underground, lean into bugs as the
    // defining danger of the deep: infestations are meaningfully more likely on
    // a Strata level than they would be under an ordinary mountain.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.ChanceFactorNow))]
    public static class Patch_UndergroundInfestationWeight
    {
        public static void Postfix(IncidentWorker __instance, IIncidentTarget target, ref float __result)
        {
            if (__result <= 0f || !__instance.def.defName.Contains("Infestation"))
            {
                return;
            }
            if (target is Map map && StrataMapUtility.IsUnderground(map))
            {
                __result *= 1.6f;
            }
        }
    }
}
