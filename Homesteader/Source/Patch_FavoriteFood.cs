using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
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

            sb.Append("Favorite food: ").Append(fav.label);
            __result = sb.ToString().TrimEnd();
        }
    }

    // Assign favorites when any humanlike pawn spawns (colonists, guests, raiders, etc.).
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_FavoriteFoodOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            FavoriteFoodUtility.Comp?.EnsureFavorite(__instance);
        }
    }
}
