using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    public enum DateActivity
    {
        Unresolved = 0,
        Hangout = 1,
        Dinner = 2,
        Picnic = 3,
        Walk = 4,
        Stargaze = 5,
        Dance = 6,
        Gift = 7,
        Recreation = 8,
    }

    /// <summary>
    /// Picks what a couple actually does on a date. Deterministic per couple per
    /// day-slot so both partners independently resolve the same activity.
    /// </summary>
    public static class DateNightActivities
    {
        /// <summary>Dev-tool override: dates resolved before the expiry tick use this
        /// activity, so both partners (who resolve at different ticks) match.</summary>
        public static DateActivity ForcedActivity = DateActivity.Unresolved;
        public static int ForcedActivityExpireTick = -1;

        public static DateActivity Resolve(Pawn pawn, Pawn partner)
        {
            if (pawn?.Map == null || partner == null)
            {
                return DateActivity.Hangout;
            }
            if (DateNightMod.Settings != null && !DateNightMod.Settings.enableDateActivities)
            {
                return DateActivity.Hangout;
            }
            if (ForcedActivity != DateActivity.Unresolved)
            {
                if (Find.TickManager.TicksGame <= ForcedActivityExpireTick)
                {
                    return ForcedActivity;
                }
                ForcedActivity = DateActivity.Unresolved;
            }

            List<DateActivity> candidates = new List<DateActivity> { DateActivity.Hangout };

            Map map = pawn.Map;
            bool mealsAvailable = FindMealFor(pawn, partner) != null;
            bool niceOutside = IsNiceOutside(map);

            if (mealsAvailable && HasColonistTable(map))
            {
                candidates.Add(DateActivity.Dinner);
            }
            if (mealsAvailable && niceOutside && !IsNight(map))
            {
                candidates.Add(DateActivity.Picnic);
            }
            if (niceOutside)
            {
                candidates.Add(DateActivity.Walk);
            }
            if (IsNight(map) && !IsRaining(map))
            {
                candidates.Add(DateActivity.Stargaze);
            }
            if (FindGatherSpotFor(pawn, partner) != null)
            {
                candidates.Add(DateActivity.Dance);
            }
            if (AllowGifts() && FindGiftFor(pawn, partner) != null)
            {
                candidates.Add(DateActivity.Gift);
            }
            if (FindJoyBuildingFor(pawn, partner) != null)
            {
                candidates.Add(DateActivity.Recreation);
            }

            // Same seed on both partners: couple key + day + schedule hour block.
            int seed = Gen.HashCombineInt(CoupleSeed(pawn, partner), GenDate.DaysPassed * 31 + GenLocalDate.HourInteger(map) / 6);
            Rand.PushState(seed);
            DateActivity picked = candidates[Rand.Range(0, candidates.Count)];
            Rand.PopState();
            return picked;
        }

        public static bool AllowGifts()
        {
            return DateNightMod.Settings == null || DateNightMod.Settings.allowGiftDates;
        }

        public static bool IsInitiator(Pawn pawn, Pawn partner)
        {
            return partner == null || pawn.thingIDNumber < partner.thingIDNumber;
        }

        public static int CoupleSeed(Pawn a, Pawn b)
        {
            int x = a.thingIDNumber;
            int y = b.thingIDNumber;
            return x < y ? Gen.HashCombineInt(x, y) : Gen.HashCombineInt(y, x);
        }

        public static bool IsNight(Map map)
        {
            int hour = GenLocalDate.HourInteger(map);
            return hour >= 20 || hour <= 4;
        }

        public static bool IsRaining(Map map)
        {
            return map.weatherManager != null
                && (map.weatherManager.RainRate > 0.1f || map.weatherManager.SnowRate > 0.1f);
        }

        public static bool IsNiceOutside(Map map)
        {
            if (IsRaining(map))
            {
                return false;
            }
            float temp = map.mapTemperature.OutdoorTemp;
            return temp > -5f && temp < 40f;
        }

        public static bool HasColonistTable(Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def.IsTable)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Best reachable, reservable prepared meal for this pawn.</summary>
        public static Thing FindMealFor(Pawn pawn, Pawn partner)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Thing best = null;
            float bestScore = float.MinValue;
            List<Thing> foods = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            for (int i = 0; i < foods.Count; i++)
            {
                Thing food = foods[i];
                if (food.def.ingestible == null
                    || food.def.ingestible.preferability < FoodPreferability.MealAwful
                    || !food.def.IsNutritionGivingIngestible
                    || !food.IngestibleNow)
                {
                    continue;
                }
                if (food.IsForbidden(pawn) || !FoodUtility.WillEat(pawn, food, null, careIfNotAcceptableForTitle: false))
                {
                    continue;
                }
                if (!pawn.CanReserveAndReach(food, PathEndMode.ClosestTouch, Danger.Some, 10, 1))
                {
                    continue;
                }

                float score = (float)food.def.ingestible.preferability * 1000f
                    - pawn.Position.DistanceToSquared(food.Position);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = food;
                }
            }
            return best;
        }

        private static readonly string[] GiftDefNames =
        {
            "Beer", "Chocolate", "Ambrosia", "PsychiteTea", "InsectJelly",
        };

        /// <summary>A small luxury the giver can fetch and hand over.</summary>
        public static Thing FindGiftFor(Pawn giver, Pawn receiver)
        {
            if (giver?.Map == null)
            {
                return null;
            }

            Thing best = null;
            float bestDist = float.MaxValue;
            for (int d = 0; d < GiftDefNames.Length; d++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(GiftDefNames[d]);
                if (def == null)
                {
                    continue;
                }
                List<Thing> things = giver.Map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (!thing.Spawned || thing.IsForbidden(giver))
                    {
                        continue;
                    }
                    if (!giver.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some, 10, 1))
                    {
                        continue;
                    }
                    float dist = giver.Position.DistanceToSquared(thing.Position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = thing;
                    }
                }
            }
            return best;
        }

        public static Thing FindGatherSpotFor(Pawn pawn, Pawn partner)
        {
            if (pawn?.Map == null)
            {
                return null;
            }
            Thing best = null;
            float bestDist = float.MaxValue;
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                CompGatherSpot gather = building.TryGetComp<CompGatherSpot>();
                if (gather == null || !gather.Active)
                {
                    continue;
                }
                if (!BothCanReach(pawn, partner, building))
                {
                    continue;
                }
                float dist = PairDistance(pawn, partner, building.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = building;
                }
            }
            return best;
        }

        /// <summary>Nearest colonist joy building (chess table, TV, horseshoes...).</summary>
        public static Building FindJoyBuildingFor(Pawn pawn, Pawn partner)
        {
            if (pawn?.Map == null)
            {
                return null;
            }
            Building best = null;
            float bestDist = float.MaxValue;
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (building.def.building?.joyKind == null)
                {
                    continue;
                }
                if (!BothCanReach(pawn, partner, building))
                {
                    continue;
                }
                float dist = PairDistance(pawn, partner, building.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = building;
                }
            }
            return best;
        }

        /// <summary>Best-of-N random unroofed cell, preferring beauty. For picnics and stargazing.</summary>
        public static IntVec3 FindOutdoorSpot(Pawn pawn, Pawn partner, bool preferBeauty)
        {
            Map map = pawn.Map;
            IntVec3 root = partner != null
                ? new IntVec3((pawn.Position.x + partner.Position.x) / 2, 0, (pawn.Position.z + partner.Position.z) / 2)
                : pawn.Position;
            if (!root.InBounds(map))
            {
                root = pawn.Position;
            }

            IntVec3 best = IntVec3.Invalid;
            float bestBeauty = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                if (!CellFinder.TryFindRandomCellNear(root, map, 20,
                        c => IsUsableOutdoorCell(c, map, pawn, partner), out IntVec3 cell))
                {
                    break;
                }
                if (!preferBeauty)
                {
                    return cell;
                }
                float beauty = BeautyUtility.AverageBeautyPerceptible(cell, map);
                if (beauty > bestBeauty)
                {
                    bestBeauty = beauty;
                    best = cell;
                }
            }
            return best;
        }

        private static bool IsUsableOutdoorCell(IntVec3 cell, Map map, Pawn pawn, Pawn partner)
        {
            if (!cell.InBounds(map) || cell.Roofed(map) || !cell.Standable(map))
            {
                return false;
            }
            if (cell.GetDangerFor(pawn, map) != Danger.None)
            {
                return false;
            }
            if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }
            if (partner != null && partner.Map == map && !partner.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }
            return true;
        }

        private static bool BothCanReach(Pawn pawn, Pawn partner, Thing thing)
        {
            if (!pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            if (partner != null && partner.Map == pawn.Map
                && !partner.CanReach(thing, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            return true;
        }

        private static float PairDistance(Pawn pawn, Pawn partner, IntVec3 pos)
        {
            float dist = pawn.Position.DistanceToSquared(pos);
            if (partner != null && partner.Map == pawn.Map)
            {
                dist += partner.Position.DistanceToSquared(pos);
            }
            return dist;
        }

        /// <summary>Nearest colonist table cell both pawns can reach (for dinner dates).</summary>
        public static LocalTargetInfo FindDinnerSpot(Pawn pawn, Pawn partner)
        {
            Building best = null;
            float bestDist = float.MaxValue;
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (!building.def.IsTable)
                {
                    continue;
                }
                if (!BothCanReach(pawn, partner, building))
                {
                    continue;
                }
                float dist = PairDistance(pawn, partner, building.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = building;
                }
            }
            if (best == null)
            {
                return LocalTargetInfo.Invalid;
            }

            // Stand next to the table, not on it.
            foreach (IntVec3 side in GenAdj.CellsAdjacentCardinal(best))
            {
                if (side.InBounds(pawn.Map) && side.Standable(pawn.Map)
                    && pawn.CanReach(side, PathEndMode.OnCell, Danger.Some))
                {
                    return side;
                }
            }
            return best;
        }

        /// <summary>Where this activity happens. Falls back to the generic date spot.</summary>
        public static LocalTargetInfo FindSpotFor(DateActivity activity, Pawn pawn, Pawn partner)
        {
            switch (activity)
            {
                case DateActivity.Dinner:
                {
                    LocalTargetInfo table = FindDinnerSpot(pawn, partner);
                    if (table.IsValid)
                    {
                        return table;
                    }
                    break;
                }
                case DateActivity.Picnic:
                {
                    IntVec3 cell = FindOutdoorSpot(pawn, partner, preferBeauty: true);
                    if (cell.IsValid)
                    {
                        return cell;
                    }
                    break;
                }
                case DateActivity.Stargaze:
                {
                    IntVec3 cell = FindOutdoorSpot(pawn, partner, preferBeauty: false);
                    if (cell.IsValid)
                    {
                        return cell;
                    }
                    break;
                }
                case DateActivity.Walk:
                {
                    IntVec3 cell = FindOutdoorSpot(pawn, partner, preferBeauty: true);
                    if (cell.IsValid)
                    {
                        return cell;
                    }
                    break;
                }
                case DateActivity.Dance:
                {
                    Thing gather = FindGatherSpotFor(pawn, partner);
                    if (gather != null)
                    {
                        return gather;
                    }
                    break;
                }
                case DateActivity.Recreation:
                {
                    Building joy = FindJoyBuildingFor(pawn, partner);
                    if (joy != null)
                    {
                        if (joy.def.hasInteractionCell)
                        {
                            return joy.InteractionCell;
                        }
                        return joy;
                    }
                    break;
                }
            }
            return DateNightDateUtility.FindDateSpot(pawn, partner);
        }
    }
}
