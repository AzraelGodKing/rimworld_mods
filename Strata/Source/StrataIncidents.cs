using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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

        public static IncidentDef Strata_DeepRaid;

        public static IncidentDef Strata_FloodSeep;

        public static IncidentDef Strata_GasFirestorm;

        public static IncidentDef Strata_DeepSiege;

        public static IncidentDef Strata_CaveBreakthrough;

        public static IncidentDef Strata_ProspectorDig;

        public static IncidentDef Strata_LostMiners;

        static StrataIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataIncidentDefOf));
        }

        public static bool IsStrataUndergroundEvent(IncidentDef def)
        {
            return def == Strata_CaveIn || def == Strata_GasPocket
                || def == Strata_DeepVein || def == Strata_DeepRaid
                || def == Strata_FloodSeep || def == Strata_GasFirestorm
                || def == Strata_CaveBreakthrough || def == Strata_LostMiners;
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

        // Same relative position on another level's grid — used for landings,
        // shaft junctions, and camera jumps so things stack vertically.
        public static IntVec3 ProportionalCell(IntVec3 pos, Map from, Map to)
        {
            if (from == null || to == null || from.Size.x <= 0 || from.Size.z <= 0)
            {
                return to?.Center ?? IntVec3.Invalid;
            }
            int x = Mathf.Clamp(Mathf.RoundToInt((float)pos.x / from.Size.x * to.Size.x), 0, to.Size.x - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((float)pos.z / from.Size.z * to.Size.z), 0, to.Size.z - 1);
            return new IntVec3(x, 0, z);
        }

        // Proportional alignment, nudging inward when the spot hugs the map edge.
        public static IntVec3 VerticalAlign(IntVec3 pos, Map from, Map to, float searchRadius = 25f, int edgeMargin = 8)
        {
            IntVec3 target = ProportionalCell(pos, from, to);
            if (target.InBounds(to) && target.DistanceToEdge(to) >= edgeMargin)
            {
                return target;
            }
            IntVec3 origin = target.IsValid ? target : to.Center;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, searchRadius, useCenter: true))
            {
                if (cell.InBounds(to) && cell.DistanceToEdge(to) >= edgeMargin)
                {
                    return cell;
                }
            }
            return to.Center;
        }
    }

    // Underground levels are pocket maps, and pocket maps have NO incident
    // target tags in vanilla - the storyteller (and even the debug incident
    // menu) never targets them with anything. Tag Strata levels as player-home
    // incident targets so infestations, diseases, and Strata's own events can
    // fire down there; the CanFireNow patch below filters out everything that
    // can't physically reach sealed rock.
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.IncidentTargetTags))]
    public static class Patch_UndergroundIncidentTargets
    {
        public static IEnumerable<IncidentTargetTagDef> Postfix(IEnumerable<IncidentTargetTagDef> values, MapParent __instance)
        {
            bool hasPlayerHome = false;
            foreach (IncidentTargetTagDef tag in values)
            {
                hasPlayerHome |= tag == IncidentTargetTagDefOf.Map_PlayerHome;
                yield return tag;
            }
            if (!hasPlayerHome && __instance is PocketMapParent && __instance.HasMap
                && StrataMapUtility.IsUnderground(__instance.Map))
            {
                yield return IncidentTargetTagDefOf.Map_PlayerHome;
            }
        }
    }

    // Enforces the "underground is sealed rock" fiction: sky and weather
    // conditions and raids/threats that can't physically reach a buried level
    // never fire down there. Infestations (which erupt from within) and
    // Strata's own underground events are always allowed. Without this filter,
    // the target tag above would let the storyteller drop solar flares and
    // mech clusters on a rock vault.
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
            // Strata's own events (cave-in, gas, deep vein, deep raid).
            if (def.defName.Contains("Infestation") || StrataIncidentDefOf.IsStrataUndergroundEvent(def))
            {
                return;
            }

            // Allow things that make sense in a buried, occupied level regardless
            // of the sky: disease, deep-drill bugs, and abstract world quests.
            IncidentCategoryDef cat = def.category;
            if (cat == IncidentCategoryDefOf.DiseaseHuman
                || cat == IncidentCategoryDefOf.DeepDrillInfestation
                || cat == IncidentCategoryDefOf.GiveQuest)
            {
                return;
            }

            // Block everything that has to arrive from outside a sealed rock
            // level: raids, sieges, mech clusters (ThreatBig/Small), and the
            // sky/edge arrivals that live in Misc/Special - drop pods, crashing
            // ship chunks, wanderers walking in, resource pods, etc.
            if (cat == IncidentCategoryDefOf.ThreatBig
                || cat == IncidentCategoryDefOf.ThreatSmall
                || cat == IncidentCategoryDefOf.Misc
                || cat == IncidentCategoryDefOf.Special)
            {
                __result = false;
                return;
            }

            // Any remaining sky/weather condition.
            if (def.gameCondition != null && BlockedConditions.Contains(def.gameCondition.defName))
            {
                __result = false;
            }
        }
    }

    // Dev-mode diagnostics: deep raid is Strata's only ThreatBig incident, and
    // vanilla gates ThreatBig behind checks that fail silently (big threats
    // disabled in difficulty, post-wipe wanderer grace, refire cooldown).
    // When it refuses to fire in dev mode, say which gate did it.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    public static class Patch_DeepRaidDiagnostics
    {
        // The storyteller probes CanFireNow for every candidate incident on a
        // regular cadence; without a cooldown this line floods a dev-mode log.
        private const int LogCooldownTicks = 2500;

        private static int lastLogTick = -99999;

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (__result || !Prefs.DevMode || __instance.def != StrataIncidentDefOf.Strata_DeepRaid)
            {
                return;
            }
            if (!parms.forced && Find.TickManager.TicksGame - lastLogTick < LogCooldownTicks)
            {
                return;
            }
            lastLogTick = Find.TickManager.TicksGame;
            bool firedRecently = false;
            try
            {
                firedRecently = (bool)AccessTools.Method(typeof(IncidentWorker), "FiredTooRecently")
                    .Invoke(__instance, new object[] { parms.target });
            }
            catch
            {
            }
            Log.Message("[Strata] Deep raid blocked. Vanilla gates: "
                + $"targetAllowed={__instance.def.TargetAllowed(parms.target)}, "
                + $"allowBigThreats={Find.Storyteller?.difficulty?.allowBigThreats}, "
                + $"firedTooRecently={firedRecently}, "
                + $"gameEnding={Find.GameEnder.gameEnding}, "
                + $"wanderersGraceActive={Find.TickManager.TicksGame < Find.GameEnder.newWanderersCreatedTick + 300000}, "
                + $"points={parms.points}");
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
                // Deeper levels crawl with more bugs.
                __result *= Mathf.Min(1.3f + 0.35f * StrataDepth.Of(map), 3f);
            }
        }
    }
}
