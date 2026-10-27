using System;
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
                if (tryFindIngredients != null && TryInvokeFindIngredients(bill, pawn, billGiver))
                {
                    return true;
                }
            }
            return !HasShortfall(bill, map);
        }

        // 1.6 takes a fifth missing-ingredients list. Passing the old four
        // arguments throws inside every work think once a pawn is assigned.
        private static bool TryInvokeFindIngredients(Bill bill, Pawn pawn, Thing billGiver)
        {
            var chosen = new List<ThingCount>();
            var worker = new WorkGiver_DoBill();
            int count = tryFindIngredients.GetParameters().Length;
            object[] args = count >= 5
                ? new object[] { bill, pawn, billGiver, chosen, new List<IngredientCount>() }
                : new object[] { bill, pawn, billGiver, chosen };
            try
            {
                return args.Length == count && (bool)tryFindIngredients.Invoke(worker, args);
            }
            catch (System.Exception e)
            {
                StrataLog.Verbose("[Strata] TryFindBestBillIngredients failed: " + e.GetType().Name);
                return false;
            }
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
                    entry.AddShortfall(def, needed, billGiver.Position, hardNeed: true);
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

        private struct PendingBillFetch
        {
            public int thingId;
            public int thingMapId;
            public int destMapId;
            public int giverId;
            public int count;
            public ThingDef def;
        }

        private static readonly Dictionary<int, PendingBillFetch> pending =
            new Dictionary<int, PendingBillFetch>();

        public static void ResetSession() => pending.Clear();

        public static bool HasPending(Pawn pawn)
        {
            return pawn != null && pending.ContainsKey(pawn.thingIDNumber);
        }

        public static void ClearPending(Pawn pawn)
        {
            if (pawn != null)
            {
                pending.Remove(pawn.thingIDNumber);
            }
        }

        // The bench is on this floor and the ingredient is on another. Walk
        // there, carry it back, then the normal bill job can start.
        public static Job TryFetchForLocalBills(Pawn pawn, WorkTypeDef onlyType, out WorkTypeDef workType)
        {
            workType = null;
            if (pawn?.Map == null || pawn.workSettings == null || !pawn.workSettings.EverWork)
            {
                return null;
            }

            Map home = pawn.Map;
            int scanned = 0;
            foreach (Building building in home.listerBuildings.allBuildingsColonist)
            {
                if (++scanned > 24)
                {
                    break;
                }
                if (building is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }
                foreach (Bill bill in billGiver.BillStack)
                {
                    if (bill.suspended || !bill.ShouldDoNow() || !UsesStandardIngredients(bill)
                        || !PawnCanDoBill(pawn, bill) || !HasShortfall(bill, home))
                    {
                        continue;
                    }
                    WorkTypeDef type = WorkRelaySignals.WorkTypeForBill(pawn, bill);
                    if (type == null || (onlyType != null && type != onlyType))
                    {
                        continue;
                    }
                    if (TryFindOffMapIngredient(pawn, bill, home, out Thing ingredient, out int count)
                        && TryMakeBillFetch(pawn, ingredient, building, count, out Job job))
                    {
                        workType = type;
                        return job;
                    }
                }
            }
            return null;
        }

        public static bool TryFinishFetch(Pawn pawn)
        {
            if (pawn?.Map == null || !pending.TryGetValue(pawn.thingIDNumber, out PendingBillFetch fetch))
            {
                return false;
            }
            pending.Remove(pawn.thingIDNumber);

            Map thingMap = FindMap(fetch.thingMapId);
            Map destMap = FindMap(fetch.destMapId);
            if (thingMap == null || destMap == null || pawn.Map != thingMap)
            {
                return false;
            }

            Thing ingredient = FindThing(thingMap, fetch.thingId);
            if (ingredient == null || !ingredient.Spawned || !pawn.CanReserve(ingredient)
                || !pawn.CanReach(ingredient, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }

            Thing giver = fetch.giverId > 0
                ? StrataPortalUtility.FindThingByIdAcrossMaps(fetch.giverId)
                : null;
            MapPortal portal = LevelGraph.BestFirstStep(thingMap, destMap, pawn.Position, pawn);
            if (portal == null)
            {
                return false;
            }

            // They came from the bench floor for this; no return trip.
            HaulToLevelTargets.Remember(pawn, destMap, returnMap: null, giver);
            Job haul = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, ingredient, portal);
            haul.count = Math.Min(ingredient.stackCount, Math.Max(1, fetch.count));
            return pawn.jobs.TryTakeOrderedJob(haul, JobTag.MiscWork);
        }

        private static bool TryFindOffMapIngredient(
            Pawn pawn,
            Bill bill,
            Map home,
            out Thing ingredient,
            out int count)
        {
            ingredient = null;
            count = 0;
            RecipeDef recipe = bill.recipe;
            var landings = new List<IntVec3>();
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(home))
            {
                Map map = link.map;
                if (map == null || map == home)
                {
                    continue;
                }
                landings.Clear();
                landings.Add(link.arrivalCell);
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    IngredientCount ing = recipe.ingredients[i];
                    int needed = NeededCount(ing);
                    if (needed <= 0)
                    {
                        continue;
                    }
                    int have = 0;
                    Thing stack = null;
                    int looked = 0;
                    foreach (ThingDef def in MatchingDefs(bill, ing))
                    {
                        if (++looked > 16)
                        {
                            break;
                        }
                        have += CountOf(home, def);
                        if (stack != null || CountOf(map, def) <= 0)
                        {
                            continue;
                        }
                        stack = FirstReachableStack(pawn, map, def, landings);
                    }
                    if (have >= needed || stack == null)
                    {
                        continue;
                    }
                    ingredient = stack;
                    count = Math.Min(stack.stackCount, Math.Max(1, needed - have));
                    return true;
                }
            }
            return false;
        }

        private static Thing FirstReachableStack(Pawn pawn, Map map, ThingDef def, List<IntVec3> landings)
        {
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < list.Count && i < 40; i++)
            {
                Thing thing = list[i];
                if (thing == null || !thing.Spawned || thing.IsForbidden(pawn) || !pawn.CanReserve(thing))
                {
                    continue;
                }
                for (int c = 0; c < landings.Count; c++)
                {
                    if (!landings[c].IsValid || !landings[c].InBounds(map))
                    {
                        continue;
                    }
                    if (map.reachability.CanReach(
                        landings[c], thing, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.PassDoors)))
                    {
                        return thing;
                    }
                }
            }
            return null;
        }

        private static bool TryMakeBillFetch(Pawn pawn, Thing ingredient, Thing giver, int count, out Job job)
        {
            job = null;
            Map ingredientMap = ingredient.Map;
            Map destMap = giver.Map;
            if (ingredientMap == null || destMap == null
                || LevelGraph.BestFirstStep(pawn.Map, ingredientMap, pawn.Position, pawn) == null)
            {
                return false;
            }

            pending[pawn.thingIDNumber] = new PendingBillFetch
            {
                thingId = ingredient.thingIDNumber,
                thingMapId = ingredientMap.uniqueID,
                destMapId = destMap.uniqueID,
                giverId = giver.thingIDNumber,
                count = count,
                def = ingredient.def,
            };

            job = PawnRelay.TryRelayToMap(
                pawn,
                ingredientMap,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: ingredient.Position);
            if (job == null)
            {
                pending.Remove(pawn.thingIDNumber);
                return false;
            }

            pawn.Reserve(ingredient, job, stackCount: count, errorOnFailed: false);
            return true;
        }

        private static Map FindMap(int id)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == id)
                {
                    return maps[i];
                }
            }
            return null;
        }

        private static Thing FindThing(Map map, int id)
        {
            if (id <= 0 || map == null)
            {
                return null;
            }
            Thing thing = StrataPortalUtility.FindThingByIdAcrossMaps(id);
            return thing != null && thing.MapHeld == map ? thing : null;
        }
    }
}
