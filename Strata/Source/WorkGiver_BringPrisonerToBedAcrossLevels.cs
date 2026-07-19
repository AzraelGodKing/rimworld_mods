using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Carry/escort a prisoner through stairs to their assigned bed on another
    // linked floor. Vanilla WorkGiver_Warden_TakeToBed uses FindBedFor on the
    // current map only, so an OwnedBed upstairs never appears as a job target.
    public class WorkGiver_BringPrisonerToBedAcrossLevels : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.OnCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                yield break;
            }

            List<Pawn> prisoners = pawn.Map.mapPawns.PrisonersOfColonySpawned;
            if (prisoners == null)
            {
                yield break;
            }

            for (int i = 0; i < prisoners.Count; i++)
            {
                yield return prisoners[i];
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryFindJob(pawn, t as Pawn, forced, out _, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!TryFindJob(pawn, t as Pawn, forced, out MapPortal portal, out Building_Bed bed))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(StrataDefOf.Strata_BringPrisonerToLevel, t, portal, bed);
            job.count = 1;
            return job;
        }

        private static bool TryFindJob(
            Pawn warden,
            Pawn prisoner,
            bool forced,
            out MapPortal portal,
            out Building_Bed bed)
        {
            portal = null;
            bed = null;
            if (warden == null || prisoner == null || !prisoner.IsPrisonerOfColony)
            {
                return false;
            }

            if (prisoner.Dead || prisoner.Destroyed || prisoner.InAggroMentalState
                || prisoner.IsForbidden(warden)
                || prisoner.CurrentBed() != null && prisoner.ownership?.OwnedBed == prisoner.CurrentBed())
            {
                return false;
            }

            if (!warden.CanReserveAndReach(
                    prisoner,
                    PathEndMode.OnCell,
                    warden.NormalMaxDanger(),
                    1,
                    -1,
                    null,
                    ignoreOtherReservations: forced))
            {
                return false;
            }

            bed = prisoner.ownership?.OwnedBed;
            if (bed == null || !bed.Spawned || bed.Map == null || bed.Map == prisoner.Map
                || !bed.ForPrisoners)
            {
                if (forced && prisoner.ownership?.OwnedBed == null)
                {
                    JobFailReason.Is("NoPrisonerBedShort".Translate());
                }

                return false;
            }

            if (prisoner.CurrentBed() == bed)
            {
                return false;
            }

            bool bedReachable = false;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(prisoner.Map))
            {
                if (link.map == bed.Map)
                {
                    bedReachable = true;
                    break;
                }
            }

            if (!bedReachable)
            {
                return false;
            }

            MapPortal step = LevelGraph.BestFirstStep(
                prisoner.Map,
                bed.Map,
                warden.Position,
                warden);
            if (step == null || !step.Spawned || !step.IsEnterable(out _)
                || !warden.CanReach(step, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }

            portal = step;
            return true;
        }
    }
}
