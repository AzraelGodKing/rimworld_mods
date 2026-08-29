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

            if (partner.jobs?.curDriver is JobDriver_Date other
                && other.CoupleActivity != DateActivity.Unresolved)
            {
                return other.CoupleActivity;
            }

            Pawn initiator = IsInitiator(pawn, partner) ? pawn : partner;
            Pawn otherPawn = initiator == pawn ? partner : pawn;

            List<DateActivity> candidates = new List<DateActivity> { DateActivity.Hangout };

            Map map = pawn.Map;
            bool mealsAvailable = FindMealFor(pawn, partner) != null
                || FindMealFor(partner, pawn) != null;
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
            if (AllowGifts() && FindGiftFor(initiator, otherPawn) != null)
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

        public static long CoupleKey(Pawn a, Pawn b)
        {
            return CoupleKey(a.thingIDNumber, b.thingIDNumber);
        }

        public static long CoupleKey(int x, int y)
        {
            if (x > y)
            {
                int tmp = x;
                x = y;
                y = tmp;
            }
            return ((long)x << 32) | (uint)y;
        }

        public static bool BothCanReachPublic(Pawn pawn, Pawn partner, Thing thing)
        {
            return BothCanReach(pawn, partner, thing);
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
            int bestId = int.MaxValue;
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
                if (building.thingIDNumber < bestId)
                {
                    bestId = building.thingIDNumber;
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
            int bestId = int.MaxValue;
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
                if (building.thingIDNumber < bestId)
                {
                    bestId = building.thingIDNumber;
                    best = building;
                }
            }
            return best;
        }

        /// <summary>Best-of-N random unroofed cell, preferring beauty. For picnics and stargazing.</summary>
        public static IntVec3 FindOutdoorSpot(Pawn pawn, Pawn partner, bool preferBeauty)
        {
            Map map = pawn.Map;
            IntVec3 root = StableSearchRoot(pawn, partner);

            IntVec3 best = IntVec3.Invalid;
            float bestBeauty = float.MinValue;
            int seed = Gen.HashCombineInt(
                CoupleSeed(pawn, partner),
                GenDate.DaysPassed * 31 + GenLocalDate.HourInteger(map) / 6);
            seed = Gen.HashCombineInt(seed, preferBeauty ? 7 : 13);
            Rand.PushState(seed);
            try
            {
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
            }
            finally
            {
                Rand.PopState();
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

        /// <summary>Shared table; initiator and partner take adjacent seats.</summary>
        public static LocalTargetInfo FindDinnerSpot(Pawn pawn, Pawn partner)
        {
            Building best = null;
            int bestId = int.MaxValue;
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
                if (building.thingIDNumber < bestId)
                {
                    bestId = building.thingIDNumber;
                    best = building;
                }
            }
            if (best == null)
            {
                return LocalTargetInfo.Invalid;
            }

            var seats = new List<IntVec3>();
            foreach (IntVec3 side in GenAdj.CellsAdjacentCardinal(best))
            {
                if (side.InBounds(pawn.Map) && side.Standable(pawn.Map)
                    && pawn.CanReach(side, PathEndMode.OnCell, Danger.Some)
                    && (partner == null || partner.Map != pawn.Map
                        || partner.CanReach(side, PathEndMode.OnCell, Danger.Some)))
                {
                    seats.Add(side);
                }
            }
            if (seats.Count == 0)
            {
                return best;
            }
            int idx = DateNightDoubleDates.StandIndex(pawn, partner);
            if (idx >= seats.Count)
            {
                return AdjacentTo(seats[0], pawn, partner);
            }
            return seats[idx];
        }

        /// <summary>Shared venue root (table, gather spot, outdoor cell) before pairing stand cells.</summary>
        public static LocalTargetInfo FindVenueRoot(DateActivity activity, Pawn pawn, Pawn partner)
        {
            if (DateNightDoubleDates.TryGetSharedVenue(pawn, out LocalTargetInfo shared) && shared.IsValid)
            {
                return shared;
            }

            DateActivity kind = activity == DateActivity.Gift ? DateActivity.Hangout : activity;
            if (DateNightVenues.TryGetPreferredRoot(pawn, partner, out LocalTargetInfo fav)
                && fav.IsValid
                && VenueFitsActivity(kind, fav, pawn.Map))
            {
                return fav;
            }

            LocalTargetInfo venue = LocalTargetInfo.Invalid;
            switch (kind)
            {
                case DateActivity.Dinner:
                {
                    LocalTargetInfo dinner = FindDinnerTable(pawn, partner);
                    if (dinner.IsValid)
                    {
                        return dinner;
                    }
                    break;
                }
                case DateActivity.Picnic:
                case DateActivity.Walk:
                {
                    IntVec3 cell = FindOutdoorSpot(pawn, partner, preferBeauty: true);
                    if (cell.IsValid)
                    {
                        venue = cell;
                    }
                    break;
                }
                case DateActivity.Stargaze:
                {
                    IntVec3 cell = FindOutdoorSpot(pawn, partner, preferBeauty: false);
                    if (cell.IsValid)
                    {
                        venue = cell;
                    }
                    break;
                }
                case DateActivity.Dance:
                {
                    Thing gather = FindGatherSpotFor(pawn, partner);
                    if (gather != null)
                    {
                        venue = gather;
                    }
                    break;
                }
                case DateActivity.Recreation:
                {
                    Building joy = FindJoyBuildingFor(pawn, partner);
                    if (joy != null)
                    {
                        venue = joy;
                    }
                    break;
                }
            }

            if (!venue.IsValid)
            {
                venue = DateNightDateUtility.FindDateSpot(pawn, partner);
            }
            return venue;
        }

        /// <summary>Where this activity happens. Falls back to the generic date spot.</summary>
        public static LocalTargetInfo FindSpotFor(DateActivity activity, Pawn pawn, Pawn partner)
        {
            DateActivity kind = activity == DateActivity.Gift ? DateActivity.Hangout : activity;
            LocalTargetInfo venue = FindVenueRoot(kind, pawn, partner);
            if (kind == DateActivity.Dinner)
            {
                LocalTargetInfo seat = DinnerSeatAt(venue, pawn, partner);
                if (seat.IsValid)
                {
                    return seat;
                }
            }
            if (kind == DateActivity.Walk || !venue.IsValid)
            {
                return venue;
            }
            if (kind == DateActivity.Recreation && venue.HasThing && venue.Thing.def.hasInteractionCell
                && DateNightDoubleDates.StandIndex(pawn, partner) == 0)
            {
                return venue.Thing.InteractionCell;
            }

            return PairStandCell(venue, pawn, partner);
        }

        private static bool VenueFitsActivity(DateActivity kind, LocalTargetInfo venue, Map map)
        {
            if (!venue.IsValid || map == null)
            {
                return false;
            }
            if (kind == DateActivity.Dinner)
            {
                Thing thing = venue.Thing ?? venue.Cell.GetEdifice(map);
                return thing != null && thing.def.IsTable;
            }
            if (kind == DateActivity.Stargaze || kind == DateActivity.Picnic || kind == DateActivity.Walk)
            {
                IntVec3 cell = venue.HasThing ? venue.Thing.Position : venue.Cell;
                return cell.InBounds(map) && !cell.Roofed(map);
            }
            return true;
        }

        private static LocalTargetInfo FindDinnerTable(Pawn pawn, Pawn partner)
        {
            Building best = null;
            int bestId = int.MaxValue;
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
                if (building.thingIDNumber < bestId)
                {
                    bestId = building.thingIDNumber;
                    best = building;
                }
            }
            return best == null ? LocalTargetInfo.Invalid : best;
        }

        private static LocalTargetInfo DinnerSeatAt(LocalTargetInfo table, Pawn pawn, Pawn partner)
        {
            if (!table.IsValid)
            {
                return LocalTargetInfo.Invalid;
            }

            Map map = pawn.Map;
            Thing thing = table.Thing ?? table.Cell.GetEdifice(map);
            if (thing == null)
            {
                return table;
            }

            var seats = new List<IntVec3>();
            foreach (IntVec3 side in GenAdj.CellsAdjacentCardinal(thing))
            {
                if (side.InBounds(map) && side.Standable(map)
                    && pawn.CanReach(side, PathEndMode.OnCell, Danger.Some)
                    && (partner == null || partner.Map != map
                        || partner.CanReach(side, PathEndMode.OnCell, Danger.Some)))
                {
                    seats.Add(side);
                }
            }
            if (seats.Count == 0)
            {
                return thing;
            }
            int idx = DateNightDoubleDates.StandIndex(pawn, partner);
            if (idx >= seats.Count)
            {
                return AdjacentTo(seats[seats.Count - 1], pawn, partner);
            }
            return seats[idx];
        }

        /// <summary>
        /// Two standable cells at the same venue. Initiator takes the first, partner the
        /// second, so they never claim the same chair / campfire cell.
        /// </summary>
        private static LocalTargetInfo PairStandCell(LocalTargetInfo venue, Pawn pawn, Pawn partner)
        {
            IntVec3 root = venue.HasThing ? venue.Thing.Position : venue.Cell;
            Map map = pawn.Map;
            var seats = new List<IntVec3>();
            if (IsSharedStandable(root, map, pawn, partner))
            {
                seats.Add(root);
            }
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 side = root + dir;
                if (IsSharedStandable(side, map, pawn, partner) && !seats.Contains(side))
                {
                    seats.Add(side);
                }
            }
            if (seats.Count == 0)
            {
                return venue;
            }
            int idx = DateNightDoubleDates.StandIndex(pawn, partner);
            if (idx >= seats.Count)
            {
                return AdjacentTo(seats[0], pawn, partner);
            }
            return seats[idx];
        }

        private static bool IsSharedStandable(IntVec3 cell, Map map, Pawn pawn, Pawn partner)
        {
            if (!cell.InBounds(map) || !cell.Standable(map))
            {
                return false;
            }
            if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }
            if (partner != null && partner.Map == map
                && !partner.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }
            return true;
        }

        /// <summary>Partner stands next to the shared venue instead of occupying the same cell.</summary>
        public static LocalTargetInfo AdjacentTo(LocalTargetInfo venue, Pawn pawn, Pawn partner)
        {
            IntVec3 root = venue.HasThing ? venue.Thing.Position : venue.Cell;
            Map map = pawn.Map;
            foreach (IntVec3 dir in GenAdj.CardinalDirections)
            {
                IntVec3 side = root + dir;
                if (!side.InBounds(map) || !side.Standable(map))
                {
                    continue;
                }
                if (partner != null && side == partner.Position)
                {
                    continue;
                }
                if (pawn.CanReach(side, PathEndMode.OnCell, Danger.Some))
                {
                    return side;
                }
            }
            return venue;
        }

        /// <summary>
        /// Search origin that does not depend on where each pawn currently stands,
        /// so both partners get the same outdoor cell even if they resolve minutes apart.
        /// </summary>
        private static IntVec3 StableSearchRoot(Pawn pawn, Pawn partner)
        {
            Map map = pawn.Map;
            Thing gather = FindGatherSpotFor(pawn, partner);
            if (gather != null)
            {
                return gather.Position;
            }

            Building first = null;
            int bestId = int.MaxValue;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.thingIDNumber < bestId)
                {
                    bestId = building.thingIDNumber;
                    first = building;
                }
            }
            if (first != null)
            {
                return first.Position;
            }
            return map.Center;
        }
    }
}
