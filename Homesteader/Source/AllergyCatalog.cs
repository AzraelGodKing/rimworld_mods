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
        public string Id;
        public AllergyKindType Type;
        public string LabelKey;
        /// <summary>Relative weight when rolling a real allergy (None is separate).</summary>
        public float Weight = 1f;
        public string[] DefNames;
        public string[] DefNameContains;
        public string[] CategoryDefNames;

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
                DefNameContains = new[] { "Milk", "Cheese", "Butter", "Cream", "Yogurt" },
            },
            new AllergyKind
            {
                Id = "Eggs",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Eggs",
                CategoryDefNames = new[] { "EggsUnfertilized", "EggsFertilized" },
                DefNameContains = new[] { "Egg" },
            },
            new AllergyKind
            {
                Id = "Peanuts",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Peanuts",
                DefNameContains = new[] { "Peanut" },
            },
            new AllergyKind
            {
                Id = "TreeNuts",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_TreeNuts",
                DefNames = new[] { "RawNuts" },
                // Avoid bare "Nut" — it false-matches MealNutrientPaste.
                DefNameContains = new[] { "Nuts", "Walnut", "Almond", "Cashew", "Pecan", "Hazelnut" },
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
                DefNameContains = new[] { "Wheat", "Flour", "Bread", "Biscuit", "Toast", "Pasta", "Hardtack" },
            },
            new AllergyKind
            {
                Id = "Soy",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Soy",
                DefNameContains = new[] { "Soy", "Tofu", "Edamame" },
            },
            new AllergyKind
            {
                Id = "Fish",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Fish",
                DefNames = new[] { "Homesteader_SmokedFish", "Homesteader_SaltedFish" },
                DefNameContains = new[] { "Fish" },
            },
            new AllergyKind
            {
                Id = "Shellfish",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Shellfish",
                DefNameContains = new[] { "Shellfish", "Shrimp", "Crab", "Lobster", "Oyster", "Clam", "Mussel" },
            },
            new AllergyKind
            {
                Id = "Sesame",
                Type = AllergyKindType.Food,
                LabelKey = "Homesteader_Allergy_Sesame",
                DefNameContains = new[] { "Sesame", "Tahini" },
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

        private static Dictionary<string, AllergyKind> byId;

        public static AllergyKind Get(string id)
        {
            EnsureIndex();
            if (id.NullOrEmpty() || id == NoneId)
            {
                return null;
            }

            return byId.TryGetValue(id, out AllergyKind kind) ? kind : null;
        }

        public static bool IsValidId(string id) => id == NoneId || Get(id) != null;

        public static string LabelFor(string id)
        {
            if (id == NoneId || id.NullOrEmpty())
            {
                return "Homesteader_TastesNone".Translate();
            }

            AllergyKind kind = Get(id);
            return kind != null ? kind.LabelCap : id;
        }

        /// <summary>Weighted roll: usually None; otherwise 1 allergy (rarely 2).</summary>
        public static List<string> RollAllergyIds(HashSet<string> excludedIds = null)
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

        public static bool FoodMatches(AllergyKind kind, ThingDef foodDef)
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

            // Peanut is a nut — don't let TreeNuts swallow Peanut* defs if Peanuts kind exists separately.
            if (kind.DefNameContains != null)
            {
                string name = foodDef.defName;
                for (int i = 0; i < kind.DefNameContains.Length; i++)
                {
                    string token = kind.DefNameContains[i];
                    if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (kind.Id == "TreeNuts" && name.IndexOf("Peanut", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    // "Egg" in "Eggplant" etc. — require Egg at start or after underscore for eggs.
                    if (kind.Id == "Eggs")
                    {
                        if (name.StartsWith("Egg", StringComparison.OrdinalIgnoreCase)
                            || name.IndexOf("Egg", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (name.IndexOf("Eggplant", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                continue;
                            }

                            return true;
                        }

                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool FoodMatchesAny(IEnumerable<string> allergyIds, ThingDef foodDef)
        {
            if (allergyIds == null || foodDef == null)
            {
                return false;
            }

            foreach (string id in allergyIds)
            {
                AllergyKind kind = Get(id);
                if (FoodMatches(kind, foodDef))
                {
                    return true;
                }
            }

            return false;
        }

        public static string MatchingFoodAllergyId(IEnumerable<string> allergyIds, ThingDef foodDef)
        {
            if (allergyIds == null || foodDef == null)
            {
                return null;
            }

            foreach (string id in allergyIds)
            {
                AllergyKind kind = Get(id);
                if (FoodMatches(kind, foodDef))
                {
                    return id;
                }
            }

            return null;
        }

        public static bool IsEnvironmentallyExposed(Pawn pawn, AllergyKind kind)
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
                        WeatherDef fog = DefDatabase<WeatherDef>.GetNamedSilentFail("Fog");
                        bool foggy = fog != null && map.weatherManager.CurWeatherPerceived == fog;
                        return map.weatherManager.RainRate > 0.4f || foggy;
                    }

                    Room room = pos.GetRoom(map);
                    if (room != null && !room.PsychologicallyOutdoors && room.GetStat(RoomStatDefOf.Cleanliness) < -2.5f)
                    {
                        return true;
                    }

                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, 5f, true))
                    {
                        if (!cell.InBounds(map))
                        {
                            continue;
                        }

                        List<Thing> things = cell.GetThingList(map);
                        for (int i = 0; i < things.Count; i++)
                        {
                            Thing t = things[i];
                            CompRottable rot = t.TryGetComp<CompRottable>();
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

        private static void EnsureIndex()
        {
            if (byId != null)
            {
                return;
            }

            byId = new Dictionary<string, AllergyKind>();
            for (int i = 0; i < All.Length; i++)
            {
                byId[All[i].Id] = All[i];
            }
        }
    }
}
