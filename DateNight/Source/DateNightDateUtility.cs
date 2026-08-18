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

        public static void NotifyDateFinished(Pawn pawn, Pawn partner)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || partner == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }

            ThoughtDef def = DateNightDefOf.DateNight_HadADate;
            if (def != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(def, partner);
            }

            DateNightWindows.NotifyDateSuccess(pawn, partner);
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
