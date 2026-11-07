#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Homesteader
{
    public enum AllergyKindType
    {
        Food,
        Environmental,
    }

    public sealed class AllergyKind
    {
        public string Id = "";
        public AllergyKindType Type;
        public string LabelKey = "";
        /// <summary>Relative weight when rolling a real allergy (None is separate).</summary>
        public float Weight = 1f;
        public string[]? DefNames;
        public string[]? DefNameContains;
        public string[]? CategoryDefNames;

        public string LabelCap => LabelKey.Translate().CapitalizeFirst();
    }

    /// <summary>
    /// Big-9 style food allergies + common environmental ones. "None" is rolled separately with a heavy weight.
    /// </summary>
    public static class AllergyCatalog
    {
        public const string NoneId = "None";

        // ~88% none with 13 allergens at weight 1 each: chance none = NoneWeight / (NoneWeight + sum).
        public const float NoneWeight = 100f;
        public const float SecondAllergyChance = 0.12f;

        public static readonly AllergyKind[] All =
        {
            new AllergyKind
            {
                Id = "Milk",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Milk",
                DefNames = new[]
                {
                    "Milk",
                    "Homesteader_Cream",
                    "Homesteader_Butter",
                    "Homesteader_Buttermilk",
                    "Homesteader_Cheese",
                    "Homesteader_SmokedCheese",
                    "Homesteader_WaxedCheese",
                    "Homesteader_ButtermilkBiscuits",
                    "Homesteader_PumpkinPie",
                    "Homesteader_PloughmansLunch",
                    "Homesteader_Bread",
                    "Homesteader_Flapjacks",
                    "Homesteader_ToastAndJam",
                },
                DefNameContains = AllergyTokens.Milk,
            },
            new AllergyKind
            {
                Id = "Eggs",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Eggs",
                CategoryDefNames = new[] { "EggsUnfertilized", "EggsFertilized" },
                DefNameContains = AllergyTokens.Eggs,
            },
            new AllergyKind
            {
                Id = "Peanuts",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Peanuts",
                DefNameContains = AllergyTokens.Peanuts,
            },
            new AllergyKind
            {
                Id = "TreeNuts",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_TreeNuts",
                DefNames = new[] { "RawNuts" },
                // Avoid bare "Nut" — it false-matches MealNutrientPaste.
                DefNameContains = AllergyTokens.TreeNuts,
            },
            new AllergyKind
            {
                Id = "Wheat",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Wheat",
                DefNames = new[]
                {
                    "RawWheat",
                    "Flour",
                    "Homesteader_Bread",
                    "Homesteader_Porridge",
                    "Homesteader_Flapjacks",
                    "Homesteader_ToastAndJam",
                    "Homesteader_ButtermilkBiscuits",
                    "Homesteader_HoneyPorridge",
                    "Homesteader_PumpkinPie",
                    "Homesteader_PloughmansLunch",
                },
                DefNameContains = AllergyTokens.Wheat,
            },
            new AllergyKind
            {
                Id = "Soy",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Soy",
                DefNameContains = AllergyTokens.Soy,
            },
            new AllergyKind
            {
                Id = "Fish",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Fish",
                DefNames = new[] { "Homesteader_SmokedFish", "Homesteader_SaltedFish" },
                DefNameContains = AllergyTokens.Fish,
            },
            new AllergyKind
            {
                Id = "Shellfish",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Shellfish",
                DefNameContains = AllergyTokens.Shellfish,
            },
            new AllergyKind
            {
                Id = "Sesame",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Sesame",
                DefNameContains = AllergyTokens.Sesame,
            },
            new AllergyKind
            {
                Id = "Pollen",
                Type = AllergyKindType.Environmental,
                LabelKey = "Homesteader_Allergy_Pollen",
            },
            new AllergyKind
            {
                Id = "DustMites",
                Type = AllergyKindType.Environmental,
                LabelKey = "Homesteader_Allergy_DustMites",
            },
            new AllergyKind
            {
                Id = "PetDander",
                Type = AllergyKindType.Environmental,
                LabelKey = "Homesteader_Allergy_PetDander",
            },
            new AllergyKind
            {
                Id = "Mold",
                Type = AllergyKindType.Environmental,
                LabelKey = "Homesteader_Allergy_Mold",
            },
        };

        private static readonly Dictionary<string, AllergyKind> byId = BuildIndex();

        public static AllergyKind? Get(string? id)
        {
            if (id == null || id.Length == 0 || id == NoneId)
            {
                return null;
            }

            return byId.TryGetValue(id, out AllergyKind? kind) ? kind : null;
        }

        public static bool IsValidId(string? id) => id == NoneId || Get(id) != null;

        public static string LabelFor(string? id)
        {
            if (id == NoneId || id.NullOrEmpty())
            {
                return "Homesteader_TastesNone".Translate();
            }

            AllergyKind? kind = Get(id);
            return kind != null ? kind.LabelCap : id!;
        }

        /// <summary>Weighted roll: usually None; otherwise 1 allergy (rarely 2).</summary>
        public static List<string> RollAllergyIds(HashSet<string>? excludedIds = null)
        {
            float total = NoneWeight;
            var choices = new List<AllergyKind>();
            for (int i = 0; i < All.Length; i++)
            {
                AllergyKind kind = All[i];
                if (excludedIds != null && excludedIds.Contains(kind.Id))
                {
                    continue;
                }

                choices.Add(kind);
                total += kind.Weight;
            }

            if (choices.Count == 0 || Rand.Range(0f, total) < NoneWeight)
            {
                return new List<string>();
            }

            AllergyKind first = choices.RandomElementByWeight(k => k.Weight);
            var result = new List<string> { first.Id };

            if (Rand.Chance(SecondAllergyChance))
            {
                List<AllergyKind> rest = choices.Where(k => k.Id != first.Id).ToList();
                if (rest.Count > 0)
                {
                    result.Add(rest.RandomElementByWeight(k => k.Weight).Id);
                }
            }

            return result;
        }

        public static bool FoodMatches(AllergyKind? kind, ThingDef? foodDef)
        {
            if (kind == null || kind.Type != AllergyKindType.Food || foodDef == null)
            {
                return false;
            }

            if (kind.DefNames != null)
            {
                for (int i = 0; i < kind.DefNames.Length; i++)
                {
                    if (foodDef.defName == kind.DefNames[i])
                    {
                        return true;
                    }
                }
            }

            if (kind.CategoryDefNames != null && foodDef.thingCategories != null)
            {
                for (int i = 0; i < kind.CategoryDefNames.Length; i++)
                {
                    ThingCategoryDef cat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(kind.CategoryDefNames[i]);
                    if (cat != null && foodDef.IsWithinCategory(cat))
                    {
                        return true;
                    }
                }
            }

            return kind.DefNameContains != null
                && HomesteaderPure.NameMatchesAllergy(kind.Id, foodDef.defName, kind.DefNameContains);
        }

        public static bool FoodMatchesAny(IEnumerable<string>? allergyIds, ThingDef? foodDef)
        {
            return MatchingFoodAllergyId(allergyIds, foodDef) != null;
        }

        public static string? MatchingFoodAllergyId(IEnumerable<string>? allergyIds, ThingDef? foodDef)
        {
            if (allergyIds == null || foodDef == null)
            {
                return null;
            }

            int defMask = FoodMask(foodDef);
            return defMask == 0 ? null : FirstIdInMask(allergyIds, defMask);
        }

        public static string? MatchingFoodAllergyId(IEnumerable<string>? allergyIds, Thing? food)
        {
            if (allergyIds == null || food == null)
            {
                return null;
            }

            int foodMask = FoodMask(food.def);
            CompIngredients? ingredients = food.TryGetComp<CompIngredients>();
            if (ingredients?.ingredients != null)
            {
                for (int i = 0; i < ingredients.ingredients.Count; i++)
                {
                    foodMask |= FoodMask(ingredients.ingredients[i]);
                }
            }

            if (foodMask == 0)
            {
                return null;
            }

            // The food itself wins over its ingredients, as before (AZR-561).
            return FirstIdInMask(allergyIds, FoodMask(food.def)) ?? FirstIdInMask(allergyIds, foodMask);
        }

        // Per-ThingDef food-allergen bits by def.index, computed on first use
        // (AZR-561). Bit i is All[i]; ComputedBit marks a filled slot.
        private const int ComputedBit = 1 << 15;
        private static ushort[] defMasks = new ushort[0];

        internal static int FoodMask(ThingDef? def)
        {
            if (def == null)
            {
                return 0;
            }

            int index = def.index;
            if (index >= defMasks.Length)
            {
                ushort[] grown = new ushort[Math.Max(index + 1, DefDatabase<ThingDef>.DefCount)];
                Array.Copy(defMasks, grown, defMasks.Length);
                defMasks = grown;
            }

            int cached = defMasks[index];
            if ((cached & ComputedBit) != 0)
            {
                return cached & ~ComputedBit;
            }

            int mask = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (FoodMatches(All[i], def))
                {
                    mask |= 1 << i;
                }
            }

            defMasks[index] = (ushort)(mask | ComputedBit);
            return mask;
        }

        private static string? FirstIdInMask(IEnumerable<string> allergyIds, int mask)
        {
            if (mask == 0)
            {
                return null;
            }

            foreach (string id in allergyIds)
            {
                if (bitById.TryGetValue(id, out int bit) && (mask & (1 << bit)) != 0)
                {
                    return id;
                }
            }

            return null;
        }

        private static readonly Dictionary<string, int> bitById = BuildBits();

        private static Dictionary<string, int> BuildBits()
        {
            var bits = new Dictionary<string, int>(All.Length);
            for (int i = 0; i < All.Length; i++)
            {
                bits[All[i].Id] = i;
            }

            return bits;
        }

        public static bool IsEnvironmentallyExposed(Pawn? pawn, AllergyKind? kind)
        {
            if (pawn?.Map == null || kind == null || kind.Type != AllergyKindType.Environmental)
            {
                return false;
            }

            Map map = pawn.Map;
            IntVec3 pos = pawn.Position;
            switch (kind.Id)
            {
                case "Pollen":
                    return !pos.Roofed(map)
                        && GenLocalDate.Season(map) != Season.Winter
                        && map.weatherManager.RainRate < 0.1f;

                case "DustMites":
                {
                    if (!pos.Roofed(map))
                    {
                        return false;
                    }

                    Room room = pos.GetRoom(map);
                    return room != null && !room.PsychologicallyOutdoors && room.GetStat(RoomStatDefOf.Cleanliness) < -1f;
                }

                case "PetDander":
                {
                    IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        Pawn other = pawns[i];
                        if (other == pawn || other.Dead || other.RaceProps?.Animal != true)
                        {
                            continue;
                        }

                        if (other.Position.InHorDistOf(pos, 6f))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                case "Mold":
                {
                    if (!pos.Roofed(map))
                    {
                        WeatherDef? fog = DefDatabase<WeatherDef>.GetNamedSilentFail("Fog");
                        bool foggy = fog != null && map.weatherManager.CurWeatherPerceived == fog;
                        return map.weatherManager.RainRate > 0.4f || foggy;
                    }

                    Room room = pos.GetRoom(map);
                    if (room != null && !room.PsychologicallyOutdoors && room.GetStat(RoomStatDefOf.Cleanliness) < -2.5f)
                    {
                        return true;
                    }

                    int radial = GenRadial.NumCellsInRadius(5f);
                    for (int r = 0; r < radial; r++)
                    {
                        IntVec3 cell = pos + GenRadial.RadialPattern[r];
                        if (!cell.InBounds(map))
                        {
                            continue;
                        }

                        List<Thing> things = cell.GetThingList(map);
                        for (int i = 0; i < things.Count; i++)
                        {
                            Thing t = things[i];
                            CompRottable? rot = t.TryGetComp<CompRottable>();
                            if (rot != null && rot.Stage != RotStage.Fresh)
                            {
                                return true;
                            }

                            if (t is Corpse corpse && corpse.GetRotStage() != RotStage.Fresh)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                default:
                    return false;
            }
        }

        private static Dictionary<string, AllergyKind> BuildIndex()
        {
            var index = new Dictionary<string, AllergyKind>(All.Length);
            for (int i = 0; i < All.Length; i++)
            {
                index[All[i].Id] = All[i];
            }

            return index;
        }
    }
}
