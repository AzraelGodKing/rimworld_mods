using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class GameComponent_HomesteaderFavorites : GameComponent
    {
        public const int FavoriteCount = 5;
        private const int EnvironmentalAllergyCheckInterval = 250;

        // Packed "DefA|DefB|..." per pawn. Legacy favoritesByPawnId (single def) migrates on load.
        // Allergies pack AllergyCatalog ids (or empty = rolled None). Key present means rolled.
        private Dictionary<int, string> favoritesByPawnId;
        private Dictionary<int, string> favoriteListsPacked = new Dictionary<int, string>();
        private Dictionary<int, string> allergiesPacked = new Dictionary<int, string>();
        private Dictionary<int, string> discoveredAllergiesPacked = new Dictionary<int, string>();
        private int envAllergyCursor;

        public GameComponent_HomesteaderFavorites(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref favoritesByPawnId, "favoritesByPawnId", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref favoriteListsPacked, "favoriteListsPacked", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref allergiesPacked, "allergiesPacked", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(
                ref discoveredAllergiesPacked,
                "discoveredAllergiesPacked",
                LookMode.Value,
                LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                favoriteListsPacked ??= new Dictionary<int, string>();
                allergiesPacked ??= new Dictionary<int, string>();
                discoveredAllergiesPacked ??= new Dictionary<int, string>();
                MigrateLegacyFavorites();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % EnvironmentalAllergyCheckInterval != 0)
            {
                return;
            }

            // One colonist per pulse — spreads Mold/PetDander map scans (FPS+-style).
            Pawn chosen = null;
            int seen = 0;
            int target = envAllergyCursor;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Pawn> free = maps[m].mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < free.Count; i++)
                {
                    if (seen == target)
                    {
                        chosen = free[i];
                    }
                    seen++;
                }
            }

            if (seen == 0)
            {
                envAllergyCursor = 0;
                return;
            }

            envAllergyCursor = (target + 1) % seen;
            if (chosen != null)
            {
                CheckEnvironmentalAllergies(chosen);
            }
        }

        private void CheckEnvironmentalAllergies(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true || pawn.needs?.mood == null)
            {
                return;
            }

            EnsureTastes(pawn);
            List<string> allergies = GetAllergyNames(pawn.thingIDNumber);
            for (int i = 0; i < allergies.Count; i++)
            {
                AllergyKind kind = AllergyCatalog.Get(allergies[i]);
                if (kind == null || kind.Type != AllergyKindType.Environmental)
                {
                    continue;
                }

                if (!AllergyCatalog.IsEnvironmentallyExposed(pawn, kind))
                {
                    continue;
                }

                FavoriteFoodUtility.ApplyAllergicReaction(pawn, kind.Id);
            }
        }

        private void MigrateLegacyFavorites()
        {
            if (favoritesByPawnId == null || favoritesByPawnId.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<int, string> kv in favoritesByPawnId)
            {
                if (favoriteListsPacked.ContainsKey(kv.Key) || kv.Value.NullOrEmpty())
                {
                    continue;
                }

                favoriteListsPacked[kv.Key] = kv.Value;
            }

            favoritesByPawnId = null;
        }

        private static List<string> Unpack(string packed)
        {
            var list = new List<string>();
            if (packed.NullOrEmpty())
            {
                return list;
            }

            string[] parts = packed.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i].NullOrEmpty())
                {
                    list.Add(parts[i]);
                }
            }

            return list;
        }

        private static string Pack(List<string> list)
        {
            return list == null || list.Count == 0 ? string.Empty : string.Join("|", list);
        }

        private List<string> GetFavoriteNames(int pawnId)
        {
            if (!favoriteListsPacked.TryGetValue(pawnId, out string packed))
            {
                return new List<string>();
            }

            return Unpack(packed);
        }

        private void SetFavoriteNames(int pawnId, List<string> names)
        {
            favoriteListsPacked[pawnId] = Pack(names);
        }

        private List<string> GetAllergyNames(int pawnId)
        {
            if (!allergiesPacked.TryGetValue(pawnId, out string packed))
            {
                return new List<string>();
            }

            return Unpack(packed);
        }

        private void SetAllergyNames(int pawnId, List<string> names)
        {
            allergiesPacked[pawnId] = Pack(names);
        }

        private List<string> GetDiscoveredNames(int pawnId)
        {
            if (!discoveredAllergiesPacked.TryGetValue(pawnId, out string packed))
            {
                return new List<string>();
            }

            return Unpack(packed);
        }

        private void SetDiscoveredNames(int pawnId, List<string> names)
        {
            discoveredAllergiesPacked[pawnId] = Pack(names);
        }

        public List<ThingDef> GetFavorites(Pawn pawn)
        {
            EnsureTastes(pawn);
            return pawn == null ? new List<ThingDef>() : ResolveDefs(GetFavoriteNames(pawn.thingIDNumber));
        }

        public List<string> GetAllergyIds(Pawn pawn)
        {
            EnsureTastes(pawn);
            return pawn == null ? new List<string>() : new List<string>(GetAllergyNames(pawn.thingIDNumber));
        }

        public List<string> GetDiscoveredAllergyIds(Pawn pawn)
        {
            return pawn == null ? new List<string>() : GetDiscoveredNames(pawn.thingIDNumber);
        }

        public bool IsFavorite(Pawn pawn, ThingDef foodDef)
        {
            if (pawn == null || foodDef == null)
            {
                return false;
            }

            EnsureTastes(pawn);
            return GetFavoriteNames(pawn.thingIDNumber).Contains(foodDef.defName);
        }

        public bool HasAllergyId(Pawn pawn, string allergyId)
        {
            if (pawn == null || allergyId.NullOrEmpty())
            {
                return false;
            }

            EnsureTastes(pawn);
            return GetAllergyNames(pawn.thingIDNumber).Contains(allergyId);
        }

        public bool IsFoodAllergy(Pawn pawn, ThingDef foodDef)
        {
            if (pawn == null || foodDef == null)
            {
                return false;
            }

            EnsureTastes(pawn);
            return AllergyCatalog.FoodMatchesAny(GetAllergyNames(pawn.thingIDNumber), foodDef);
        }

        public bool IsDiscoveredFoodAllergy(Pawn pawn, ThingDef foodDef)
        {
            if (pawn == null || foodDef == null)
            {
                return false;
            }

            string match = AllergyCatalog.MatchingFoodAllergyId(GetAllergyNames(pawn.thingIDNumber), foodDef);
            return match != null && GetDiscoveredNames(pawn.thingIDNumber).Contains(match);
        }

        public bool IsDiscoveredAllergyId(Pawn pawn, string allergyId)
        {
            if (pawn == null || allergyId.NullOrEmpty())
            {
                return false;
            }

            return GetDiscoveredNames(pawn.thingIDNumber).Contains(allergyId);
        }

        public void DiscoverAllergy(Pawn pawn, string allergyId)
        {
            if (pawn == null || allergyId.NullOrEmpty() || !HasAllergyId(pawn, allergyId))
            {
                return;
            }

            List<string> names = GetDiscoveredNames(pawn.thingIDNumber);
            if (!names.Contains(allergyId))
            {
                names.Add(allergyId);
                SetDiscoveredNames(pawn.thingIDNumber, names);
            }
        }

        public void EnsureTastes(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true || pawn.needs?.mood == null)
            {
                return;
            }

            int id = pawn.thingIDNumber;
            EnsureAllergiesRolled(id);

            List<string> allergies = GetAllergyNames(id);
            List<string> favorites = GetFavoriteNames(id);
            FillUniqueFavorites(favorites, FavoriteCount, allergies);
            SetFavoriteNames(id, favorites);

            if (!discoveredAllergiesPacked.ContainsKey(id))
            {
                SetDiscoveredNames(id, new List<string>());
            }
            else
            {
                // Drop discoveries that are no longer on this pawn.
                List<string> discovered = GetDiscoveredNames(id);
                discovered.RemoveAll(n => !allergies.Contains(n));
                SetDiscoveredNames(id, discovered);
            }
        }

        private void EnsureAllergiesRolled(int pawnId)
        {
            bool hasKey = allergiesPacked.ContainsKey(pawnId);
            List<string> allergies = GetAllergyNames(pawnId);
            bool legacyOrInvalid = false;
            for (int i = allergies.Count - 1; i >= 0; i--)
            {
                if (!AllergyCatalog.IsValidId(allergies[i]) || allergies[i] == AllergyCatalog.NoneId)
                {
                    allergies.RemoveAt(i);
                    legacyOrInvalid = true;
                }
            }

            // Missing key, or legacy ThingDef allergy strings → rare weighted roll (often None).
            if (!hasKey || legacyOrInvalid)
            {
                allergies = AllergyCatalog.RollAllergyIds();
            }

            SetAllergyNames(pawnId, allergies);
        }

        public void RerollTastes(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int id = pawn.thingIDNumber;
            favoriteListsPacked.Remove(id);
            allergiesPacked.Remove(id);
            discoveredAllergiesPacked.Remove(id);
            EnsureTastes(pawn);
        }

        private static void FillUniqueFavorites(List<string> list, int targetCount, List<string> allergyIds)
        {
            list.RemoveAll(n =>
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(n);
                return def == null
                    || !FavoriteFoodUtility.IsValidFavoriteCandidate(def)
                    || AllergyCatalog.FoodMatchesAny(allergyIds, def);
            });

            int guard = 0;
            while (list.Count < targetCount && guard++ < 200)
            {
                ThingDef pick = FavoriteFoodUtility.PickRandomFavorite(list, allergyIds);
                if (pick == null)
                {
                    break;
                }

                list.Add(pick.defName);
            }
        }

        private static List<ThingDef> ResolveDefs(List<string> names)
        {
            var result = new List<ThingDef>();
            if (names == null)
            {
                return result;
            }

            for (int i = 0; i < names.Count; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(names[i]);
                if (def != null)
                {
                    result.Add(def);
                }
            }

            return result;
        }
    }

    public static class FavoriteFoodUtility
    {
        private static readonly string[] PriorityFavoriteDefNames =
        {
            "Homesteader_Sausage",
            "Homesteader_CannedStew",
            "Homesteader_CannedJam",
            "Homesteader_AppleJuice",
            "Homesteader_Flapjacks",
            "Homesteader_ToastAndJam",
            "Homesteader_PloughmansLunch",
            "Homesteader_HoneyPorridge",
            "Homesteader_PumpkinPie",
            "Homesteader_ButtermilkBiscuits",
            "Homesteader_HeartyStew",
            "Homesteader_TrailStew",
            "Homesteader_Bread",
            "Homesteader_Porridge",
            "Homesteader_Jam",
            "Homesteader_Cheese",
            "Homesteader_SmokedCheese",
            "Homesteader_WaxedCheese",
            "Homesteader_Mead",
            "Homesteader_Cider",
            "Homesteader_MapleSyrup",
            "Homesteader_Honey",
            "MealSimple",
            "MealFine",
            "MealLavish",
            "MealSurvivalPack",
            "Pemmican",
            "Chocolate",
            "Beer",
            "Wine",
            "Milk",
            "RawBerries",
            "InsectJelly",
            "Ambrosia",
            "MealNutrientPaste",
            "Wellspring_BoiledWater",
        };

        private static List<ThingDef> cachedPool;

        public static GameComponent_HomesteaderFavorites Comp =>
            Current.Game?.GetComponent<GameComponent_HomesteaderFavorites>();

        public static List<ThingDef> GetFavoritePool()
        {
            if (cachedPool != null)
            {
                return cachedPool;
            }

            var set = new HashSet<ThingDef>();

            foreach (string defName in PriorityFavoriteDefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (IsValidFavoriteCandidate(def))
                {
                    set.Add(def);
                }
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!IsValidFavoriteCandidate(def))
                {
                    continue;
                }

                FoodPreferability pref = def.ingestible.preferability;
                if (pref == FoodPreferability.MealAwful
                    || pref == FoodPreferability.MealSimple
                    || pref == FoodPreferability.MealFine
                    || pref == FoodPreferability.MealLavish)
                {
                    set.Add(def);
                }
            }

            cachedPool = set.ToList();
            return cachedPool;
        }

        public static bool IsValidFavoriteCandidate(ThingDef def)
        {
            if (def?.ingestible == null || def.IsCorpse)
            {
                return false;
            }

            if (def.ingestible.sourceDef?.race?.Humanlike == true)
            {
                return false;
            }

            if (def.label.NullOrEmpty() || def.defName.EndsWith("Base"))
            {
                return false;
            }

            return true;
        }

        public static ThingDef PickRandomFavorite(List<string> already = null, List<string> allergyIds = null)
        {
            List<ThingDef> pool = GetFavoritePool();
            List<ThingDef> filtered = pool
                .Where(d => already == null || !already.Contains(d.defName))
                .Where(d => !AllergyCatalog.FoodMatchesAny(allergyIds, d))
                .ToList();
            return filtered.Count == 0 ? null : filtered.RandomElement();
        }

        public static List<ThingDef> GetFavorites(Pawn pawn) => Comp?.GetFavorites(pawn) ?? new List<ThingDef>();

        public static bool IsFavorite(Pawn pawn, ThingDef foodDef) => Comp != null && Comp.IsFavorite(pawn, foodDef);

        public static bool IsAllergy(Pawn pawn, ThingDef foodDef) => Comp != null && Comp.IsFoodAllergy(pawn, foodDef);

        public static bool IsDiscoveredAllergy(Pawn pawn, ThingDef foodDef) =>
            Comp != null && Comp.IsDiscoveredFoodAllergy(pawn, foodDef);

        public static void ApplyAllergicReaction(Pawn pawn, string allergyId)
        {
            if (pawn?.health == null || allergyId.NullOrEmpty() || Comp == null)
            {
                return;
            }

            if (!Comp.HasAllergyId(pawn, allergyId))
            {
                return;
            }

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail("Homesteader_AllergicReaction");
            if (hediff != null && pawn.health.hediffSet.HasHediff(hediff))
            {
                // Already flaring — still discover, but don't stack mood spam every env tick.
                if (!Comp.IsDiscoveredAllergyId(pawn, allergyId))
                {
                    Comp.DiscoverAllergy(pawn, allergyId);
                    Messages.Message(
                        "Homesteader_AllergyDiscovered".Translate(pawn.LabelShort, AllergyCatalog.LabelFor(allergyId)),
                        pawn,
                        MessageTypeDefOf.NegativeEvent);
                }

                return;
            }

            bool firstDiscovery = !Comp.IsDiscoveredAllergyId(pawn, allergyId);
            Comp.DiscoverAllergy(pawn, allergyId);

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_AteAllergen");
            if (thought != null && pawn.needs?.mood != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }

            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }

            if (firstDiscovery)
            {
                Messages.Message(
                    "Homesteader_AllergyDiscovered".Translate(pawn.LabelShort, AllergyCatalog.LabelFor(allergyId)),
                    pawn,
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public class ITab_Pawn_HomesteaderTastes : ITab
    {
        private Vector2 scrollPos;

        public ITab_Pawn_HomesteaderTastes()
        {
            size = new Vector2(420f, 380f);
            labelKey = "Homesteader_TabTastes";
        }

        private Pawn SelPawnForTastes => SelPawn ?? (SelThing as Corpse)?.InnerPawn;

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = SelPawnForTastes;
                return pawn?.RaceProps?.Humanlike == true && pawn.needs?.mood != null;
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawnForTastes;
            if (pawn == null)
            {
                return;
            }

            FavoriteFoodUtility.Comp?.EnsureTastes(pawn);
            Rect outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            List<ThingDef> favorites = FavoriteFoodUtility.GetFavorites(pawn);
            List<string> allergies = FavoriteFoodUtility.Comp?.GetAllergyIds(pawn) ?? new List<string>();
            List<string> discovered = FavoriteFoodUtility.Comp?.GetDiscoveredAllergyIds(pawn) ?? new List<string>();

            float viewHeight = 80f + favorites.Count * 28f + 40f + Math.Max(allergies.Count, 1) * 28f + 50f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            listing.Label("Homesteader_TastesFavoritesHeader".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();
            if (favorites.Count == 0)
            {
                listing.Label("Homesteader_TastesNone".Translate());
            }
            else
            {
                for (int i = 0; i < favorites.Count; i++)
                {
                    listing.Label("  • " + favorites[i].LabelCap);
                }
            }

            listing.Gap(12f);
            Text.Font = GameFont.Medium;
            listing.Label("Homesteader_TastesAllergiesHeader".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();
            bool revealHidden = HomesteaderMod.Settings != null && HomesteaderMod.Settings.revealAllergies;
            if (allergies.Count == 0)
            {
                // "None" stays hidden too — otherwise empty list spoils that they have no allergy.
                listing.Label(revealHidden
                    ? "Homesteader_TastesNone".Translate()
                    : "  • " + "Homesteader_TastesAllergyHidden".Translate());
            }
            else
            {
                for (int i = 0; i < allergies.Count; i++)
                {
                    string allergyId = allergies[i];
                    bool known = discovered.Contains(allergyId) || revealHidden;
                    string line = known
                        ? "  • " + AllergyCatalog.LabelFor(allergyId)
                        : "  • " + "Homesteader_TastesAllergyHidden".Translate();
                    listing.Label(line);
                }
            }

            // Reroll is a cheat — only when God mode is on (DevMode alone is not enough).
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                listing.Gap(12f);
                if (listing.ButtonText("Homesteader_TastesRerollDev".Translate()))
                {
                    FavoriteFoodUtility.Comp?.RerollTastes(pawn);
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }

    [StaticConstructorOnStartup]
    public static class HomesteaderTastesTabInjector
    {
        static HomesteaderTastesTabInjector()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race?.Humanlike != true)
                {
                    continue;
                }

                def.inspectorTabs ??= new List<Type>();
                if (!def.inspectorTabs.Contains(typeof(ITab_Pawn_HomesteaderTastes)))
                {
                    def.inspectorTabs.Add(typeof(ITab_Pawn_HomesteaderTastes));
                }

                def.inspectorTabsResolved ??= new List<InspectTabBase>();
                bool already = false;
                for (int i = 0; i < def.inspectorTabsResolved.Count; i++)
                {
                    if (def.inspectorTabsResolved[i] is ITab_Pawn_HomesteaderTastes)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                {
                    def.inspectorTabsResolved.Add(
                        InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_HomesteaderTastes)));
                }
            }
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting))]
    public static class Patch_FavoriteFoodThoughts
    {
        public static void Postfix(Pawn ingester, Thing foodSource, ThingDef foodDef, List<FoodUtility.ThoughtFromIngesting> __result)
        {
            if (ingester?.needs?.mood == null || __result == null || foodDef == null)
            {
                return;
            }

            string allergyId = AllergyCatalog.MatchingFoodAllergyId(
                FavoriteFoodUtility.Comp?.GetAllergyIds(ingester),
                foodDef);
            if (allergyId != null)
            {
                FavoriteFoodUtility.ApplyAllergicReaction(ingester, allergyId);
            }

            if (!FavoriteFoodUtility.IsFavorite(ingester, foodDef))
            {
                return;
            }

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_AteFavoriteFood");
            if (thought == null)
            {
                return;
            }

            for (int i = 0; i < __result.Count; i++)
            {
                if (__result[i].thought == thought)
                {
                    return;
                }
            }

            __result.Add(new FoodUtility.ThoughtFromIngesting
            {
                thought = thought,
                fromPrecept = null
            });
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.FoodOptimality))]
    public static class Patch_FavoriteFoodOptimality
    {
        public static void Postfix(Pawn eater, Thing foodSource, ThingDef foodDef, ref float __result)
        {
            if (eater == null || foodDef == null)
            {
                return;
            }

            if (FavoriteFoodUtility.IsFavorite(eater, foodDef))
            {
                __result += 40f;
            }

            // Only avoid after the allergy has been discovered (hidden until first reaction).
            if (FavoriteFoodUtility.IsDiscoveredAllergy(eater, foodDef))
            {
                __result -= 80f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_FavoriteFoodOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            FavoriteFoodUtility.Comp?.EnsureTastes(__instance);
        }
    }
}
