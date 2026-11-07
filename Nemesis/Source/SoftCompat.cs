#nullable enable
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
        public const string DeepColonyId = "azraelgodking.DeepColony";

        /// <summary>Homesteader pantry / smokehouse / cellar storage buildings for food raids.</summary>
        private static readonly HashSet<string> HomesteaderFoodStorageDefNames = new HashSet<string>
        {
            "Homesteader_Smokehouse",
            "Homesteader_RootCellar",
            "Homesteader_PreservesShelf",
            "Homesteader_StorageCrate",
            "Homesteader_StorageBarrel",
            "Homesteader_LargeStorageCrate",
            "Homesteader_IngredientBarrel",
            "Homesteader_Icehouse",
            "Homesteader_Springhouse",
        };

        private static bool _stormChecked;
        private static bool _stormActive;
        private static bool _strataChecked;
        private static bool _strataActive;
        private static bool _homeChecked;
        private static bool _homeActive;
        private static bool _deepChecked;
        private static bool _cellarChecked;
        private static ThingDef? _rootCellarDef;

        // Surface home map, rechecked at most every SurfaceMapRecheckTicks.
        private const int SurfaceMapRecheckTicks = 250;
        private static Map? _surfaceMap;
        private static int _surfaceMapTick = int.MinValue / 2;

        // Bound once as typed delegates: no object[] or boxing per call (AZR-535).
        private static Func<Map, bool>? _strataIsUnderground;
        private static Func<Map, bool>? _strataIsUpper;
        private static Func<Pawn, List<ThingDef>>? _homeGetFavorites;
        private static Action<string, string, Pawn?, LetterDef?>? _deepRecordNote;

        private static TDelegate? Bind<TDelegate>(string typeName, string method) where TDelegate : Delegate
        {
            try
            {
                Type? type = GenTypes.GetTypeInAnyAssembly(typeName);
                MethodInfo? mi = type?.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
                return mi == null ? null : Delegate.CreateDelegate(typeof(TDelegate), mi, throwOnBindFailure: false) as TDelegate;
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Could not bind " + typeName + "." + method + ": " + e.Message);
                return null;
            }
        }

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
                        // Strata's public modder API, not its internal map utility.
                        _strataIsUnderground = Bind<Func<Map, bool>>("Strata.StrataModderApi", "IsUnderground");
                        _strataIsUpper = Bind<Func<Map, bool>>("Strata.StrataModderApi", "IsUpperLevel");
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
                        _homeGetFavorites = Bind<Func<Pawn, List<ThingDef>>>("Homesteader.FavoriteFoodUtility", "GetFavorites");
                }
                return _homeActive;
            }
        }

        /// <summary>
        /// True if another antagonist mod already owns this pawn (hediff/comp name
        /// markers). Fail-open: unknown markers → false.
        /// </summary>
        public static bool IsForeignAntagonistPawn(Pawn? pawn)
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
                    string? name = hediffs[i]?.def?.defName;
                    if (name == null || name.Length == 0)
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
            _stormChecked = _strataChecked = _homeChecked = _deepChecked = false;
            _stormActive = _strataActive = _homeActive = false;
            _strataIsUnderground = _strataIsUpper = null;
            _homeGetFavorites = null;
            _deepRecordNote = null;
            _cellarChecked = false;
            _rootCellarDef = null;
            _surfaceMap = null;
            _surfaceMapTick = int.MinValue / 2;
            _warcasketChecked = false;
            _warcasketKinds = null;
        }

        /// <summary>
        /// Fail-open copy of a hunt epitaph onto Deep Colony's Legacy letters, through its public
        /// FamilyLetterUtility.RecordNote (no letter), so its own trim and save rules apply (AZR-556).
        /// Azrael's series hub looks this method up by name: keep the name.
        /// </summary>
        public static void OfferEpitaphToDeepColony(string title, string body)
        {
            if (!_deepChecked)
            {
                _deepChecked = true;
                if (ModLister.GetActiveModWithIdentifier(DeepColonyId, ignorePostfix: true) != null)
                    _deepRecordNote = Bind<Action<string, string, Pawn?, LetterDef?>>("DeepColony.FamilyLetterUtility", "RecordNote");
            }
            if (_deepRecordNote == null) return;
            try
            {
                _deepRecordNote(title, body, null, null);
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Deep Colony epitaph handoff skipped: " + e.Message);
            }
        }

        /// <summary>
        /// Remember a faction-appropriate animal kind for later escort spawn.
        /// The animal fights beside the captain; no Giddy-Up gate.
        /// </summary>
        public static void TryAssignMountKind(Pawn? rider, NemesisData? data, int level)
        {
            if (data == null || level < 2) return;
            if (!(NemesisMod.Settings?.enableSoftMounts ?? true)) return;
            if (!string.IsNullOrEmpty(data.mountKindDefName)) return;

            TechLevel tech = rider?.Faction?.def?.techLevel ?? TechLevel.Neolithic;
            string kindName;
            if (tech <= TechLevel.Neolithic)
                kindName = Rand.Bool ? "Elephant" : "Muffalo";
            else if (tech <= TechLevel.Medieval)
                kindName = Rand.Bool ? "Horse" : "Muffalo";
            else
                kindName = Rand.Bool ? "Horse" : "Thrumbo";

            PawnKindDef? kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName)
                               ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Muffalo")
                               ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Horse");
            if (kind == null || !kind.RaceProps.Animal) return;
            data.mountKindDefName = kind.defName;
        }

        public static void TrySpawnMountBeside(Map? map, Faction? faction, NemesisData? data, Pawn? near)
        {
            if (data == null || map == null || faction == null) return;
            if (!(NemesisMod.Settings?.enableSoftMounts ?? true)) return;
            if (string.IsNullOrEmpty(data.mountKindDefName)) return;

            PawnKindDef? kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(data.mountKindDefName);
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

                Lord? lord = near?.GetLord();
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
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Mount escort spawn failed (" + kind.defName + "): " + e.Message);
            }
        }

        private const int WarcasketMinLevel = 5;
        private static bool _warcasketChecked;
        private static List<PawnKindDef>? _warcasketKinds;

        /// <summary>Humanlike pawn kinds named like VFE Pirates warcaskets; empty without that mod.</summary>
        private static List<PawnKindDef> WarcasketKinds
        {
            get
            {
                if (!_warcasketChecked)
                {
                    _warcasketChecked = true;
                    _warcasketKinds = new List<PawnKindDef>();
                    foreach (PawnKindDef k in DefDatabase<PawnKindDef>.AllDefsListForReading)
                    {
                        if (k.defName.IndexOf("Warcasket", StringComparison.OrdinalIgnoreCase) >= 0
                            && k.RaceProps?.Humanlike == true)
                            _warcasketKinds.Add(k);
                    }
                }
                return _warcasketKinds!;
            }
        }

        /// <summary>
        /// Soft warcasket support: a high-level Destroyer or Survivor brings one warcasket
        /// (VFE Pirates) of their own faction if it has one, else any. Joins the captain's lord.
        /// </summary>
        public static void TrySpawnWarcasketBeside(Map? map, Faction? faction, NemesisData? data, Pawn? near)
        {
            if (map == null || faction == null || data == null) return;
            if (!(NemesisMod.Settings?.enableSoftMechs ?? true)) return;
            if (data.progressionLevel < WarcasketMinLevel) return;
            if (data.combatFocus != NemesisCombatFocus.Destroyer && data.combatFocus != NemesisCombatFocus.Survivor) return;
            if (faction.def.techLevel < TechLevel.Industrial) return;
            List<PawnKindDef> kinds = WarcasketKinds;
            if (kinds.Count == 0) return;

            PawnKindDef? kind = null;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i].defaultFactionDef == faction.def) { kind = kinds[i]; break; }
            }
            kind ??= kinds.RandomElement();
            try
            {
                Pawn casket = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                IntVec3 cell = IntVec3.Invalid;
                if (near != null && near.Spawned && near.Map == map)
                    CellFinder.TryFindRandomSpawnCellForPawnNear(near.Position, map, out cell, 4);
                if (!cell.IsValid)
                    CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, 0f, out cell);
                if (!cell.IsValid) return;
                GenSpawn.Spawn(casket, cell, map);
                Lord? lord = near?.GetLord();
                if (lord != null)
                    lord.AddPawn(casket);
                else
                    LordMaker.MakeNewLord(faction,
                        new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: true),
                        map, new[] { casket });
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Warcasket escort spawn failed (" + kind.defName + "): " + e.Message);
            }
        }

        public static bool IsStrataUnderground(Map? map)
        {
            if (!StrataActive || map == null || _strataIsUnderground == null) return false;
            try { return _strataIsUnderground(map); }
            catch { return false; }
        }

        public static bool IsStrataUpper(Map? map)
        {
            if (!StrataActive || map == null || _strataIsUpper == null) return false;
            try { return _strataIsUpper(map); }
            catch { return false; }
        }

        /// <summary>Prefer surface player-home maps when Strata multi-level stacks exist.</summary>
        public static Map? PreferHarassmentMap(Map? fallback)
        {
            if (!StrataActive) return fallback;
            int now = Find.TickManager?.TicksGame ?? 0;
            Map? cached = _surfaceMap;
            if (now - _surfaceMapTick < SurfaceMapRecheckTicks
                && cached != null && cached.IsPlayerHome && Find.Maps.Contains(cached))
                return cached;
            Map? found = FindSurfaceHomeMap();
            _surfaceMap = found;
            _surfaceMapTick = now;
            return found ?? fallback;
        }

        private static Map? FindSurfaceHomeMap()
        {
            Map? anyHome = null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map m = maps[i];
                if (m == null || !m.IsPlayerHome) continue;
                if (!IsStrataUnderground(m) && !IsStrataUpper(m)) return m;
                anyHome ??= m;
            }
            return anyHome;
        }

        // Live dampener cover (radius and power) through Stormproof's compat API.
        public static bool IsBuildingEmpProtected(Building building) =>
            StormproofActive && AzraelCommon.StormproofClient.IsEmpProtected(building);

        public static bool MapHasReadySurgeProtector(Map map) =>
            StormproofActive && AzraelCommon.StormproofClient.MapHasReadySurgeProtector(map);

        public static string? TryFavoriteFoodLabel(Pawn? pawn)
        {
            if (pawn == null || !HomesteaderActive || _homeGetFavorites == null) return null;
            try
            {
                List<ThingDef>? list = _homeGetFavorites(pawn);
                if (list != null && list.Count > 0 && list[0] != null)
                    return list[0].label;
            }
            catch { /* fail open */ }
            return null;
        }

        public static bool MapHasRootCellar(Map? map)
        {
            if (map == null || !HomesteaderActive) return false;
            if (!_cellarChecked)
            {
                _cellarChecked = true;
                _rootCellarDef = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_RootCellar");
            }
            ThingDef? def = _rootCellarDef;
            if (def == null) return false;
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            return things != null && things.Count > 0;
        }

        public static bool IsHomesteaderFoodStorageDef(string? defName) =>
            defName != null && HomesteaderFoodStorageDefNames.Contains(defName);

        /// <summary>True when the stack sits in / on a Homesteader pantry, cellar, or smokehouse.</summary>
        public static bool IsOnHomesteaderFoodStorage(Thing? thing)
        {
            if (thing?.Map == null || !HomesteaderActive) return false;
            try
            {
                // Haulable stacks in Building_Storage slot groups.
                SlotGroup? group = thing.GetSlotGroup();
                if (group?.parent is Building storage
                    && IsHomesteaderFoodStorageDef(storage.def?.defName))
                    return true;

                // Smokehouse / workbench: food sitting on the building's cells.
                Building? edifice = thing.Position.GetEdifice(thing.Map);
                if (edifice != null && IsHomesteaderFoodStorageDef(edifice.def?.defName))
                    return true;

                // Fallback: same cell as a Homesteader food-storage building.
                List<Thing>? at = thing.Map.thingGrid.ThingsListAtFast(thing.Position);
                if (at == null) return false;
                for (int i = 0; i < at.Count; i++)
                {
                    if (at[i] is Building b && IsHomesteaderFoodStorageDef(b.def?.defName))
                        return true;
                }
            }
            catch
            {
                /* fail open */
            }
            return false;
        }
    }
}
