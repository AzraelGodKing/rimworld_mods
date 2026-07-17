using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
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

        private static bool IsValidFavoriteCandidate(ThingDef def)
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

        public static ThingDef PickRandomFavorite()
        {
            List<ThingDef> pool = GetFavoritePool();
            return pool.Count == 0 ? null : pool.RandomElement();
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

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting))]
    public static class Patch_FavoriteFoodThoughts
    {
        public static void Postfix(Pawn ingester, Thing foodSource, ThingDef foodDef, List<ThoughtDef> __result)
        {
            if (ingester?.needs?.mood == null || __result == null || foodDef == null)
            {
                return;
            }

            if (!FavoriteFoodUtility.IsFavorite(ingester, foodDef))
            {
                return;
            }

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_AteFavoriteFood");
            if (thought != null && !__result.Contains(thought))
            {
                __result.Add(thought);
            }
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
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_FavoriteFoodInspectString
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            if (__instance?.RaceProps?.Humanlike != true || __instance.needs?.mood == null)
            {
                return;
            }

            ThingDef fav = FavoriteFoodUtility.GetFavorite(__instance);
            if (fav == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder(__result);
            if (!string.IsNullOrEmpty(__result))
            {
                sb.AppendLine();
            }

            sb.Append("Homesteader_FavoriteFoodInspect".Translate(fav.label));
            __result = sb.ToString().TrimEnd();
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_FavoriteFoodOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            FavoriteFoodUtility.Comp?.EnsureFavorite(__instance);
        }
    }
}
