using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // The same moments vanilla would rather sleep, eat, recreate, or take a
    // drug than work. A floor assignment does not outrank those.
    public static class NeedsBreak
    {
        // How long a need may hold work back without the pawn starting
        // anything that meets it. Vanilla can fail to find a joy source (or a
        // bed, or food) while the need still reads low; without this limit the
        // colony wandered for hours and never took a stair trip.
        private const int GraceTicks = 1250;

        private static readonly Dictionary<int, int> wantSince = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> lastNeedJob = new Dictionary<int, int>();

        internal static void ResetSession()
        {
            wantSince.Clear();
            lastNeedJob.Clear();
        }

        public static bool ShouldBreak(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            bool wants = WantsFood(pawn) || WantsRest(pawn) || WantsJoy(pawn)
                || WantsChemical(pawn) || WantsMeditate(pawn);
            int id = pawn.thingIDNumber;
            if (!wants)
            {
                wantSince.Remove(id);
                return false;
            }

            int now = Find.TickManager.TicksGame;
            if (!wantSince.TryGetValue(id, out int since))
            {
                wantSince[id] = now;
                return true;
            }
            if (now - since < GraceTicks)
            {
                return true;
            }
            // Still tending the need (eating, sleeping, joy) recently: keep the break.
            return lastNeedJob.TryGetValue(id, out int last) && now - last < GraceTicks;
        }

        internal static void NoteJobStarted(Pawn pawn, Job job)
        {
            if (pawn == null || job?.def == null || !IsNeedJob(job.def))
            {
                return;
            }
            lastNeedJob[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            if (job.def == JobDefOf.Ingest)
            {
                FoodTripMemory.Ate(pawn);
            }
        }

        private static bool IsNeedJob(JobDef def)
        {
            return def.joyKind != null
                || def == JobDefOf.Ingest
                || def == JobDefOf.LayDown
                || def == JobDefOf.Meditate;
        }

        public static bool WantsMeditate(Pawn pawn)
        {
            if (!ModsConfig.RoyaltyActive || pawn?.timetable == null)
            {
                return false;
            }

            if (pawn.timetable.CurrentAssignment != TimeAssignmentDefOf.Meditate)
            {
                return false;
            }

            return MeditationUtility.CanMeditateNow(pawn);
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

            if (food.CurLevelPercentage >= pawn.RaceProps.FoodLevelPercentageWantEat)
            {
                return false;
            }

            // Hunger alone is not a reason to drop work: with nothing to eat
            // here or on a linked floor, vanilla keeps them working.
            return CanFindFood(pawn, food);
        }

        private static bool CanFindFood(Pawn pawn, Need_Food food)
        {
            if (!pawn.Spawned || pawn.Map == null)
            {
                return true;
            }

            bool desperate = food.CurCategory == HungerCategory.Starving;
            if (FoodUtility.TryFindBestFoodSourceFor(pawn, pawn, desperate, out _, out _,
                    canRefillDispenser: true, canUseInventory: true, canUsePackAnimalInventory: true,
                    allowForbidden: false, allowCorpse: pawn.AnimalOrWildMan()))
            {
                return true;
            }

            if (StrataMod.Settings != null && !StrataMod.Settings.foodRelayEnabled)
            {
                return false;
            }

            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);
            for (int i = 0; i < links.Count; i++)
            {
                Map map = links[i].map;
                if (map != null && map != pawn.Map && PawnRelay.HasFoodFor(pawn, map))
                {
                    return true;
                }
            }

            return false;
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

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_NeedsBreak_NoteJob
    {
        public static void Prefix(Pawn ___pawn, Job newJob)
        {
            if (___pawn?.RaceProps != null && ___pawn.RaceProps.Humanlike)
            {
                NeedsBreak.NoteJobStarted(___pawn, newJob);
            }
        }
    }
}
