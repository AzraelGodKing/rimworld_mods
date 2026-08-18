using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    public static class DateNightUtility
    {
        public const float AlwaysDoLovinMtbHours = 0.1f;
        public const int AlwaysDoLovinCooldownTicks = 100;

        // Stable rendezvous bed per couple for the current Lovin window (no claim churn).
        private static readonly Dictionary<long, int> RendezvousBedIds = new Dictionary<long, int>();

        public static bool IsLovinSchedule(Pawn pawn)
        {
            if (pawn?.timetable == null || DateNightDefOf.DateNight_Lovin == null)
            {
                return false;
            }
            return pawn.timetable.CurrentAssignment == DateNightDefOf.DateNight_Lovin;
        }

        public static bool ShouldBoostLovinChance(Pawn pawn, Pawn partner)
        {
            return IsLovinSchedule(pawn) || IsLovinSchedule(partner);
        }

        public static Job TryMakeBedJob(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Downed || pawn.Drafted)
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

            Building_Bed target = GetRendezvousBed(pawn);
            if (target == null)
            {
                return null;
            }
            if (pawn.CurrentBed() == target)
            {
                return null;
            }

            return JobMaker.MakeJob(JobDefOf.LayDown, target);
        }

        public static void TickScheduledPawn(Pawn pawn)
        {
            if (!IsLovinSchedule(pawn) || pawn.Map == null || pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                return;
            }
            if (pawn.jobs == null)
            {
                return;
            }
            if (pawn.CurJobDef == JobDefOf.Lovin)
            {
                return;
            }

            // Needs first: never yank someone off food / chem / etc.
            if (ShouldSatisfyNeedsBeforeBed(pawn) || IsDoingNeedJob(pawn))
            {
                return;
            }

            Building_Bed target = GetRendezvousBed(pawn);
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

            // Only the lower-ID partner initiates, so we don't double-StartJob.
            if (inTarget && target.SleepingSlotsCount > 1)
            {
                Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
                if (partner != null && partner.CurrentBed() == target)
                {
                    TryStartLovinNow(pawn);
                }
            }
        }

        public static bool TryStartLovinNow(Pawn pawn)
        {
            if (pawn?.jobs == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.Lovin)
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

            Pawn partner = LovePartnerRelationUtility.GetPartnerInMyBed(pawn);
            if (partner == null)
            {
                // Fallback: love partner physically in the same bed occupants list.
                partner = FindLovePartnerOccupying(pawn, bed);
            }
            if (partner == null || !partner.health.capacities.CanBeAwake || partner.Dead || partner.Downed)
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
            if (DateNightMod.Settings != null && DateNightMod.Settings.pregnancySafeCooldown)
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

            Job lovin = JobMaker.MakeJob(JobDefOf.Lovin, partner, bed);
            lovin.ignoreForbidden = true;
            pawn.jobs.StartJob(lovin, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            return pawn.CurJobDef == JobDefOf.Lovin;
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

            Need_Rest rest = pawn.needs?.rest;
            if (rest != null && rest.CurCategory >= RestCategory.Exhausted)
            {
                return true;
            }

            return false;
        }

        public static bool IsDoingNeedJob(Pawn pawn)
        {
            JobDef def = pawn?.CurJobDef;
            if (def == null)
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
            if (pawn?.mindState != null
                && (DateNightMod.Settings == null || !DateNightMod.Settings.pregnancySafeCooldown))
            {
                pawn.mindState.canLovinTick = 0;
            }

            // Drop cached rendezvous so a new window can pick a sensible bed once.
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner != null)
            {
                RendezvousBedIds.Remove(CoupleKey(pawn, partner));
            }
        }

        public static void NotifyLeftLovinSchedule(Pawn pawn)
        {
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner != null && !IsLovinSchedule(partner))
            {
                RendezvousBedIds.Remove(CoupleKey(pawn, partner));
            }
        }

        /// <summary>
        /// One stable double bed for the couple. Does not claim ownership (LayDown toil claims).
        /// </summary>
        public static Building_Bed GetRendezvousBed(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner == null)
            {
                return DoubleBedOrNull(pawn.ownership?.OwnedBed) ?? RestUtility.FindBedFor(pawn);
            }

            long key = CoupleKey(pawn, partner);
            if (RendezvousBedIds.TryGetValue(key, out int bedId))
            {
                Building_Bed cached = FindBedById(pawn.Map, bedId);
                if (IsUsableDouble(cached, pawn, partner))
                {
                    return cached;
                }
                RendezvousBedIds.Remove(key);
            }

            Building_Bed chosen = ResolveNewRendezvousBed(pawn, partner);
            if (chosen != null)
            {
                RendezvousBedIds[key] = chosen.thingIDNumber;
            }
            return chosen;
        }

        private static Building_Bed ResolveNewRendezvousBed(Pawn pawn, Pawn partner)
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
            Building_Bed owned = DoubleBedOrNull(pawn.ownership?.OwnedBed);
            if (IsUsableDouble(owned, pawn, partner))
            {
                return owned;
            }
            Building_Bed partnerOwned = DoubleBedOrNull(partner.ownership?.OwnedBed);
            if (IsUsableDouble(partnerOwned, pawn, partner))
            {
                return partnerOwned;
            }

            return FindNearestFreeDouble(pawn, partner);
        }

        private static Building_Bed FindNearestFreeDouble(Pawn pawn, Pawn partner)
        {
            Building_Bed best = null;
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

        private static bool IsUsableDouble(Building_Bed bed, Pawn a, Pawn b)
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

        private static Building_Bed DoubleBedOrNull(Building_Bed bed)
        {
            return bed != null && bed.SleepingSlotsCount > 1 ? bed : null;
        }

        private static Building_Bed FindBedById(Map map, int thingId)
        {
            foreach (Building_Bed bed in map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
            {
                if (bed.thingIDNumber == thingId)
                {
                    return bed;
                }
            }
            return null;
        }

        private static Pawn FindLovePartnerOccupying(Pawn pawn, Building_Bed bed)
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

        private static long CoupleKey(Pawn a, Pawn b)
        {
            int x = a.thingIDNumber;
            int y = b.thingIDNumber;
            if (x > y)
            {
                int tmp = x;
                x = y;
                y = tmp;
            }
            return ((long)x << 32) | (uint)y;
        }
    }
}
