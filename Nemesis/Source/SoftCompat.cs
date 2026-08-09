using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI.Group;

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

        private static readonly string[] GiddyUpPackageIds =
        {
            "Owlchemist.GiddyUp",
            "roolo.giddyupcore",
            "roolo.giddyuprideandroll",
            "roolo.giddyupcaravan",
        };

        private static readonly string[] RimesisPackageIds =
        {
            "Font.Rimesis",
            "font.rimesis",
            "Rimesis",
        };

        private static readonly string[] BfvPackageIds =
        {
            "SmashPhil.BackForVengeance",
            "smashphil.backforvengeance",
            "BackForVengeance",
            "VanillaExpanded.BackForVengeance",
        };

        private static bool _stormChecked;
        private static bool _stormActive;
        private static bool _strataChecked;
        private static bool _strataActive;
        private static bool _homeChecked;
        private static bool _homeActive;
        private static bool _giddyChecked;
        private static bool _giddyActive;
        private static bool _rimesisChecked;
        private static bool _rimesisActive;
        private static bool _bfvChecked;
        private static bool _bfvActive;

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

        public static bool GiddyUpActive
        {
            get
            {
                if (!_giddyChecked)
                {
                    _giddyChecked = true;
                    _giddyActive = false;
                    for (int i = 0; i < GiddyUpPackageIds.Length; i++)
                    {
                        if (ModLister.GetActiveModWithIdentifier(GiddyUpPackageIds[i]) != null)
                        {
                            _giddyActive = true;
                            break;
                        }
                    }
                }
                return _giddyActive;
            }
        }

        public static bool RimesisActive
        {
            get
            {
                if (!_rimesisChecked)
                {
                    _rimesisChecked = true;
                    _rimesisActive = AnyPackageActive(RimesisPackageIds);
                }
                return _rimesisActive;
            }
        }

        public static bool BackForVengeanceActive
        {
            get
            {
                if (!_bfvChecked)
                {
                    _bfvChecked = true;
                    _bfvActive = AnyPackageActive(BfvPackageIds);
                }
                return _bfvActive;
            }
        }

        /// <summary>
        /// True if another antagonist mod already owns this pawn (hediff/comp name
        /// markers). Fail-open: unknown markers → false.
        /// </summary>
        public static bool IsForeignAntagonistPawn(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            try
            {
                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    string name = hediffs[i]?.def?.defName;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    if (name.IndexOf("Rimesis", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("BFV", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("BackForVengeance", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Vengeance", StringComparison.OrdinalIgnoreCase) >= 0
                           && name.IndexOf("Nemesis", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                /* fail open */
            }

            return false;
        }

        public static void ResetCaches()
        {
            _stormChecked = _strataChecked = _homeChecked = _giddyChecked = false;
            _rimesisChecked = _bfvChecked = false;
            _stormActive = _strataActive = _homeActive = _giddyActive = false;
            _rimesisActive = _bfvActive = false;
            _strataIsUnderground = _strataIsUpper = null;
            _homeGetFavorites = null;
        }

        private static bool AnyPackageActive(string[] ids)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (ModLister.GetActiveModWithIdentifier(ids[i], ignorePostfix: true) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remember a faction-appropriate animal kind for later escort spawn.
        /// Fail-open without Giddy-Up (animal still fights beside the captain).
        /// </summary>
        public static void TryAssignMountKind(Pawn rider, NemesisData data, int level)
        {
            if (data == null || level < 2) return;
            if (!(NemesisMod.Settings?.enableSoftMounts ?? true)) return;
            if (!string.IsNullOrEmpty(data.mountKindDefName)) return;

            // Prefer assigning when Giddy-Up is present; still allow animal escort without it.
            TechLevel tech = rider?.Faction?.def?.techLevel ?? TechLevel.Neolithic;
            string kindName;
            if (tech <= TechLevel.Neolithic)
                kindName = Rand.Bool ? "Elephant" : "Muffalo";
            else if (tech <= TechLevel.Medieval)
                kindName = Rand.Bool ? "Horse" : "Muffalo";
            else
                kindName = Rand.Bool ? "Horse" : "Thrumbo";

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName)
                               ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Muffalo")
                               ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Horse");
            if (kind == null || !kind.RaceProps.Animal) return;
            data.mountKindDefName = kind.defName;
        }

        public static void TrySpawnMountBeside(Map map, Faction faction, NemesisData data, Pawn near)
        {
            if (data == null || map == null || faction == null) return;
            if (!(NemesisMod.Settings?.enableSoftMounts ?? true)) return;
            if (string.IsNullOrEmpty(data.mountKindDefName)) return;

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(data.mountKindDefName);
            if (kind == null) return;

            try
            {
                PawnGenerationRequest req = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false);
                Pawn animal = PawnGenerator.GeneratePawn(req);
                IntVec3 cell = IntVec3.Invalid;
                if (near != null && near.Spawned && near.Map == map)
                    CellFinder.TryFindRandomSpawnCellForPawnNear(near.Position, map, out cell, 3);
                if (!cell.IsValid)
                    CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out cell);
                if (!cell.IsValid) return;
                GenSpawn.Spawn(animal, cell, map);

                Lord lord = near?.GetLord();
                if (lord != null)
                    lord.AddPawn(animal);
                else
                {
                    LordMaker.MakeNewLord(
                        faction,
                        new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: true),
                        map,
                        new[] { animal });
                }
            }
            catch
            {
                /* fail open */
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
