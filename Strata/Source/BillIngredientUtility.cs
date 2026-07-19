using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Bills only search their own map for ingredients. Strata treats active
    // production bills like construction sites: track what the level is short
    // of, pull matching stacks through the stairs, and only relay workers once
    // the bench can actually start.
    public static class BillIngredientUtility
    {
        private static MethodInfo tryFindIngredients;

        private static List<WorkTypeDef> doBillWorkTypes;

        public static bool CanStartOnMap(Bill bill, Thing billGiver, Map map, Pawn pawn)
        {
            if (bill == null || billGiver == null || map == null || bill.suspended || !bill.ShouldDoNow())
            {
                return false;
            }
            if (!UsesStandardIngredients(bill))
            {
                return true;
            }
            if (pawn != null && pawn.Map == map && pawn.CanReserve(billGiver))
            {
                tryFindIngredients ??= AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
                if (tryFindIngredients != null)
                {
                    var chosen = new List<ThingCount>();
                    var worker = new WorkGiver_DoBill();
                    if ((bool)tryFindIngredients.Invoke(worker, new object[] { bill, pawn, billGiver, chosen }))
                    {
                        return true;
                    }
                }
            }
            return !HasShortfall(bill, map);
        }

        public static bool PawnCanDoBill(Pawn pawn, Bill bill)
        {
            if (pawn?.workSettings == null || bill?.recipe == null)
            {
                return false;
            }
            WorkTypeDef required = bill.recipe.requiredGiverWorkType;
            if (required != null)
            {
                return pawn.workSettings.WorkIsActive(required);
            }
            EnsureDoBillWorkTypes();
            for (int i = 0; i < doBillWorkTypes.Count; i++)
            {
                if (pawn.workSettings.WorkIsActive(doBillWorkTypes[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static void EnsureDoBillWorkTypes()
        {
            if (doBillWorkTypes != null)
            {
                return;
            }
            doBillWorkTypes = new List<WorkTypeDef>();
            var seen = new HashSet<WorkTypeDef>();
            foreach (WorkGiverDef wgd in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (wgd.giverClass == null || wgd.workType == null
                    || !typeof(WorkGiver_DoBill).IsAssignableFrom(wgd.giverClass))
                {
                    continue;
                }
                if (seen.Add(wgd.workType))
                {
                    doBillWorkTypes.Add(wgd.workType);
                }
            }
        }

        public static bool MapHasRunnableBill(Pawn pawn, Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }
                foreach (Bill bill in billGiver.BillStack)
                {
                    if (!bill.ShouldDoNow() || !PawnCanDoBill(pawn, bill))
                    {
                        continue;
                    }
                    if (CanStartOnMap(bill, building, map, pawn))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static void AddShortfalls(LevelDemand.Entry entry, Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }
                foreach (Bill bill in billGiver.BillStack)
                {
                    if (bill.suspended || !bill.ShouldDoNow() || !UsesStandardIngredients(bill))
                    {
                        continue;
                    }
                    AccumulateShortfall(entry, map, building, bill);
                }
            }
        }

        private static bool UsesStandardIngredients(Bill bill)
        {
            RecipeDef recipe = bill.recipe;
            return recipe?.ingredients != null && recipe.ingredients.Count > 0;
        }

        private static void AccumulateShortfall(LevelDemand.Entry entry, Map map, Thing billGiver, Bill bill)
        {
            RecipeDef recipe = bill.recipe;
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ing = recipe.ingredients[i];
                int needed = NeededCount(ing);
                if (needed <= 0)
                {
                    continue;
                }
                // Record full recipe need — LevelDemand.Build subtracts on-map
                // stock once. Subtracting here too double-counted and cancelled
                // cellar→surface pulls when the stove map already had a little food.
                foreach (ThingDef def in MatchingDefs(bill, ing))
                {
                    entry.AddShortfall(def, needed, billGiver.Position);
                    break;
                }
            }
        }

        // After a cross-level haul lands, prefer delivering to a billgiver that
        // still needs this def (tavern stove) over dumping into a stockpile.
        public static Thing FindBillGiverNeeding(Pawn pawn, ThingDef def)
        {
            if (pawn?.Map == null || def == null)
            {
                return null;
            }
            Map map = pawn.Map;
            Thing best = null;
            float bestDist = float.MaxValue;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }
                if (!BillGiverNeeds(building, billGiver, def))
                {
                    continue;
                }
                if (!pawn.CanReserveAndReach(building, PathEndMode.Touch, Danger.Deadly, 1, 1))
                {
                    continue;
                }
                float dist = pawn.Position.DistanceToSquared(building.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = building;
                }
            }
            return best;
        }

        private static bool BillGiverNeeds(Thing building, IBillGiver billGiver, ThingDef def)
        {
            foreach (Bill bill in billGiver.BillStack)
            {
                if (bill.suspended || !bill.ShouldDoNow() || !UsesStandardIngredients(bill))
                {
                    continue;
                }
                RecipeDef recipe = bill.recipe;
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    IngredientCount ing = recipe.ingredients[i];
                    foreach (ThingDef match in MatchingDefs(bill, ing))
                    {
                        if (match == def)
                        {
                            return CountOf(building.Map, def) < NeededCount(ing);
                        }
                    }
                }
            }
            return false;
        }

        private static bool HasShortfall(Bill bill, Map map)
        {
            RecipeDef recipe = bill.recipe;
            if (recipe?.ingredients == null)
            {
                return false;
            }
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ing = recipe.ingredients[i];
                int needed = NeededCount(ing);
                if (needed <= 0)
                {
                    continue;
                }
                int have = 0;
                foreach (ThingDef def in MatchingDefs(bill, ing))
                {
                    have += CountOf(map, def);
                }
                if (have < needed)
                {
                    return true;
                }
            }
            return false;
        }

        private static int NeededCount(IngredientCount ing)
        {
            return GenMath.RoundRandom(ing.GetBaseCount());
        }

        private static IEnumerable<ThingDef> MatchingDefs(Bill bill, IngredientCount ing)
        {
            if (ing.IsFixedIngredient && ing.FixedIngredient != null)
            {
                ThingDef def = ing.FixedIngredient;
                if (AllowsIngredient(bill, def, ing))
                {
                    yield return def;
                }
                yield break;
            }
            ThingFilter filter = ing.filter;
            if (filter != null)
            {
                foreach (ThingDef def in filter.AllowedThingDefs)
                {
                    if (AllowsIngredient(bill, def, ing))
                    {
                        yield return def;
                    }
                }
                yield break;
            }
            // Broad category ingredients (any meat, any leather, etc.): scan
            // defs that pass the same stacked filters vanilla DoBill uses.
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.category == ThingCategory.Item && AllowsIngredient(bill, def, ing))
                {
                    yield return def;
                }
            }
        }

        private static bool AllowsIngredient(Bill bill, ThingDef def, IngredientCount ing)
        {
            if (!bill.IsFixedOrAllowedIngredient(def))
            {
                return false;
            }
            RecipeDef recipe = bill.recipe;
            if (recipe?.fixedIngredientFilter != null && !recipe.fixedIngredientFilter.Allows(def))
            {
                return false;
            }
            if (bill.ingredientFilter != null && !bill.ingredientFilter.Allows(def))
            {
                return false;
            }
            if (ing?.filter != null && !ing.filter.Allows(def))
            {
                return false;
            }
            return true;
        }

        private static int CountOf(Map map, ThingDef def)
        {
            if (def.CountAsResource)
            {
                return StrataResources.RawGetCount(map, def);
            }
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                count += things[i].stackCount;
            }
            return count;
        }
    }
}
