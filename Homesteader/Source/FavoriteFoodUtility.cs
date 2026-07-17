using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Homesteader
{
    public class GameComponent_HomesteaderFavorites : GameComponent
    {
        private Dictionary<int, string> favoritesByPawnId = new Dictionary<int, string>();

        public GameComponent_HomesteaderFavorites(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref favoritesByPawnId, "favoritesByPawnId", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && favoritesByPawnId == null)
            {
                favoritesByPawnId = new Dictionary<int, string>();
            }
        }

        public ThingDef GetFavorite(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            EnsureFavorite(pawn);
            if (!favoritesByPawnId.TryGetValue(pawn.thingIDNumber, out string defName))
            {
                return null;
            }

            return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        }

        public void EnsureFavorite(Pawn pawn)
        {
            // Any humanlike with a mood can have a favorite — colonists, guests, raiders, etc.
            if (pawn?.RaceProps?.Humanlike != true || pawn.needs?.mood == null)
            {
                return;
            }

            if (favoritesByPawnId.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            ThingDef pick = FavoriteFoodUtility.PickRandomFavorite();
            if (pick == null)
            {
                return;
            }

            favoritesByPawnId[pawn.thingIDNumber] = pick.defName;
        }

        public void RerollFavorite(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            favoritesByPawnId.Remove(pawn.thingIDNumber);
            EnsureFavorite(pawn);
        }
    }

    public static class FavoriteFoodUtility
    {
        // Always offered when present (Homesteader pantry + common vanilla favorites).
        private static readonly string[] PriorityFavoriteDefNames =
        {
            // Homesteader
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
            // Vanilla staples / treats
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

            // All meal-preferability foods (vanilla + mods), excluding corpses / paste weirdness filters.
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

        private static bool IsValidFavoriteCandidate(ThingDef def)
        {
            if (def?.ingestible == null)
            {
                return false;
            }

            // Skip corpses and humanlike meat.
            if (def.IsCorpse)
            {
                return false;
            }

            if (def.ingestible.sourceDef?.race?.Humanlike == true)
            {
                return false;
            }

            // Skip non-concrete defs (no label / *Base abstracts).
            if (def.label.NullOrEmpty() || def.defName.EndsWith("Base"))
            {
                return false;
            }

            return true;
        }

        public static ThingDef PickRandomFavorite()
        {
            List<ThingDef> pool = GetFavoritePool();
            if (pool.Count == 0)
            {
                return null;
            }

            return pool.RandomElement();
        }

        public static ThingDef GetFavorite(Pawn pawn) => Comp?.GetFavorite(pawn);

        public static bool IsFavorite(Pawn pawn, ThingDef foodDef)
        {
            if (pawn == null || foodDef == null)
            {
                return false;
            }

            ThingDef fav = GetFavorite(pawn);
            return fav != null && fav == foodDef;
        }
    }
}
