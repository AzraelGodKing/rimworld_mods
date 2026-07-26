using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla RestUtility.FindBedFor / WorkGiver_RescueDowned only see the
    // patient's current map, so a downed colonist on a dig with no beds cannot
    // be rescued upstairs. Scan linked floors for a usable non-prisoner bed.
    public static class RescueBedAcrossLevels
    {
        public static bool TryFind(
            Pawn rescuer,
            Pawn patient,
            out Building_Bed bed,
            out MapPortal portal,
            bool ignoreReservations = false)
        {
            bed = null;
            portal = null;
            if (rescuer?.Map == null || patient == null || !LevelGraph.AnyLinkFrom(rescuer.Map))
            {
                return false;
            }

            // Local bed available — leave that to vanilla Rescue.
            if (HasLocalBed(rescuer, patient, ignoreReservations))
            {
                return false;
            }

            Building_Bed best = null;
            Map bestMap = null;
            float bestScore = float.MaxValue;
            bool preferMedical = HealthAIUtility.ShouldSeekMedicalRest(patient)
                || HealthAIUtility.ShouldSeekMedicalRestUrgent(patient);

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(rescuer.Map))
            {
                if (!link.arrivalCell.IsValid || !link.arrivalCell.InBounds(link.map))
                {
                    continue;
                }

                List<Thing> beds = link.map.listerThings.ThingsInGroup(ThingRequestGroup.Bed);
                for (int i = 0; i < beds.Count; i++)
                {
                    if (beds[i] is not Building_Bed candidate)
                    {
                        continue;
                    }

                    if (!IsUsableRescueBed(candidate, patient, link.arrivalCell, ignoreReservations))
                    {
                        continue;
                    }

                    float score = link.arrivalCell.DistanceToSquared(candidate.Position);
                    if (preferMedical)
                    {
                        score += candidate.Medical ? 0f : 100000f;
                    }
                    else if (candidate.Medical)
                    {
                        // Prefer ordinary beds when not seeking medical rest.
                        score += 50000f;
                    }

                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                        bestMap = link.map;
                    }
                }
            }

            if (best == null || bestMap == null)
            {
                return false;
            }

            MapPortal step = LevelGraph.BestFirstStep(
                rescuer.Map,
                bestMap,
                rescuer.Position,
                rescuer);
            if (step == null || !step.Spawned || !step.IsEnterable(out _)
                || !rescuer.CanReach(step, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            bed = best;
            portal = step;
            return true;
        }

        public static bool HasAny(Pawn rescuer, Pawn patient)
        {
            return TryFind(rescuer, patient, out _, out _, ignoreReservations: true)
                || TryFind(rescuer, patient, out _, out _, ignoreReservations: false);
        }

        private static bool HasLocalBed(Pawn rescuer, Pawn patient, bool ignoreReservations)
        {
            Building_Bed local = RestUtility.FindBedFor(
                patient,
                rescuer,
                checkSocialProperness: false,
                ignoreOtherReservations: ignoreReservations);
            if (local != null)
            {
                return true;
            }

            local = RestUtility.FindPatientBedFor(patient);
            return local != null && local.Map == patient.Map;
        }

        private static bool IsUsableRescueBed(
            Building_Bed bed,
            Pawn patient,
            IntVec3 arrivalCell,
            bool ignoreReservations)
        {
            if (bed == null || !bed.Spawned || bed.ForPrisoners || bed.IsBurning())
            {
                return false;
            }

            if (!bed.def.building.bed_humanlike && patient.RaceProps.Humanlike)
            {
                return false;
            }

            if (!RestUtility.CanUseBedEver(patient, bed.def))
            {
                return false;
            }

            if (!bed.AnyUnoccupiedSleepingSlot && !bed.IsOwner(patient, out _))
            {
                return false;
            }

            if (bed.Medical)
            {
                if (!HealthAIUtility.ShouldSeekMedicalRest(patient)
                    && !HealthAIUtility.ShouldSeekMedicalRestUrgent(patient))
                {
                    return false;
                }
            }
            else if (bed.OwnersForReading.Count > 0 && !bed.IsOwner(patient, out _)
                && !RestUtility.BedOwnerWillShare(bed, patient, null))
            {
                return false;
            }

            float temp = GenTemperature.GetTemperatureForCell(bed.Position, bed.Map);
            if (!patient.SafeTemperatureRange().Includes(temp))
            {
                return false;
            }

            if (!ignoreReservations
                && bed.Map.reservationManager.IsReservedByAnyoneOf(bed, Faction.OfPlayer))
            {
                return false;
            }

            if (!bed.Map.reachability.CanReach(
                    arrivalCell,
                    bed,
                    PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.PassDoors)))
            {
                return false;
            }

            return true;
        }
    }
}
