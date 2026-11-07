#nullable enable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DateNight
{
    public static class DateNightDateUtility
    {
        public static bool CanDate(Pawn pawn, Pawn? partner, bool force = false)
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
            if (!force && (DateNightUtility.IsBusyWithRitual(pawn)
                || DateNightUtility.IsBusyWithRitual(partner)))
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
            if (!DateNightRomance.CanDateTogether(pawn, partner))
            {
                return false;
            }
            if (!force && DateNightRotrSoftCompat.ShouldYieldDate(pawn, partner))
            {
                return false;
            }
            if (!force && !DateCooldownReady(pawn, partner))
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
            if (DateNightUtility.IsBusyWithRitual(pawn))
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

            Pawn? partner = DateNightRomance.FindDatePartner(pawn);
            if (partner == null || !DateNightUtility.IsDateSchedule(partner))
            {
                return;
            }
            if (DateNightUtility.IsBusyWithRitual(partner))
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

            Pawn? partner = DateNightRomance.FindDatePartner(pawn);
            if (partner == null || !CanDate(pawn, partner, force))
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

            pawn.jobs.StartJob(MakeDateJob(pawn, partner), JobCondition.InterruptForced, null,
                resumeCurJobAfterwards: false);
            return pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate;
        }

        /// <summary>
        /// The one place a date job is built. The joy-giver path used to skip the
        /// bond start and double-date binding that a scheduled start did.
        /// </summary>
        public static Job MakeDateJob(Pawn pawn, Pawn partner)
        {
            DateNightRomance.EnsureBondStarted(pawn, partner);
            DateNightDoubleDates.BindIfPossible(pawn, partner);

            Job job = JobMaker.MakeJob(DateNightDefOf.DateNight_GoOnDate);
            job.SetTarget(TargetIndex.A, partner);
            job.SetTarget(TargetIndex.B, FindDateSpot(pawn, partner));
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.ignoreForbidden = true;
            return job;
        }

        /// <summary>Ticks a good date keeps the lovin-chance spark alive (1 day).</summary>
        public const int GoodDateBoostTicks = 60000;

        // pawn id -> tick of their last completed date (post-date lovin boost).
        private static Dictionary<int, int> lastGoodDateTicks = new Dictionary<int, int>();

        // pawn id -> tick when they may start another date (mirrors canLovinTick).
        private static Dictionary<int, int> canDateTicks = new Dictionary<int, int>();

        /// <summary>Call from the GameComponent ctor; a new game never runs ExposeData.</summary>
        public static void Reset()
        {
            lastGoodDateTicks = new Dictionary<int, int>();
            canDateTicks = new Dictionary<int, int>();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref lastGoodDateTicks, "dateNightLastGoodDateTicks",
                LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref canDateTicks, "dateNightCanDateTicks",
                LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (lastGoodDateTicks == null)
                {
                    lastGoodDateTicks = new Dictionary<int, int>();
                }
                if (canDateTicks == null)
                {
                    canDateTicks = new Dictionary<int, int>();
                }
            }
        }

        public static void PruneDeadPawns()
        {
            PruneDeadKeys(lastGoodDateTicks);
            PruneDeadKeys(canDateTicks);
        }

        private static void PruneDeadKeys(Dictionary<int, int> dict)
        {
            if (dict == null || dict.Count == 0)
            {
                return;
            }

            List<int>? remove = null;
            foreach (int id in dict.Keys)
            {
                if (!PawnStillAlive(id))
                {
                    if (remove == null)
                    {
                        remove = new List<int>();
                    }
                    remove.Add(id);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                dict.Remove(remove[i]);
            }
        }

        internal static bool PawnStillAlive(int id)
        {
            return FindPawnById(id) != null;
        }

        internal static Pawn? FindPawnById(int id)
        {
            return DateNightPawns.Alive(id);
        }

        /// <summary>
        /// Same durations as lovin: 10000–20000 ticks (~4–8 hours) by default,
        /// or ~100 ticks in Eager mode.
        /// </summary>
        public static void ApplyDateCooldown(Pawn pawn, Pawn? partner)
        {
            int until = Find.TickManager.TicksGame + DateCooldownDuration();
            SetCanDateTick(pawn, until);
            SetCanDateTick(partner, until);
        }

        public static bool DateCooldownReady(Pawn pawn, Pawn partner)
        {
            return IsDateCooldownReady(pawn) && IsDateCooldownReady(partner);
        }

        private static bool IsDateCooldownReady(Pawn pawn)
        {
            if (pawn == null)
            {
                return true;
            }
            if (!canDateTicks.TryGetValue(pawn.thingIDNumber, out int until))
            {
                return true;
            }
            return Find.TickManager.TicksGame >= until;
        }

        private static void SetCanDateTick(Pawn? pawn, int until)
        {
            if (pawn == null)
            {
                return;
            }
            if (!canDateTicks.TryGetValue(pawn.thingIDNumber, out int cur) || until > cur)
            {
                canDateTicks[pawn.thingIDNumber] = until;
            }
        }

        private static int DateCooldownDuration()
        {
            if (DateNightMod.Settings.eagerCooldown
                && !DateNightMod.Settings.pregnancySafeCooldown)
            {
                return DateNightUtility.AlwaysDoLovinCooldownTicks;
            }
            return Rand.RangeInclusive(10000, 20000);
        }

        public static bool HadRecentGoodDate(Pawn pawn)
        {
            if (pawn == null || !lastGoodDateTicks.TryGetValue(pawn.thingIDNumber, out int tick))
            {
                return false;
            }
            return Find.TickManager.TicksGame - tick <= GoodDateBoostTicks;
        }

        public static void NotifyDateFinished(Pawn pawn, Pawn? partner, DateActivity activity, LocalTargetInfo spot,
            bool fineMeal)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || partner == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }

            ThoughtDef? def = PickQualityThought(pawn, partner, activity, spot, fineMeal);
            if (def != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(def, partner);
                if (def == DateNightDefOf.DateNight_DateWonderful)
                {
                    DateNightVisuals.Burst(pawn, DateNightDefOf.DateNight_Sparkle);
                }
            }

            if (DateNightAnniversaries.IsAnniversaryToday(pawn, partner)
                && DateNightDefOf.DateNight_AnniversaryDate != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(
                    DateNightDefOf.DateNight_AnniversaryDate, partner);
                DateNightAnniversaries.NotifyDatedOnAnniversary(pawn, partner);
            }

            DateNightHistory.NotifyFinished(pawn, partner, def);
            DateNightVenues.NotifyFinished(pawn, partner, spot, def);
            DateNightDoubleDates.NotifyFinished(pawn, partner, def);
            DateNightRomance.NotifyDateFinished(pawn, partner, def);
            DateNightRotrSoftCompat.NotifyDateFinished(pawn, def,
                DateNightAnniversaries.IsAnniversaryToday(pawn, partner));

            if (DateNightMod.Settings.postDateLovinBoost)
            {
                lastGoodDateTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }

            DateNightWindows.NotifyDateSuccess(pawn, partner);
        }

        /// <summary>
        /// Wonderful / nice / awkward. Inputs are gathered here and scored by the
        /// pure <see cref="QualityScore"/>; the roll is seeded per couple and day so
        /// both partners agree on luck.
        /// </summary>
        private static ThoughtDef? PickQualityThought(Pawn pawn, Pawn partner, DateActivity activity,
            LocalTargetInfo spot, bool fineMeal)
        {
            if (!DateNightMod.Settings.enableDateQuality)
            {
                return DateNightDefOf.DateNight_HadADate;
            }

            CoupleTaste taste = CoupleTaste.Of(pawn, partner);
            var q = new DateQualityInputs
            {
                special = activity != DateActivity.Hangout && activity != DateActivity.Unresolved,
                anniversary = DateNightAnniversaries.IsAnniversaryToday(pawn, partner),
                favorite = DateNightVenues.IsAtFavorite(pawn, partner, spot),
                mutualOpinion = Mathf.Min(
                    pawn.relations?.OpinionOf(partner) ?? 0,
                    partner.relations?.OpinionOf(pawn) ?? 0),
                kind = taste.kind,
                abrasive = taste.abrasive,
                fineMeal = fineMeal,
                impressiveness = -1f,
            };

            Map map = pawn.Map;
            IntVec3 cell = spot.IsValid ? spot.Cell : pawn.Position;
            if (map != null && cell.InBounds(map))
            {
                q.beauty = BeautyUtility.AverageBeautyPerceptible(cell, map);
                q.rainedOn = !cell.Roofed(map) && DateNightActivities.IsRaining(map);
                Room? room = cell.GetRoom(map);
                if (room != null && !room.PsychologicallyOutdoors && room.ProperRoom)
                {
                    q.impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
                }
            }

            Rand.PushState(Gen.HashCombineInt(
                DateNightDoubleDates.QualitySeed(pawn, partner), GenDate.DaysPassed));
            q.roll = Rand.RangeInclusive(-1, 1);
            Rand.PopState();

            int score = QualityScore(q);
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

        /// <summary>
        /// 2+ is wonderful, -1 or less awkward. Opinion, traits, a fine meal and an
        /// impressive room joined beauty, weather and luck in 2.0.0 (AZR-559).
        /// </summary>
        public static int QualityScore(in DateQualityInputs q)
        {
            int score = q.roll;
            if (q.special) score++;
            if (q.beauty >= 4f) score++;
            else if (q.beauty < 0f) score--;
            if (q.rainedOn) score--;
            if (q.anniversary) score += 2;
            if (q.favorite) score++;
            if (q.mutualOpinion >= 60) score++;
            else if (q.mutualOpinion < 0) score--;
            if (q.kind) score++;
            if (q.abrasive) score--;
            if (q.fineMeal) score++;
            if (q.impressiveness >= 65f) score++;
            return score;
        }

        /// <summary>Date cut short by a draft, mental break, or an active threat.</summary>
        public static void NotifyDateInterrupted(Pawn pawn, Pawn? partner)
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

            ThoughtDef? def = DateNightDefOf.DateNight_DateRuined;
            if (def != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(def);
            }

            LocalTargetInfo spot = LocalTargetInfo.Invalid;
            if (pawn.jobs?.curJob != null)
            {
                spot = pawn.jobs.curJob.GetTarget(TargetIndex.B);
            }
            DateNightVenues.NotifyRuined(pawn, partner, spot);
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

            ThoughtDef? def = DateNightDefOf.DateNight_ReceivedGift;
            if (def != null)
            {
                receiver.needs.mood.thoughts.memories.TryGainMemory(def, giver);
            }
        }

        public static LocalTargetInfo FindDateSpot(Pawn pawn, Pawn partner)
        {
            Thing? gather = DateNightActivities.FindGatherSpotFor(pawn, partner);
            if (gather != null)
            {
                return gather;
            }

            IntVec3 outdoor = DateNightActivities.FindOutdoorSpot(pawn, partner, preferBeauty: true);
            if (outdoor.IsValid)
            {
                return outdoor;
            }

            return partner;
        }

        /// <summary>
        /// A good Lovin-window mood, nullified when the pawn's ideoligion disapproves
        /// of lovin (AZR-574). Checked through the same InitiatedLovin history event
        /// vanilla uses, so it follows whatever precepts the ideoligion has.
        /// </summary>
        internal static void TryGainLovinThought(Pawn? pawn, Pawn? other, ThoughtDef? def)
        {
            if (pawn != null && DateNightHooks.IdeologyAllowsLovin(pawn))
            {
                TryGainThought(pawn, other, def);
            }
        }

        internal static void TryGainThought(Pawn? pawn, Pawn? other, ThoughtDef? def)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null || other == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }
            pawn.needs.mood.thoughts.memories.TryGainMemory(def, other);
        }
    }

    /// <summary>Everything the date quality score looks at (AZR-559).</summary>
    public struct DateQualityInputs
    {
        public bool special;
        public float beauty;
        public bool rainedOn;
        public int roll;
        public bool anniversary;
        public bool favorite;
        public int mutualOpinion;
        public bool kind;
        public bool abrasive;
        public bool fineMeal;
        /// <summary>Room impressiveness, or -1 outdoors.</summary>
        public float impressiveness;
    }
}
