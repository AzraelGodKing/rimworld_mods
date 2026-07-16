using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Ritual animals still need a handler escort for ceremonies. Prisoners and
    // slaves climb shafts alone during prison breaks / escapes
    // (EscapePortalUtility); escorts remain only for cross-level rituals.
    public static class RitualEscortUtility
    {
        public static bool NeedsEscort(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return false;
            }
            if (pawn.IsPrisonerOfColony)
            {
                return true;
            }
            return pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer;
        }

        public static Pawn FindEscort(Pawn escortee, Map map, RitualRoleAssignments assignments)
        {
            if (map == null || escortee == null)
            {
                return null;
            }
            Pawn preferred = PreferredEscort(escortee, assignments);
            if (preferred != null && preferred.Spawned && preferred.Map == map && CanEscort(preferred, escortee))
            {
                return preferred;
            }
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (CanEscort(pawn, escortee))
                {
                    return pawn;
                }
            }
            return null;
        }

        private static Pawn PreferredEscort(Pawn escortee, RitualRoleAssignments assignments)
        {
            if (escortee.IsPrisonerOfColony)
            {
                return null; // any warden on the map is fine
            }
            if (!escortee.RaceProps.Animal)
            {
                return null;
            }
            Pawn master = escortee.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond);
            if (master != null && IsParticipant(master, assignments))
            {
                return master;
            }
            foreach (Pawn pawn in assignments?.Participants ?? Enumerable.Empty<Pawn>())
            {
                if (pawn != null && pawn.RaceProps.Humanlike && pawn.workSettings?.WorkIsActive(WorkTypeDefOf.Handling) == true)
                {
                    return pawn;
                }
            }
            return null;
        }

        private static bool IsParticipant(Pawn pawn, RitualRoleAssignments assignments)
        {
            if (pawn == null || assignments == null)
            {
                return false;
            }
            foreach (Pawn participant in assignments.Participants)
            {
                if (participant == pawn)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CanEscort(Pawn escort, Pawn escortee)
        {
            if (escort == null || escortee == null || escort == escortee)
            {
                return false;
            }
            if (!escort.Spawned || escort.Dead || escort.Downed || escort.Drafted)
            {
                return false;
            }
            if (!escort.IsFreeColonist || escort.InMentalState)
            {
                return false;
            }
            if (escortee.IsPrisonerOfColony)
            {
                return escort.workSettings?.WorkIsActive(WorkTypeDefOf.Warden) == true;
            }
            if (escortee.RaceProps.Animal)
            {
                return escort.workSettings?.WorkIsActive(WorkTypeDefOf.Handling) == true
                    || escort == escortee.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond);
            }
            return false;
        }

        public static bool TryStartEscortHop(Pawn escort, Pawn escortee, MapPortal portal)
        {
            if (escort == null || escortee == null || portal == null || !portal.Spawned)
            {
                return false;
            }
            if (escort.CurJobDef == StrataDefOf.Strata_EscortToPortal
                && escort.CurJob.GetTarget(TargetIndex.B).Thing == escortee)
            {
                return true;
            }
            if (!escort.CanReach(portal, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_EscortToPortal, portal, escortee);
            return escort.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
