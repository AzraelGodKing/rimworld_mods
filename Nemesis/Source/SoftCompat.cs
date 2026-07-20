using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
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

        public static void ResetCaches()
        {
            _stormChecked = _strataChecked = _homeChecked = false;
            _stormActive = _strataActive = _homeActive = false;
            _strataIsUnderground = _strataIsUpper = null;
            _homeGetFavorites = null;
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
            if (pawn == null || !HomesteaderActive || _homeGetFavorites == null) return null;
            try
            {
                if (_homeGetFavorites.Invoke(null, new object[] { pawn }) is List<ThingDef> list
                    && list.Count > 0 && list[0] != null)
                    return list[0].label;
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
    }
}
