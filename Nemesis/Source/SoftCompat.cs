using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Soft detection of sibling mods via packageId / defName / reflection. Never hard-requires them.
    /// </summary>
    public static class SoftCompat
    {
        public const string StormproofId = "AzraelGodKing.Stormproof";
        public const string StrataId = "AzraelGodKing.Strata";
        public const string HomesteaderId = "AzraelGodKing.Homesteader";

        private static bool _stormChecked;
        private static bool _stormActive;
        private static bool _strataChecked;
        private static bool _strataActive;
        private static bool _homeChecked;
        private static bool _homeActive;

        private static MethodInfo _strataIsUnderground;
        private static MethodInfo _strataIsUpper;
        private static MethodInfo _homeGetFavorites;
        private static bool _strataBurrowProbed;
        private static bool _strataBurrowAvailable;
        private static IncidentDef _strataDeepRaidDef;

        public static bool StormproofActive
        {
            get
            {
                if (!_stormChecked)
                {
                    _stormChecked = true;
                    _stormActive = ModLister.GetActiveModWithIdentifier(StormproofId) != null;
                }
                return _stormActive;
            }
        }

        public static bool StrataActive
        {
            get
            {
                if (!_strataChecked)
                {
                    _strataChecked = true;
                    _strataActive = ModLister.GetActiveModWithIdentifier(StrataId) != null;
                    if (_strataActive)
                    {
                        Type util = GenTypes.GetTypeInAnyAssembly("Strata.StrataMapUtility", "Strata");
                        if (util != null)
                        {
                            _strataIsUnderground = util.GetMethod("IsUnderground", BindingFlags.Public | BindingFlags.Static);
                            _strataIsUpper = util.GetMethod("IsUpperLevel", BindingFlags.Public | BindingFlags.Static);
                        }
                    }
                }
                return _strataActive;
            }
        }

        public static bool HomesteaderActive
        {
            get
            {
                if (!_homeChecked)
                {
                    _homeChecked = true;
                    _homeActive = ModLister.GetActiveModWithIdentifier(HomesteaderId) != null;
                    if (_homeActive)
                    {
                        Type util = GenTypes.GetTypeInAnyAssembly("Homesteader.FavoriteFoodUtility", "Homesteader");
                        _homeGetFavorites = util?.GetMethod("GetFavorites", BindingFlags.Public | BindingFlags.Static);
                    }
                }
                return _homeActive;
            }
        }

        /// <summary>True when Strata's deep/burrowing raid incident can be resolved by defName.</summary>
        public static bool StrataBurrowAvailable
        {
            get
            {
                EnsureStrataBurrowProbe();
                return _strataBurrowAvailable;
            }
        }

        public static void ResetCaches()
        {
            _stormChecked = _strataChecked = _homeChecked = false;
            _stormActive = _strataActive = _homeActive = false;
            _strataIsUnderground = _strataIsUpper = null;
            _homeGetFavorites = null;
            _strataBurrowProbed = false;
            _strataBurrowAvailable = false;
            _strataDeepRaidDef = null;
        }

        private static void EnsureStrataBurrowProbe()
        {
            if (_strataBurrowProbed) return;
            _strataBurrowProbed = true;
            _strataBurrowAvailable = false;
            _strataDeepRaidDef = null;
            if (!StrataActive) return;
            try
            {
                // Prefer known defName; also accept a worker type probe for fail-open depth.
                IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("Strata_DeepRaid");
                if (def?.Worker == null)
                {
                    Type worker = GenTypes.GetTypeInAnyAssembly("Strata.IncidentWorker_DeepRaid", "Strata");
                    if (worker == null) return;
                    // Worker type exists but def missing — still not fireable.
                    return;
                }
                _strataDeepRaidDef = def;
                _strataBurrowAvailable = true;
            }
            catch
            {
                _strataBurrowAvailable = false;
            }
        }

        public static bool IsStrataUnderground(Map map)
        {
            if (!StrataActive || map == null || _strataIsUnderground == null) return false;
            try { return (bool)_strataIsUnderground.Invoke(null, new object[] { map }); }
            catch { return false; }
        }

        public static bool IsStrataUpper(Map map)
        {
            if (!StrataActive || map == null || _strataIsUpper == null) return false;
            try { return (bool)_strataIsUpper.Invoke(null, new object[] { map }); }
            catch { return false; }
        }

        /// <summary>Prefer surface player-home maps when Strata multi-level stacks exist.</summary>
        public static Map PreferHarassmentMap(Map fallback)
        {
            if (!StrataActive) return fallback;
            Map best = null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m == null || !m.IsPlayerHome) continue;
                if (IsStrataUnderground(m) || IsStrataUpper(m)) continue;
                return m;
            }
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m != null && m.IsPlayerHome) { best = m; break; }
            }
            return best ?? fallback;
        }

        public static bool IsBuildingEmpProtected(Building building)
        {
            if (building?.Map == null || !StormproofActive) return false;
            const float radius = 14.9f;
            List<Thing> things = building.Map.listerThings.ThingsOfDef(
                DefDatabase<ThingDef>.GetNamedSilentFail("Stormproof_EmpDampener"));
            if (things == null || things.Count == 0) return false;
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t == null || t.Destroyed || !t.Spawned) continue;
                CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn) continue;
                if (t.Position.DistanceTo(building.Position) <= radius)
                    return true;
            }
            return false;
        }

        public static bool MapHasReadySurgeProtector(Map map)
        {
            if (map == null || !StormproofActive) return false;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Stormproof_SurgeProtector");
            if (def == null) return false;
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things == null) return false;
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t == null || t.Destroyed || !t.Spawned) continue;
                CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
                if (power == null || power.PowerOn) return true;
            }
            return false;
        }

        public static string TryFavoriteFoodLabel(Pawn pawn)
        {
            ThingDef def = TryFavoriteFoodDef(pawn);
            return def?.label;
        }

        public static ThingDef TryFavoriteFoodDef(Pawn pawn)
        {
            if (pawn == null || !HomesteaderActive || _homeGetFavorites == null) return null;
            try
            {
                if (_homeGetFavorites.Invoke(null, new object[] { pawn }) is List<ThingDef> list
                    && list.Count > 0 && list[0] != null)
                    return list[0];
            }
            catch { /* fail open */ }
            return null;
        }

        public static bool MapHasRootCellar(Map map)
        {
            if (map == null || !HomesteaderActive) return false;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_RootCellar");
            if (def == null) return false;
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            return things != null && things.Count > 0;
        }

        /// <summary>Damage Homesteader beehives if present. Returns how many hit.</summary>
        public static int TryVandalizeBeehives(Map map, int maxHits)
        {
            if (map == null || !HomesteaderActive || maxHits <= 0) return 0;
            ThingDef hive = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_Beehive");
            if (hive == null) return 0;
            List<Thing> things = map.listerThings.ThingsOfDef(hive);
            if (things == null || things.Count == 0) return 0;

            List<Thing> candidates = new List<Thing>();
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t != null && !t.Destroyed && t.Faction == Faction.OfPlayer)
                    candidates.Add(t);
            }
            int hit = 0;
            while (candidates.Count > 0 && hit < maxHits)
            {
                int idx = Rand.Range(0, candidates.Count);
                Thing t = candidates[idx];
                candidates.RemoveAt(idx);
                t.TakeDamage(new DamageInfo(DamageDefOf.Blunt, t.MaxHitPoints * Rand.Range(0.4f, 0.9f)));
                hit++;
            }
            return hit;
        }

        /// <summary>
        /// Foul a Homesteader/Wellspring well if present: taunt letter + brief gut-churn hediff on nearby colonists.
        /// </summary>
        public static bool TryFoulWell(Map map, NemesisData data)
        {
            if (map == null || !HomesteaderActive || data == null) return false;
            ThingDef wellDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wellspring_HandDugWell");
            if (wellDef == null)
                wellDef = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_Well");
            if (wellDef == null) return false;

            List<Thing> wells = map.listerThings.ThingsOfDef(wellDef);
            if (wells == null || wells.Count == 0) return false;

            Thing well = wells[Rand.Range(0, wells.Count)];
            HediffDef gut = DefDatabase<HediffDef>.GetNamedSilentFail("FoodPoisoning");
            if (gut != null)
            {
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn p = colonists[i];
                    if (p == null || p.Dead) continue;
                    if (p.Position.DistanceTo(well.Position) > 25f) continue;
                    if (Rand.Chance(0.55f))
                        p.health.AddHediff(gut);
                }
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_WellFoulTitle".Translate(data.nemesisName),
                "Nemesis_Letter_WellFoulBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                LetterDefOf.NegativeEvent,
                well);
            return true;
        }

        /// <summary>
        /// Fire Strata deep/burrow raid via incident worker reflection. Fail-open: never throws.
        /// Prefers an underground Strata map when available.
        /// </summary>
        public static bool TryFireStrataBurrow(NemesisData data, Map surfaceFallback)
        {
            EnsureStrataBurrowProbe();
            if (!_strataBurrowAvailable || _strataDeepRaidDef?.Worker == null || data == null)
                return false;

            Map map = null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m != null && m.IsPlayerHome && IsStrataUnderground(m))
                {
                    map = m;
                    break;
                }
            }
            if (map == null)
                return false; // Deep raid requires underground — do not fire on surface.

            try
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(
                    _strataDeepRaidDef.category ?? IncidentCategoryDefOf.ThreatBig, map);
                parms.forced = true;
                parms.points = Mathf.Max(250f, parms.points * 0.7f);
                if (!_strataDeepRaidDef.Worker.TryExecute(parms))
                    return false;

                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_StrataBurrowTitle".Translate(data.nemesisName),
                    "Nemesis_Letter_StrataBurrowBody".Translate(data.nemesisName, NemesisTaunts.TargetPhrase(data)),
                    LetterDefOf.ThreatBig);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prefer Stormproof brain buildings (LoadShedder / GridMonitor) as first EMP targets.
        /// Appends unprotected matches to <paramref name="priority"/>; returns true if any found.
        /// </summary>
        public static bool CollectStormproofBrainTargets(Map map, List<Building> priority)
        {
            if (map == null || !StormproofActive || priority == null) return false;
            string[] names = { "Stormproof_LoadShedder", "Stormproof_GridMonitor" };
            bool any = false;
            for (int n = 0; n < names.Length; n++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(names[n]);
                if (def == null) continue;
                List<Thing> things = map.listerThings.ThingsOfDef(def);
                if (things == null) continue;
                for (int i = 0; i < things.Count; i++)
                {
                    Building b = things[i] as Building;
                    if (b == null || b.Destroyed) continue;
                    CompPowerTrader power = b.TryGetComp<CompPowerTrader>();
                    if (power == null || !power.PowerOn) continue;
                    if (IsBuildingEmpProtected(b)) continue;
                    priority.Add(b);
                    any = true;
                }
            }
            return any;
        }

        /// <summary>True when a harsh game condition or many colonists on caravan favors firing now.</summary>
        public static bool IsOpportunistWindow(Map map)
        {
            if (map?.GameConditionManager == null) return false;
            try
            {
                GameConditionManager gcm = map.GameConditionManager;
                if (gcm.ConditionIsActive(GameConditionDefOf.Eclipse)) return true;
                GameConditionDef solar = DefDatabase<GameConditionDef>.GetNamedSilentFail("SolarFlare");
                if (solar != null && gcm.ConditionIsActive(solar)) return true;
                GameConditionDef coldSnap = DefDatabase<GameConditionDef>.GetNamedSilentFail("ColdSnap");
                if (coldSnap != null && gcm.ConditionIsActive(coldSnap)) return true;
                GameConditionDef toxic = DefDatabase<GameConditionDef>.GetNamedSilentFail("ToxicFallout");
                if (toxic != null && gcm.ConditionIsActive(toxic)) return true;
                GameConditionDef blizzard = DefDatabase<GameConditionDef>.GetNamedSilentFail("VolcanicWinter");
                if (blizzard != null && gcm.ConditionIsActive(blizzard)) return true;
            }
            catch { /* fail-open */ }

            // Many colonists away on caravans.
            int away = 0;
            int home = map.mapPawns?.FreeColonistsSpawnedCount ?? 0;
            List<RimWorld.Planet.Caravan> caravans = Find.WorldObjects?.Caravans;
            if (caravans != null)
            {
                for (int i = 0; i < caravans.Count; i++)
                {
                    RimWorld.Planet.Caravan c = caravans[i];
                    if (c == null || c.Faction != Faction.OfPlayer) continue;
                    away += c.PawnsListForReading?.Count ?? 0;
                }
            }
            if (away >= 3 && away >= home) return true;

            // Soft Stormproof ion-storm / forecast.
            if (HasStormproofThreatCondition(map)) return true;
            return false;
        }

        public static bool HasStormproofThreatCondition(Map map)
        {
            if (!StormproofActive || map?.GameConditionManager == null) return false;
            try
            {
                string[] names = { "Stormproof_IonStorm", "Stormproof_ForecastAlert", "IonStorm" };
                for (int i = 0; i < names.Length; i++)
                {
                    GameConditionDef def = DefDatabase<GameConditionDef>.GetNamedSilentFail(names[i]);
                    if (def != null && map.GameConditionManager.ConditionIsActive(def))
                        return true;
                }
            }
            catch { /* fail-open */ }
            return false;
        }
    }
}
