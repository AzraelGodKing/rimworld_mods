using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla RestUtility.FindBedFor / CanUseBedNow require sleeper.MapHeld ==
    // bed.Map, so capture/arrest fail when the only prison beds are upstairs or
    // underground. Scan linked floors instead.
    public static class PrisonerBedAcrossLevels
    {
        public static bool TryFind(
            Pawn carrier,
            Pawn captive,
            out Building_Bed bed,
            out MapPortal portal,
            bool ignoreReservations = false)
        {
            bed = null;
            portal = null;
            if (carrier?.Map == null || captive == null || !LevelGraph.AnyLinkFrom(carrier.Map))
            {
                return false;
            }

            Building_Bed best = null;
            Map bestMap = null;
            float bestScore = float.MaxValue;
            bool preferMedical = HealthAIUtility.ShouldSeekMedicalRest(captive);

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(carrier.Map))
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

                    if (!IsUsablePrisonBed(candidate, captive, link.arrivalCell, ignoreReservations))
                    {
                        continue;
                    }

                    // Prefer medical when the captive needs it; then nearer landing.
                    float score = link.arrivalCell.DistanceToSquared(candidate.Position);
                    if (preferMedical)
                    {
                        score += candidate.Medical ? 0f : 100000f;
                    }
                    else if (candidate.Medical)
                    {
                        score += 100000f;
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
                carrier.Map,
                bestMap,
                carrier.Position,
                carrier);
            if (step == null || !step.Spawned || !step.IsEnterable(out _)
                || !carrier.CanReach(step, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            bed = best;
            portal = step;
            return true;
        }

        public static bool HasAny(Pawn carrier, Pawn captive)
        {
            return TryFind(carrier, captive, out _, out _, ignoreReservations: true)
                || TryFind(carrier, captive, out _, out _, ignoreReservations: false);
        }

        private static bool IsUsablePrisonBed(
            Building_Bed bed,
            Pawn captive,
            IntVec3 arrivalCell,
            bool ignoreReservations)
        {
            if (bed == null || !bed.Spawned || !bed.ForPrisoners || bed.IsBurning())
            {
                return false;
            }

            if (!bed.Position.IsInPrisonCell(bed.Map))
            {
                return false;
            }

            if (!RestUtility.CanUseBedEver(captive, bed.def))
            {
                return false;
            }

            if (!bed.AnyUnoccupiedSleepingSlot && !bed.IsOwner(captive, out _))
            {
                return false;
            }

            if (bed.Medical)
            {
                if (!HealthAIUtility.ShouldSeekMedicalRest(captive))
                {
                    return false;
                }
            }
            else if (!bed.IsOwner(captive, out _)
                && !RestUtility.BedOwnerWillShare(bed, captive, GuestStatus.Prisoner))
            {
                return false;
            }

            float temp = GenTemperature.GetTemperatureForCell(bed.Position, bed.Map);
            if (!captive.SafeTemperatureRange().Includes(temp))
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
