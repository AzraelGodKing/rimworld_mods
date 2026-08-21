using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    public static class DateNightDateUtility
    {
        public static bool CanDate(Pawn pawn, Pawn partner, bool force = false)
        {
            if (pawn == null || partner == null || pawn == partner)
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || !partner.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.Dead || partner.Dead || pawn.Downed || partner.Downed)
            {
                return false;
            }
            if (!force && (pawn.Drafted || partner.Drafted))
            {
                return false;
            }
            if (pawn.ageTracker == null || partner.ageTracker == null
                || !pawn.ageTracker.Adult || !partner.ageTracker.Adult)
            {
                return false;
            }
            if (!pawn.DevelopmentalStage.Adult() || !partner.DevelopmentalStage.Adult())
            {
                return false;
            }
            if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, partner))
            {
                return false;
            }
            if (!pawn.Spawned || !partner.Spawned || pawn.Map != partner.Map)
            {
                return false;
            }
            if (pawn.health == null || partner.health == null
                || !pawn.health.capacities.CanBeAwake || !partner.health.capacities.CanBeAwake)
            {
                return false;
            }
            return true;
        }

        public static void TickScheduledDate(Pawn pawn)
        {
            if (!DateNightUtility.IsDateSchedule(pawn) || pawn.Map == null || pawn.jobs == null)
            {
                return;
            }
            if (pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                return;
            }
            if (pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate)
            {
                return;
            }
            if (DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn) || DateNightUtility.IsDoingNeedJob(pawn))
            {
                return;
            }

            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner == null || !DateNightUtility.IsDateSchedule(partner))
            {
                return;
            }

            TryStartDateNow(pawn);
        }

        public static bool TryStartDateNow(Pawn pawn, bool force = false)
        {
            if (pawn?.jobs == null)
            {
                return false;
            }
            if (pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate)
            {
                return true;
            }

            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (!CanDate(pawn, partner, force))
            {
                return false;
            }
            if (DateNightDefOf.DateNight_GoOnDate == null)
            {
                return false;
            }
            if (!force && DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn))
            {
                return false;
            }

            LocalTargetInfo spot = FindDateSpot(pawn, partner);
            Job job = JobMaker.MakeJob(DateNightDefOf.DateNight_GoOnDate);
            job.SetTarget(TargetIndex.A, partner);
            job.SetTarget(TargetIndex.B, spot);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.ignoreForbidden = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            return pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate;
        }

        /// <summary>Ticks a good date keeps the lovin-chance spark alive (1 day).</summary>
        public const int GoodDateBoostTicks = 60000;

        // pawn id -> tick of their last completed date (post-date lovin boost).
        private static Dictionary<int, int> lastGoodDateTicks = new Dictionary<int, int>();

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref lastGoodDateTicks, "dateNightLastGoodDateTicks",
                LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && lastGoodDateTicks == null)
            {
                lastGoodDateTicks = new Dictionary<int, int>();
            }
        }

        public static bool HadRecentGoodDate(Pawn pawn)
        {
            if (pawn == null || !lastGoodDateTicks.TryGetValue(pawn.thingIDNumber, out int tick))
            {
                return false;
            }
            return Find.TickManager.TicksGame - tick <= GoodDateBoostTicks;
        }

        public static void NotifyDateFinished(Pawn pawn, Pawn partner, DateActivity activity, LocalTargetInfo spot)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || partner == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }

            ThoughtDef def = PickQualityThought(pawn, partner, activity, spot);
            if (def != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(def, partner);
            }

            if (DateNightMod.Settings == null || DateNightMod.Settings.postDateLovinBoost)
            {
                lastGoodDateTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }

            DateNightWindows.NotifyDateSuccess(pawn, partner);
        }

        /// <summary>
        /// Wonderful / nice / awkward, from venue beauty, weather, whether the couple
        /// did something more special than standing around, and a seeded roll shared
        /// by both partners.
        /// </summary>
        private static ThoughtDef PickQualityThought(Pawn pawn, Pawn partner, DateActivity activity, LocalTargetInfo spot)
        {
            if (DateNightMod.Settings != null && !DateNightMod.Settings.enableDateQuality)
            {
                return DateNightDefOf.DateNight_HadADate;
            }

            Map map = pawn.Map;
            int score = 0;
            if (activity != DateActivity.Hangout && activity != DateActivity.Unresolved)
            {
                score++;
            }

            IntVec3 cell = spot.IsValid ? spot.Cell : pawn.Position;
            if (map != null && cell.InBounds(map))
            {
                float beauty = BeautyUtility.AverageBeautyPerceptible(cell, map);
                if (beauty >= 4f)
                {
                    score++;
                }
                else if (beauty < 0f)
                {
                    score--;
                }
                if (!cell.Roofed(map) && DateNightActivities.IsRaining(map))
                {
                    score--;
                }
            }

            Rand.PushState(Gen.HashCombineInt(DateNightActivities.CoupleSeed(pawn, partner), GenDate.DaysPassed));
            score += Rand.RangeInclusive(-1, 1);
            Rand.PopState();

            if (score >= 2 && DateNightDefOf.DateNight_DateWonderful != null)
            {
                return DateNightDefOf.DateNight_DateWonderful;
            }
            if (score <= -1 && DateNightDefOf.DateNight_DateAwkward != null)
            {
                return DateNightDefOf.DateNight_DateAwkward;
            }
            return DateNightDefOf.DateNight_HadADate;
        }

        /// <summary>Date cut short by a draft, mental break, or an active threat.</summary>
        public static void NotifyDateInterrupted(Pawn pawn, Pawn partner)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }

            bool ruined = pawn.Drafted
                || pawn.InMentalState
                || (partner != null && (partner.Drafted || partner.InMentalState))
                || (pawn.Map != null && GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map));
            if (!ruined)
            {
                return;
            }

            ThoughtDef def = DateNightDefOf.DateNight_DateRuined;
            if (def != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(def);
            }
        }

        public static void NotifyGiftGiven(Pawn giver, Pawn receiver)
        {
            if (receiver?.needs?.mood?.thoughts?.memories == null || giver == null)
            {
                return;
            }
            if (receiver.ageTracker == null || !receiver.ageTracker.Adult || !receiver.DevelopmentalStage.Adult())
            {
                return;
            }

            ThoughtDef def = DateNightDefOf.DateNight_ReceivedGift;
            if (def != null)
            {
                receiver.needs.mood.thoughts.memories.TryGainMemory(def, giver);
            }
        }

        public static LocalTargetInfo FindDateSpot(Pawn pawn, Pawn partner)
        {
            Thing gather = FindGatherSpot(pawn, partner);
            if (gather != null)
            {
                return gather;
            }

            IntVec3 mid = new IntVec3(
                (pawn.Position.x + partner.Position.x) / 2,
                0,
                (pawn.Position.z + partner.Position.z) / 2);
            if (mid.InBounds(pawn.Map) && mid.Standable(pawn.Map)
                && pawn.CanReach(mid, PathEndMode.OnCell, Danger.Deadly)
                && partner.CanReach(mid, PathEndMode.OnCell, Danger.Deadly))
            {
                return mid;
            }

            return partner;
        }

        private static Thing FindGatherSpot(Pawn pawn, Pawn partner)
        {
            Thing best = null;
            float bestDist = float.MaxValue;
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                CompGatherSpot gather = building.TryGetComp<CompGatherSpot>();
                if (gather == null || !gather.Active)
                {
                    continue;
                }
                if (!pawn.CanReach(building, PathEndMode.OnCell, Danger.Deadly)
                    || !partner.CanReach(building, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(building.Position)
                    + partner.Position.DistanceToSquared(building.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = building;
                }
            }
            return best;
        }
    }
}
