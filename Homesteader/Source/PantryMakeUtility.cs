#nullable enable
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
        public RecipeDef recipe = null!;
        public PantryMakeStatus status;
        public int batches;
        public ThingDef? missingDef;
        public int missingCount;
        public string lockReason = "";
        public ThingDef? stationDef;
        public List<Building_WorkTable> stations = new List<Building_WorkTable>();

        /// <summary>Row text, built once per scan instead of every frame (AZR-561).</summary>
        public string text = "";
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
        private static PantryMakeReport? cached;
        private static int cachedTick = -99999;

        internal static PantryMakeReport Snapshot()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cached != null && now >= cachedTick && now - cachedTick < 60)
            {
                return cached;
            }

            cached = Scan();
            cachedTick = now;
            return cached;
        }

        internal static void Invalidate()
        {
            cached = null;
            cachedTick = -99999;
        }

        internal static void AddBillAt(RecipeDef? recipe, Building_WorkTable? table)
        {
            if (recipe == null || table?.billStack == null || !table.Spawned)
            {
                return;
            }

            HomesteaderSync.AddBill(recipe, table);
            CameraJumper.TryJumpAndSelect(table);
        }

        internal static List<Building_WorkTable> FindStations(RecipeDef? recipe)
        {
            List<Building_WorkTable> found = new List<Building_WorkTable>();
            if (recipe == null || Find.Maps == null)
            {
                return found;
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
                        if (things[i] is Building_WorkTable table && table.Spawned && table.billStack != null
                            && !found.Contains(table))
                        {
                            found.Add(table);
                        }
                    }
                }
            }

            return found;
        }

        private static PantryMakeReport Scan()
        {
            PantryMakeReport report = new PantryMakeReport();
            if (Verse.Current.ProgramState != ProgramState.Playing || Find.Maps == null)
            {
                return report;
            }

            Dictionary<ThingDef, int> stock = CountMapStock();
            List<RecipeDef> recipes = HomesteaderRecipes();
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDef recipe = recipes[i];
                PantryMakeRow? row = Evaluate(recipe, stock);
                if (row == null)
                {
                    continue;
                }

                row.text = LineText(row);
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

        private static string LineText(PantryMakeRow row)
        {
            string name = row.recipe.LabelCap;
            switch (row.status)
            {
                case PantryMakeStatus.CanMake:
                    return name + "  ×" + row.batches;
                case PantryMakeStatus.OneShort:
                    string missing = row.missingDef != null
                        ? row.missingDef.LabelCap.ToString()
                        : "Homesteader_PantryMakeUnknownIng".Translate().ToString();
                    return name + "  —  " + "Homesteader_PantryMakeNeedIng".Translate(row.missingCount, missing);
                default:
                    return name + "  —  " + row.lockReason;
            }
        }

        private static int CompareRows(PantryMakeRow a, PantryMakeRow b)
        {
            return string.CompareOrdinal(a.recipe?.label, b.recipe?.label);
        }

        // Built once after defs load: which recipes the Make page lists, and each
        // recipe's work tables (AllRecipeUsers walks every ThingDef) (AZR-561).
        private static List<RecipeDef>? homesteaderRecipes;
        private static readonly Dictionary<RecipeDef, ThingDef[]> usersByRecipe = new Dictionary<RecipeDef, ThingDef[]>();

        private static List<RecipeDef> HomesteaderRecipes()
        {
            if (homesteaderRecipes != null)
            {
                return homesteaderRecipes;
            }

            var list = new List<RecipeDef>();
            List<RecipeDef> all = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsHomesteaderRecipe(all[i]))
                {
                    list.Add(all[i]);
                }
            }

            homesteaderRecipes = list;
            return list;
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
                    IngredientCount? ing = recipe.ingredients[i];
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

        private static bool IsHomesteaderDef(Def? def)
        {
            return def?.defName != null && def.defName.StartsWith("Homesteader_");
        }

        private static ThingDef[] RecipeUsers(RecipeDef? recipe)
        {
            if (recipe == null)
            {
                return new ThingDef[0];
            }

            if (!usersByRecipe.TryGetValue(recipe, out ThingDef[]? users))
            {
                var found = new List<ThingDef>();
                IEnumerable<ThingDef>? all = recipe.AllRecipeUsers;
                if (all != null)
                {
                    foreach (ThingDef user in all)
                    {
                        if (user != null)
                        {
                            found.Add(user);
                        }
                    }
                }

                users = found.ToArray();
                usersByRecipe[recipe] = users;
            }

            return users;
        }

        private static PantryMakeRow? Evaluate(RecipeDef recipe, Dictionary<ThingDef, int> stock)
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

            row.stations = FindStations(recipe);
            if (row.stations.Count == 0)
            {
                row.status = PantryMakeStatus.Locked;
                row.lockReason = row.stationDef != null
                    ? "Homesteader_PantryMakeNeedStation".Translate(row.stationDef.label)
                    : "Homesteader_PantryMakeNeedStationGeneric".Translate();
                return row;
            }

            int batches = int.MaxValue;
            ThingDef? shortDef = null;
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

        private static int CountMatching(Dictionary<ThingDef, int> stock, ThingFilter? filter)
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

        private static ThingDef? FirstAllowedDef(ThingFilter filter, Dictionary<ThingDef, int> stock, int have)
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

        private static ThingDef? FirstUser(RecipeDef recipe)
        {
            ThingDef[] users = RecipeUsers(recipe);
            return users.Length > 0 ? users[0] : null;
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
