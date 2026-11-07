#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DateNight
{
    public static class DateNightUtility
    {
        public const float AlwaysDoLovinMtbHours = 0.1f;
        public const int AlwaysDoLovinCooldownTicks = 100;

        // Stable rendezvous bed per couple for the current Lovin window (no claim churn).
        // Ephemeral — cleared on new/load game (not scribed); thing IDs reset across colonies.
        // Holds the bed itself, not its id: the old id cache scanned every colonist
        // bed to find it again, every 60 ticks per Lovin pawn (AZR-542).
        private static readonly Dictionary<long, RendezvousEntry> RendezvousBeds = new Dictionary<long, RendezvousEntry>();
        private const int ReachRecheckTicks = 250;

        private struct RendezvousEntry
        {
            public Building_Bed bed;
            public int reachCheckedTick;
        }

        public static void ClearEphemeral()
        {
            RendezvousBeds.Clear();
        }

        public static bool IsLovinSchedule(Pawn pawn)
        {
            if (pawn?.timetable == null || DateNightDefOf.DateNight_Lovin == null)
            {
                return false;
            }
            return pawn.timetable.CurrentAssignment == DateNightDefOf.DateNight_Lovin;
        }

        public static bool IsDateSchedule(Pawn pawn)
        {
            if (pawn?.timetable == null || DateNightDefOf.DateNight_Date == null)
            {
                return false;
            }
            return pawn.timetable.CurrentAssignment == DateNightDefOf.DateNight_Date;
        }

        public static bool IsLovinOrDateSchedule(Pawn pawn)
        {
            return IsLovinSchedule(pawn) || IsDateSchedule(pawn);
        }

        /// <summary>
        /// True while this pawn is already in vanilla lovin, Date Night private time,
        /// or a replacement job from animation / RJW / breeding-ritual mods.
        /// Restarting JobDef.Lovin in that window cycles their animations and
        /// never lets the original job finish (so pregnancy never rolls).
        /// </summary>
        public static bool IsBusyWithLovin(Pawn pawn)
        {
            if (pawn?.jobs?.curDriver == null || pawn.CurJobDef == null)
            {
                return false;
            }

            JobDef def = pawn.CurJobDef;
            if (def == JobDefOf.Lovin || def == DateNightDefOf.DateNight_SelfLovin)
            {
                return true;
            }
            if (pawn.jobs.curDriver is JobDriver_Lovin)
            {
                return true;
            }
            if (def.driverClass != null && typeof(JobDriver_Lovin).IsAssignableFrom(def.driverClass))
            {
                return true;
            }
            if (DateNightRjwSoftCompat.IsRjwSexJob(pawn))
            {
                return true;
            }

            return LooksLikeLovinJob(def, pawn.jobs.curDriver);
        }

        /// <summary>
        /// Ideology rituals and similar ceremonies freeze if a pawn is yanked
        /// onto a date / lovin job. Also any lord that forbids long-need jobs.
        /// </summary>
        public static bool IsBusyWithRitual(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return false;
            }

            Lord lord = pawn.GetLord();
            if (lord?.LordJob is LordJob_Ritual)
            {
                return true;
            }
            if (lord?.CurLordToil != null && !lord.CurLordToil.AllowSatisfyLongNeeds)
            {
                return true;
            }

            DutyDef? duty = pawn.mindState?.duty?.def;
            if (duty == null)
            {
                return false;
            }
            if (!RitualDuty.TryGetValue(duty, out bool ritual))
            {
                ritual = ContainsAny(duty.defName, RitualDutyTokens);
                RitualDuty[duty] = ritual;
            }
            return ritual;
        }

        // Defs and types never change after load, so each name is classified once
        // instead of string-searched on every check (AZR-542).
        private static readonly Dictionary<DutyDef, bool> RitualDuty = new Dictionary<DutyDef, bool>();
        private static readonly Dictionary<JobDef, bool> LovinJobDef = new Dictionary<JobDef, bool>();
        private static readonly Dictionary<System.Type, bool> LovinDriverType = new Dictionary<System.Type, bool>();

        private static readonly string[] RitualDutyTokens = { "Ritual", "Spectate", "Bestow" };

        private static readonly string[] LovinTokens =
        {
            "Lovin", "JobDriver_Sex", "SexBase", "JoinInBed", "GettinLoved", "Masturbate",
            "Quickie", "Breeding", "Rimworld_Animations", "RimworldAnimations",
        };

        internal static bool ContainsAny(string? name, string[] tokens)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < tokens.Length; i++)
            {
                if (name.IndexOf(tokens[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LooksLikeLovinJob(JobDef def, JobDriver driver)
        {
            if (!LovinJobDef.TryGetValue(def, out bool byDef))
            {
                byDef = ContainsAny(def.defName, LovinTokens) || ContainsAny(def.driverClass?.Name, LovinTokens);
                LovinJobDef[def] = byDef;
            }
            if (byDef)
            {
                return true;
            }

            System.Type driverType = driver.GetType();
            if (!LovinDriverType.TryGetValue(driverType, out bool byType))
            {
                byType = false;
                System.Type? t = driverType;
                while (t != null && t != typeof(JobDriver))
                {
                    if (ContainsAny(t.Name, LovinTokens) || ContainsAny(t.Namespace, LovinTokens))
                    {
                        byType = true;
                        break;
                    }
                    t = t.BaseType;
                }
                LovinDriverType[driverType] = byType;
            }
            return byType;
        }

        public static bool HasAnyHour(Pawn pawn, TimeAssignmentDef def)
        {
            if (pawn?.timetable == null || def == null)
            {
                return false;
            }
            for (int h = 0; h < 24; h++)
            {
                if (pawn.timetable.GetAssignment(h) == def)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool SameHours(Pawn a, Pawn b, TimeAssignmentDef def)
        {
            if (a?.timetable == null || b?.timetable == null || def == null)
            {
                return true;
            }
            for (int h = 0; h < 24; h++)
            {
                bool ah = a.timetable.GetAssignment(h) == def;
                bool bh = b.timetable.GetAssignment(h) == def;
                if (ah != bh)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Copy painted hours of <paramref name="def"/> from source onto target.
        /// Hours where source has the slot are painted on target; hours where
        /// only the target had it are cleared back to Anything.
        /// </summary>
        public static void CopyHours(Pawn source, Pawn target, TimeAssignmentDef def)
        {
            if (source?.timetable == null || target?.timetable == null || def == null)
            {
                return;
            }

            TimeAssignmentDef anything = TimeAssignmentDefOf.Anything;
            for (int h = 0; h < 24; h++)
            {
                bool srcHas = source.timetable.GetAssignment(h) == def;
                bool dstHas = target.timetable.GetAssignment(h) == def;
                if (srcHas)
                {
                    target.timetable.SetAssignment(h, def);
                }
                else if (dstHas)
                {
                    target.timetable.SetAssignment(h, anything);
                }
            }
        }

        public static bool ShouldBoostLovinChance(Pawn pawn, Pawn partner)
        {
            return IsLovinSchedule(pawn) || IsLovinSchedule(partner);
        }

        public static Job? TryMakeBedJob(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Downed || pawn.Drafted)
            {
                return null;
            }
            if (IsBusyWithRitual(pawn))
            {
                return null;
            }
            if (ShouldSatisfyNeedsBeforeBed(pawn))
            {
                return null;
            }
            if (RestUtility.DisturbancePreventsLyingDown(pawn))
            {
                return null;
            }

            Building_Bed? target = GetScheduledBed(pawn);
            if (target == null)
            {
                return null;
            }
            if (pawn.CurrentBed() == target)
            {
                return null;
            }
            if (IsBusyWithLovin(pawn))
            {
                return null;
            }

            Pawn? partner = DateNightPawns.LovePartner(pawn);
            if (!WaitingForPartner(pawn, partner, target)
                && CanSelfLovin(pawn)
                && OnLovinCooldownReady(pawn)
                && !ShouldSatisfyNeedsBeforeLovin(pawn)
                && DateNightDefOf.DateNight_SelfLovin != null)
            {
                Job self = JobMaker.MakeJob(DateNightDefOf.DateNight_SelfLovin, target);
                self.ignoreForbidden = true;
                return self;
            }

            return JobMaker.MakeJob(JobDefOf.LayDown, target);
        }

        public static void TickScheduledPawn(Pawn pawn)
        {
            if (!IsLovinSchedule(pawn) || pawn.Map == null || pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                return;
            }
            if (IsBusyWithRitual(pawn))
            {
                return;
            }
            if (pawn.jobs == null)
            {
                return;
            }
            if (IsBusyWithLovin(pawn))
            {
                return;
            }

            Pawn? partner = DateNightPawns.LovePartner(pawn);
            if (partner != null && IsBusyWithLovin(partner))
            {
                return;
            }
            if (partner != null && IsBusyWithRitual(partner))
            {
                return;
            }

            if (pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate)
            {
                return;
            }

            // Needs first: never yank someone off food / chem / etc.
            if (ShouldSatisfyNeedsBeforeBed(pawn) || IsDoingNeedJob(pawn))
            {
                return;
            }

            if (partner != null
                && IsLovinSchedule(partner)
                && !DateNightHooks.CanForceCoupleLovin(pawn, partner))
            {
                DateNightDateUtility.TryStartDateNow(pawn);
                return;
            }

            // Friends / sweethearts painted Lovin: date instead of private time.
            if (partner == null)
            {
                Pawn? dateMate = DateNightRomance.FindDatePartner(pawn);
                if (dateMate != null
                    && !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, dateMate)
                    && DateNightDateUtility.CanDate(pawn, dateMate))
                {
                    DateNightDateUtility.TryStartDateNow(pawn);
                    return;
                }
            }

            Building_Bed? target = GetScheduledBed(pawn);
            if (target == null)
            {
                return;
            }

            Building_Bed current = pawn.CurrentBed();
            bool inTarget = current == target;
            bool goingToTarget = pawn.CurJobDef == JobDefOf.LayDown
                && pawn.CurJob?.GetTarget(TargetIndex.A).Thing == target;

            if (!inTarget && !goingToTarget)
            {
                Job lay = JobMaker.MakeJob(JobDefOf.LayDown, target);
                pawn.jobs.StartJob(lay, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
                return;
            }

            if (!inTarget)
            {
                return;
            }

            if (partner != null && partner.CurrentBed() == target && target.SleepingSlotsCount > 1)
            {
                if (DateNightHooks.CanForceCoupleLovin(pawn, partner))
                {
                    TryStartLovinNow(pawn);
                }
                else
                {
                    DateNightDateUtility.TryStartDateNow(pawn);
                }
                return;
            }

            if (WaitingForPartner(pawn, partner, target))
            {
                return;
            }

            TryStartSelfLovinNow(pawn);
        }

        public static bool TryStartLovinNow(Pawn pawn)
        {
            if (pawn?.jobs == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (IsBusyWithRitual(pawn))
            {
                return false;
            }
            if (IsBusyWithLovin(pawn))
            {
                return true;
            }

            Building_Bed bed = pawn.CurrentBed();
            if (bed == null || bed.Medical || bed.SleepingSlotsCount <= 1)
            {
                return false;
            }
            if (!pawn.health.capacities.CanBeAwake)
            {
                return false;
            }

            Pawn? partner = LovePartnerRelationUtility.GetPartnerInMyBed(pawn);
            if (partner == null)
            {
                // Fallback: love partner physically in the same bed occupants list.
                partner = FindLovePartnerOccupying(pawn, bed);
            }
            if (partner == null || !partner.health.capacities.CanBeAwake || partner.Dead || partner.Downed)
            {
                return false;
            }
            if (IsBusyWithLovin(partner))
            {
                return true;
            }
            if (!DateNightHooks.CanForceCoupleLovin(pawn, partner))
            {
                return false;
            }

            // Both partners must be topped up enough before lovin.
            if (ShouldSatisfyNeedsBeforeLovin(pawn) || ShouldSatisfyNeedsBeforeLovin(partner))
            {
                return false;
            }

            // Pregnancy-safe (default): keep vanilla post-lovin cooldown.
            // Eager: clear leftover cooldown while on the Lovin schedule.
            if (DateNightMod.Settings.pregnancySafeCooldown)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick
                    || Find.TickManager.TicksGame < partner.mindState.canLovinTick)
                {
                    return false;
                }
            }
            else if (IsLovinSchedule(pawn) || IsLovinSchedule(partner))
            {
                pawn.mindState.canLovinTick = 0;
                partner.mindState.canLovinTick = 0;
            }
            else if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick
                || Find.TickManager.TicksGame < partner.mindState.canLovinTick)
            {
                return false;
            }

            JobDef jobDef = DateNightRjwSoftCompat.ScheduledCoupleJobDef;
            if (jobDef != JobDefOf.Lovin)
            {
                if (DateNightRjwSoftCompat.TryStartJoinInBed(pawn, partner, bed))
                {
                    DateNightWindows.NotifyLovinSuccess(pawn, partner);
                    return true;
                }
                return false;
            }

            Job lovin = JobMaker.MakeJob(jobDef, partner, bed);
            lovin.ignoreForbidden = true;
            pawn.jobs.StartJob(lovin, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            if (DateNightRjwSoftCompat.IsScheduledCoupleJob(pawn.CurJobDef))
            {
                DateNightWindows.NotifyLovinSuccess(pawn, partner);
                return true;
            }
            return false;
        }

        public static bool CanSelfLovin(Pawn pawn, bool ignoreSetting = false)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || pawn.ageTracker == null || !pawn.ageTracker.Adult)
            {
                return false;
            }
            if (!pawn.DevelopmentalStage.Adult())
            {
                return false;
            }
            if (!DateNightHooks.IdeologyAllowsLovin(pawn))
            {
                return false;
            }
            if (!ignoreSetting && !DateNightMod.Settings.allowSelfLovin)
            {
                return false;
            }
            return pawn.health != null && pawn.health.capacities.CanBeAwake;
        }

        public static bool TryStartSelfLovinNow(Pawn pawn, bool force = false)
        {
            if (!CanSelfLovin(pawn, ignoreSetting: force) || pawn.jobs == null)
            {
                return false;
            }
            if (!force && IsBusyWithRitual(pawn))
            {
                return false;
            }
            if (IsBusyWithLovin(pawn))
            {
                return true;
            }
            if (!force && ShouldSatisfyNeedsBeforeLovin(pawn))
            {
                return false;
            }
            if (!force && !OnLovinCooldownReady(pawn))
            {
                return false;
            }

            Building_Bed? bed = pawn.CurrentBed();
            if (!IsUsableAnyBed(bed, pawn))
            {
                bed = GetSelfLovinBed(pawn);
            }
            if (!IsUsableAnyBed(bed, pawn))
            {
                return false;
            }
            if (LovePartnerRelationUtility.GetPartnerInMyBed(pawn) != null)
            {
                return false;
            }

            if (DateNightRjwSoftCompat.TryStartMasturbate(pawn, bed))
            {
                return true;
            }

            if (DateNightDefOf.DateNight_SelfLovin == null)
            {
                return false;
            }

            Job job = JobMaker.MakeJob(DateNightDefOf.DateNight_SelfLovin, bed);
            job.ignoreForbidden = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            return pawn.CurJobDef == DateNightDefOf.DateNight_SelfLovin;
        }

        public static void NotifySelfLovinFinished(Pawn pawn)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult)
            {
                return;
            }

            ThoughtDef? thought = DateNightDefOf.DateNight_PrivateTime;
            if (thought != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }

            ApplyLovinCooldown(pawn);
        }

        /// <summary>
        /// Couple rendezvous if a partner is likely to join; otherwise any usable bed
        /// (including singles) for self-lovin.
        /// </summary>
        public static Building_Bed? GetScheduledBed(Pawn pawn)
        {
            Pawn? partner = DateNightPawns.LovePartner(pawn);
            Building_Bed? couple = GetRendezvousBed(pawn);
            if (couple != null && WaitingForPartner(pawn, partner, couple))
            {
                return couple;
            }
            if (couple != null && partner != null && partner.CurrentBed() == couple)
            {
                return couple;
            }
            if (CanSelfLovin(pawn))
            {
                return GetSelfLovinBed(pawn) ?? couple;
            }
            return couple;
        }

        private static Building_Bed? GetSelfLovinBed(Pawn pawn)
        {
            Building_Bed? owned = pawn.ownership?.OwnedBed;
            if (IsUsableAnyBed(owned, pawn))
            {
                return owned;
            }

            Building_Bed found = RestUtility.FindBedFor(pawn);
            if (IsUsableAnyBed(found, pawn))
            {
                return found;
            }

            return null;
        }

        private static bool WaitingForPartner(Pawn pawn, Pawn? partner, Building_Bed bed)
        {
            if (partner == null || bed == null || bed.SleepingSlotsCount <= 1)
            {
                return false;
            }
            if (!IsLovinSchedule(partner))
            {
                return false;
            }
            if (partner.Map != pawn.Map || partner.Dead || partner.Downed)
            {
                return false;
            }
            return IsUsableDouble(bed, pawn, partner);
        }

        private static bool OnLovinCooldownReady(Pawn pawn)
        {
            if (pawn?.mindState == null)
            {
                return true;
            }
            if (DateNightMod.Settings.pregnancySafeCooldown)
            {
                return Find.TickManager.TicksGame >= pawn.mindState.canLovinTick;
            }
            if (IsLovinSchedule(pawn) && !DateNightMod.Settings.pregnancySafeCooldown)
            {
                pawn.mindState.canLovinTick = 0;
                return true;
            }
            return Find.TickManager.TicksGame >= pawn.mindState.canLovinTick;
        }

        private static void ApplyLovinCooldown(Pawn pawn)
        {
            if (pawn?.mindState == null)
            {
                return;
            }
            if (!DateNightMod.Settings.pregnancySafeCooldown
                && DateNightMod.Settings.eagerCooldown
                && IsLovinSchedule(pawn))
            {
                pawn.mindState.canLovinTick = Find.TickManager.TicksGame
                    + AlwaysDoLovinCooldownTicks;
                return;
            }

            pawn.mindState.canLovinTick = Find.TickManager.TicksGame
                + Rand.RangeInclusive(10000, 20000);
        }

        private static bool IsUsableAnyBed(Building_Bed? bed, Pawn pawn)
        {
            if (bed == null || bed.Destroyed || !bed.Spawned || bed.Medical)
            {
                return false;
            }
            if (bed.Map != pawn.Map)
            {
                return false;
            }
            if (!pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }
            return true;
        }

        /// <summary>Hungry / chem / mid need-job — don't force bed yet.</summary>
        public static bool ShouldSatisfyNeedsBeforeBed(Pawn pawn)
        {
            if (pawn?.needs == null)
            {
                return false;
            }
            if (IsDoingNeedJob(pawn))
            {
                return true;
            }

            Need_Food food = pawn.needs.food;
            if (food != null)
            {
                if (food.CurCategory >= HungerCategory.Hungry)
                {
                    return true;
                }
                if (food.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat)
                {
                    return true;
                }
            }

            if (HasUrgentChemicalNeed(pawn))
            {
                return true;
            }

            return false;
        }

        /// <summary>Same as bed gate, plus exhausted rest (sleep in bed before lovin).</summary>
        public static bool ShouldSatisfyNeedsBeforeLovin(Pawn pawn)
        {
            if (ShouldSatisfyNeedsBeforeBed(pawn))
            {
                return true;
            }

            Need_Rest? rest = pawn.needs?.rest;
            if (rest != null && rest.CurCategory >= RestCategory.Exhausted)
            {
                return true;
            }

            return false;
        }

        public static bool IsDoingNeedJob(Pawn pawn)
        {
            JobDef? def = pawn?.CurJobDef;
            if (pawn == null || def == null)
            {
                return false;
            }
            if (def == JobDefOf.Ingest || def == JobDefOf.Vomit)
            {
                return true;
            }
            if (def == JobDefOf.Flee || def == JobDefOf.FleeAndCower)
            {
                return true;
            }
            if (pawn.CurJob != null && pawn.CurJob.playerForced)
            {
                return true;
            }
            return false;
        }

        private static bool HasUrgentChemicalNeed(Pawn pawn)
        {
            List<Need> needs = pawn.needs.AllNeeds;
            for (int i = 0; i < needs.Count; i++)
            {
                if (needs[i] is Need_Chemical chemical
                    && chemical.CurCategory >= DrugDesireCategory.Desire)
                {
                    return true;
                }
            }
            return false;
        }

        public static void NotifyEnteredLovinSchedule(Pawn pawn)
        {
            if (pawn.mindState != null && !DateNightMod.Settings.pregnancySafeCooldown)
            {
                pawn.mindState.canLovinTick = 0;
            }

            // Drop cached rendezvous so a new window can pick a sensible bed once.
            Pawn? partner = DateNightPawns.LovePartner(pawn);
            if (partner != null)
            {
                RendezvousBeds.Remove(DateNightActivities.CoupleKey(pawn, partner));
            }
        }

        public static void NotifyLeftLovinSchedule(Pawn pawn)
        {
            Pawn? partner = DateNightPawns.LovePartner(pawn);
            if (partner != null && !IsLovinSchedule(partner))
            {
                RendezvousBeds.Remove(DateNightActivities.CoupleKey(pawn, partner));
            }
        }

        /// <summary>
        /// One stable double bed for the couple. Does not claim ownership (LayDown toil claims).
        /// </summary>
        public static Building_Bed? GetRendezvousBed(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Pawn? partner = DateNightPawns.LovePartner(pawn);
            if (partner == null)
            {
                return DoubleBedOrNull(pawn.ownership?.OwnedBed) ?? RestUtility.FindBedFor(pawn);
            }

            long key = DateNightActivities.CoupleKey(pawn, partner);
            int now = Find.TickManager.TicksGame;
            if (RendezvousBeds.TryGetValue(key, out RendezvousEntry entry))
            {
                Building_Bed cached = entry.bed;
                bool stillThere = !cached.Destroyed && cached.Spawned && !cached.Medical
                    && cached.Map == pawn.Map;
                if (stillThere && now - entry.reachCheckedTick < ReachRecheckTicks)
                {
                    return cached;
                }
                if (stillThere && IsUsableDouble(cached, pawn, partner))
                {
                    entry.reachCheckedTick = now;
                    RendezvousBeds[key] = entry;
                    return cached;
                }
                RendezvousBeds.Remove(key);
            }

            Building_Bed? chosen = ResolveNewRendezvousBed(pawn, partner);
            if (chosen != null)
            {
                RendezvousBeds[key] = new RendezvousEntry { bed = chosen, reachCheckedTick = now };
            }
            return chosen;
        }

        private static Building_Bed? ResolveNewRendezvousBed(Pawn pawn, Pawn partner)
        {
            // Partner already in a double — join (no ownership churn).
            Building_Bed partnerCurrent = partner.CurrentBed();
            if (IsUsableDouble(partnerCurrent, pawn, partner))
            {
                return partnerCurrent;
            }

            Building_Bed selfCurrent = pawn.CurrentBed();
            if (IsUsableDouble(selfCurrent, pawn, partner))
            {
                return selfCurrent;
            }

            // Prefer an already-owned double (either partner), without reclaiming others.
            Building_Bed? owned = DoubleBedOrNull(pawn.ownership?.OwnedBed);
            if (IsUsableDouble(owned, pawn, partner))
            {
                return owned;
            }
            Building_Bed? partnerOwned = DoubleBedOrNull(partner.ownership?.OwnedBed);
            if (IsUsableDouble(partnerOwned, pawn, partner))
            {
                return partnerOwned;
            }

            return FindNearestFreeDouble(pawn, partner);
        }

        private static Building_Bed? FindNearestFreeDouble(Pawn pawn, Pawn partner)
        {
            Building_Bed? best = null;
            float bestDist = float.MaxValue;
            foreach (Building_Bed bed in pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
            {
                if (!IsUsableDouble(bed, pawn, partner))
                {
                    continue;
                }

                // Only take emptier / free doubles — never steal a bed fully owned by others.
                bool ownedByPair = bed.OwnersForReading.Contains(pawn)
                    || bed.OwnersForReading.Contains(partner);
                int foreignOwners = 0;
                foreach (Pawn owner in bed.OwnersForReading)
                {
                    if (owner != pawn && owner != partner)
                    {
                        foreignOwners++;
                    }
                }
                if (!ownedByPair && foreignOwners >= bed.SleepingSlotsCount)
                {
                    continue;
                }
                if (!ownedByPair && !bed.AnyUnownedSleepingSlot && foreignOwners > 0)
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(bed.Position);
                if (ownedByPair)
                {
                    dist -= 100000f;
                }
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = bed;
                }
            }
            return best;
        }

        private static bool IsUsableDouble(Building_Bed? bed, Pawn a, Pawn b)
        {
            if (bed == null || bed.Destroyed || !bed.Spawned || bed.Medical)
            {
                return false;
            }
            if (bed.SleepingSlotsCount <= 1)
            {
                return false;
            }
            if (bed.Map != a.Map)
            {
                return false;
            }
            if (!a.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }
            if (b != null && b.Map == a.Map && !b.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }
            return true;
        }

        private static Building_Bed? DoubleBedOrNull(Building_Bed? bed)
        {
            return bed != null && bed.SleepingSlotsCount > 1 ? bed : null;
        }


        private static Pawn? FindLovePartnerOccupying(Pawn pawn, Building_Bed bed)
        {
            foreach (Pawn occ in bed.CurOccupants)
            {
                if (occ != pawn && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, occ))
                {
                    return occ;
                }
            }
            return null;
        }

    }
}
