using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // The same moments vanilla would rather sleep, eat, recreate, or take a
    // drug than work. A floor assignment does not outrank those.
    public static class NeedsBreak
    {
        public static bool ShouldBreak(Pawn pawn)
        {
            return WantsFood(pawn) || WantsRest(pawn) || WantsJoy(pawn) || WantsChemical(pawn);
        }

        public static bool WantsFood(Pawn pawn)
        {
            Need_Food food = pawn?.needs?.food;
            if (food == null || pawn.RaceProps == null)
            {
                return false;
            }

            if ((int)food.CurCategory < (int)HungerCategory.Starving
                && FoodUtility.ShouldBeFedBySomeone(pawn))
            {
                return false;
            }

            return food.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat;
        }

        // Mirrors JobGiver_GetRest's schedule. Work hours do not send them to
        // bed; sleep hours, and being actually tired outside those hours, do.
        public static bool WantsRest(Pawn pawn)
        {
            Need_Rest rest = pawn?.needs?.rest;
            if (rest == null || pawn.mindState == null)
            {
                return false;
            }

            if (Find.TickManager.TicksGame < pawn.mindState.canSleepTick)
            {
                return false;
            }

            if (!RestUtility.CanFallAsleep(pawn))
            {
                return false;
            }

            TimeAssignmentDef schedule = pawn.timetable?.CurrentAssignment ?? TimeAssignmentDefOf.Anything;
            float level = rest.CurLevel;
            if (schedule == TimeAssignmentDefOf.Anything || schedule == TimeAssignmentDefOf.Joy)
            {
                return level < 0.3f;
            }

            if (schedule == TimeAssignmentDefOf.Meditate)
            {
                return level < 0.16f;
            }

            if (schedule == TimeAssignmentDefOf.Sleep)
            {
                return true;
            }

            return false;
        }

        public static bool WantsJoy(Pawn pawn)
        {
            Need_Joy joy = pawn?.needs?.joy;
            if (joy == null || Find.TickManager.TicksGame < 5000)
            {
                return false;
            }

            if (JoyUtility.LordPreventsGettingJoy(pawn))
            {
                return false;
            }

            TimeAssignmentDef schedule = pawn.timetable?.CurrentAssignment ?? TimeAssignmentDefOf.Anything;
            if (!schedule.allowJoy)
            {
                return false;
            }

            float level = joy.CurLevel;
            if (schedule == TimeAssignmentDefOf.Anything)
            {
                return level < 0.35f;
            }

            if (schedule == TimeAssignmentDefOf.Joy || schedule == TimeAssignmentDefOf.Sleep)
            {
                return level < 0.95f;
            }

            return false;
        }

        public static bool WantsChemical(Pawn pawn)
        {
            if (pawn?.needs?.AllNeeds == null)
            {
                return false;
            }

            for (int i = 0; i < pawn.needs.AllNeeds.Count; i++)
            {
                if (pawn.needs.AllNeeds[i] is Need_Chemical chemical
                    && chemical.CurCategory <= DrugDesireCategory.Desire)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
