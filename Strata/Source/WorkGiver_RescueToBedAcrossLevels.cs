using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Carry a downed/wounded pawn through stairs to a bed on another linked
    // floor when this level has none. Vanilla WorkGiver_RescueDowned /
    // FindBedFor are same-map only.
    public class WorkGiver_RescueToBedAcrossLevels : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                yield break;
            }

            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (other != pawn && HealthAIUtility.WantsToBeRescued(other))
                {
                    yield return other;
                }
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

            Job job = JobMaker.MakeJob(StrataDefOf.Strata_RescueToLevel, t, portal, bed);
            job.count = 1;
            return job;
        }

        private static bool TryFindJob(
            Pawn rescuer,
            Pawn patient,
            bool forced,
            out MapPortal portal,
            out Building_Bed bed)
        {
            portal = null;
            bed = null;
            if (rescuer == null || patient == null)
            {
                return false;
            }

            if (!HealthAIUtility.CanRescueNow(rescuer, patient, forced))
            {
                return false;
            }

            if (patient.CurrentBed() != null)
            {
                return false;
            }

            if (!RescueBedAcrossLevels.TryFind(rescuer, patient, out bed, out portal)
                && !RescueBedAcrossLevels.TryFind(
                    rescuer,
                    patient,
                    out bed,
                    out portal,
                    ignoreReservations: true))
            {
                return false;
            }

            return true;
        }
    }
}
