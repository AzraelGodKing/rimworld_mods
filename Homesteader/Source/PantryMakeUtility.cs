using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Homesteader
{
    internal enum PantryMakeStatus
    {
        CanMake,
        OneShort,
        Locked
    }

    internal sealed class PantryMakeRow
    {
        public RecipeDef recipe;
        public PantryMakeStatus status;
        public int batches;
        public ThingDef missingDef;
        public int missingCount;
        public string lockReason;
        public ThingDef stationDef;
    }

    internal sealed class PantryMakeReport
    {
        public readonly List<PantryMakeRow> canMake = new List<PantryMakeRow>();
        public readonly List<PantryMakeRow> oneShort = new List<PantryMakeRow>();
        public readonly List<PantryMakeRow> locked = new List<PantryMakeRow>();
    }

    /// <summary>
    /// AZR-147 — reverse index: given map stock, which Homesteader recipes can run.
    /// Cached with the pantry snapshot; not per frame.
    /// </summary>
    internal static class PantryMakeUtility
    {
        private static PantryMakeReport cached;
        private static int cachedTick = -99999;

        internal static PantryMakeReport Snapshot()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cached != null && now - cachedTick < 60)
            {
                return cached;
            }

            cached = Scan();
            cachedTick = now;
            return cached;
        }

        internal static void Invalidate()
        {
            cachedTick = -99999;
        }

        internal static bool TryAddBill(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            Building_WorkTable table = FindStation(recipe);
            if (table?.billStack == null)
            {
                return false;
            }

            table.billStack.AddBill(new Bill_Production(recipe));
            CameraJumper.TryJumpAndSelect(table);
            return true;
        }

        private static PantryMakeReport Scan()
        {
            PantryMakeReport report = new PantryMakeReport();
            if (Verse.Current.ProgramState != ProgramState.Playing || Find.Maps == null)
            {
                return report;
            }

            Dictionary<ThingDef, int> stock = CountMapStock();
            List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDef recipe = recipes[i];
                if (!IsHomesteaderRecipe(recipe))
                {
                    continue;
                }

                PantryMakeRow row = Evaluate(recipe, stock);
                if (row == null)
                {
                    continue;
                }

                switch (row.status)
                {
                    case PantryMakeStatus.CanMake:
                        report.canMake.Add(row);
                        break;
                    case PantryMakeStatus.OneShort:
                        report.oneShort.Add(row);
                        break;
                    default:
                        report.locked.Add(row);
                        break;
                }
            }

            report.canMake.Sort(CompareRows);
            report.oneShort.Sort(CompareRows);
            report.locked.Sort(CompareRows);
            return report;
        }

        private static int CompareRows(PantryMakeRow a, PantryMakeRow b)
        {
            return string.CompareOrdinal(a.recipe?.label, b.recipe?.label);
        }

        private static bool IsHomesteaderRecipe(RecipeDef recipe)
        {
            if (IsHomesteaderDef(recipe))
            {
                return true;
            }

            if (recipe.products != null)
            {
                for (int i = 0; i < recipe.products.Count; i++)
                {
                    if (IsHomesteaderDef(recipe.products[i]?.thingDef))
                    {
                        return true;
                    }
                }
            }

            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    IngredientCount ing = recipe.ingredients[i];
                    if (ing != null && ing.IsFixedIngredient && IsHomesteaderDef(ing.FixedIngredient))
                    {
                        return true;
                    }
                }
            }

            foreach (ThingDef user in RecipeUsers(recipe))
            {
                if (IsHomesteaderDef(user))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHomesteaderDef(Def def)
        {
            return def?.defName != null && def.defName.StartsWith("Homesteader_");
        }

        private static IEnumerable<ThingDef> RecipeUsers(RecipeDef recipe)
        {
            IEnumerable<ThingDef> users = recipe?.AllRecipeUsers;
            if (users == null)
            {
                yield break;
            }

            foreach (ThingDef user in users)
            {
                if (user != null)
                {
                    yield return user;
                }
            }
        }

        private static PantryMakeRow Evaluate(RecipeDef recipe, Dictionary<ThingDef, int> stock)
        {
            if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            {
                return null;
            }

            PantryMakeRow row = new PantryMakeRow
            {
                recipe = recipe,
                stationDef = FirstUser(recipe)
            };

            if (!ResearchDone(recipe))
            {
                row.status = PantryMakeStatus.Locked;
                row.lockReason = "Homesteader_PantryMakeNeedResearch".Translate();
                return row;
            }

            if (FindStation(recipe) == null)
            {
                row.status = PantryMakeStatus.Locked;
                row.lockReason = row.stationDef != null
                    ? "Homesteader_PantryMakeNeedStation".Translate(row.stationDef.label)
                    : "Homesteader_PantryMakeNeedStationGeneric".Translate();
                return row;
            }

            int batches = int.MaxValue;
            ThingDef shortDef = null;
            int shortNeed = 0;
            int shortKinds = 0;

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ing = recipe.ingredients[i];
                int need = CountNeeded(ing);
                if (need <= 0)
                {
                    continue;
                }

                int have = CountMatching(stock, ing.filter);
                int can = have / need;
                if (can < batches)
                {
                    batches = can;
                }

                if (have < need)
                {
                    shortKinds++;
                    shortDef = FirstAllowedDef(ing.filter, stock, have);
                    shortNeed = need - have;
                }
            }

            if (batches == int.MaxValue)
            {
                batches = 0;
            }

            if (batches > 0)
            {
                row.status = PantryMakeStatus.CanMake;
                row.batches = batches;
                return row;
            }

            if (shortKinds == 1)
            {
                row.status = PantryMakeStatus.OneShort;
                row.missingDef = shortDef;
                row.missingCount = shortNeed;
                return row;
            }

            row.status = PantryMakeStatus.Locked;
            row.lockReason = "Homesteader_PantryMakeNeedMore".Translate();
            return row;
        }

        private static int CountNeeded(IngredientCount ing)
        {
            try
            {
                return (int)System.Math.Ceiling(ing.GetBaseCount());
            }
            catch
            {
                return 0;
            }
        }

        private static int CountMatching(Dictionary<ThingDef, int> stock, ThingFilter filter)
        {
            if (filter == null)
            {
                return 0;
            }

            int n = 0;
            foreach (KeyValuePair<ThingDef, int> kv in stock)
            {
                try
                {
                    if (filter.Allows(kv.Key))
                    {
                        n += kv.Value;
                    }
                }
                catch
                {
                    // Skip filter defs that throw on Allows.
                }
            }

            return n;
        }

        private static ThingDef FirstAllowedDef(ThingFilter filter, Dictionary<ThingDef, int> stock, int have)
        {
            if (have > 0)
            {
                foreach (KeyValuePair<ThingDef, int> kv in stock)
                {
                    try
                    {
                        if (kv.Value > 0 && filter.Allows(kv.Key))
                        {
                            return kv.Key;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            foreach (ThingDef def in filter.AllowedThingDefs)
            {
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        private static bool ResearchDone(RecipeDef recipe)
        {
            if (recipe.researchPrerequisite != null && !recipe.researchPrerequisite.IsFinished)
            {
                return false;
            }

            if (recipe.researchPrerequisites == null)
            {
                return true;
            }

            for (int i = 0; i < recipe.researchPrerequisites.Count; i++)
            {
                ResearchProjectDef proj = recipe.researchPrerequisites[i];
                if (proj != null && !proj.IsFinished)
                {
                    return false;
                }
            }

            return true;
        }

        private static ThingDef FirstUser(RecipeDef recipe)
        {
            foreach (ThingDef user in RecipeUsers(recipe))
            {
                return user;
            }

            return null;
        }

        private static Building_WorkTable FindStation(RecipeDef recipe)
        {
            if (Find.Maps == null)
            {
                return null;
            }

            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map map = Find.Maps[m];
                if (map == null || !map.IsPlayerHome)
                {
                    continue;
                }

                foreach (ThingDef user in RecipeUsers(recipe))
                {
                    List<Thing> things = map.listerThings.ThingsOfDef(user);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is Building_WorkTable table && table.Spawned && table.billStack != null)
                        {
                            return table;
                        }
                    }
                }
            }

            return null;
        }

        private static Dictionary<ThingDef, int> CountMapStock()
        {
            Dictionary<ThingDef, int> stock = new Dictionary<ThingDef, int>();
            if (Find.Maps == null)
            {
                return stock;
            }

            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map map = Find.Maps[m];
                if (map == null || !map.IsPlayerHome || map.haulDestinationManager == null)
                {
                    continue;
                }

                List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
                for (int g = 0; g < groups.Count; g++)
                {
                    SlotGroup group = groups[g];
                    if (group == null)
                    {
                        continue;
                    }

                    foreach (Thing thing in group.HeldThings)
                    {
                        if (thing?.def == null || thing.stackCount <= 0)
                        {
                            continue;
                        }

                        if (!stock.TryGetValue(thing.def, out int have))
                        {
                            stock[thing.def] = thing.stackCount;
                        }
                        else
                        {
                            stock[thing.def] = have + thing.stackCount;
                        }
                    }
                }
            }

            return stock;
        }
    }
}
