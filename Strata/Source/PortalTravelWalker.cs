using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Shared portal-hop advance / escort / timeout helpers for cross-level
    // ritual and caravan travel walkers. Callers own traveler lists and scribing.
    internal static class PortalTravelWalker
    {
        internal const int CheckInterval = 60;
        internal const int TimeoutTicks = 20000; // ~8 in-game hours

        internal static bool LordAlive(Map destination, Lord lord)
        {
            return destination != null && lord != null
                && Find.Maps.Contains(destination)
                && destination.lordManager.lords.Contains(lord);
        }

        internal static bool ShouldAbandon(Pawn pawn, Map destination, Lord lord, int started)
        {
            return pawn == null || pawn.Dead || pawn.Destroyed
                || pawn.Drafted
                || Find.TickManager.TicksGame - started > TimeoutTicks
                || !LordAlive(destination, lord);
        }

        internal static void TryAdvance(Pawn pawn, Map destination, IntVec3 preferArrivalNear)
        {
            if (pawn == null || !pawn.Spawned || pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return;
            }
            MapPortal step = LevelGraph.BestFirstStep(
                pawn.Map, destination, pawn.Position, pawn, preferArrivalNear);
            if (step != null && step.Spawned && step.IsEnterable(out _)
                && pawn.CanReach(step, PathEndMode.Touch, Danger.Some))
            {
                Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, step);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        internal static void TryAdvanceEscorted(
            Pawn escortee,
            ref Pawn escort,
            Map destination,
            IntVec3 preferArrivalNear,
            RitualRoleAssignments assignments)
        {
            if (escortee == null || !escortee.Spawned)
            {
                return;
            }
            if (escortee.CurJobDef == JobDefOf.EnterPortal)
            {
                return;
            }
            if (escort == null || !escort.Spawned || escort.Dead || escort.Map != escortee.Map)
            {
                escort = RitualEscortUtility.FindEscort(escortee, escortee.Map, assignments);
            }
            if (escort == null)
            {
                return;
            }
            MapPortal step = LevelGraph.BestFirstStep(
                escortee.Map, destination, escortee.Position, escortee, preferArrivalNear);
            if (step == null || !step.Spawned || !step.IsEnterable(out _))
            {
                return;
            }
            if (TryEnterPortalTogether(escort, escortee, step))
            {
                return;
            }
            RitualEscortUtility.TryStartEscortHop(escort, escortee, step);
        }

        internal static bool TryEnterPortalTogether(Pawn escort, Pawn escortee, MapPortal portal)
        {
            if (escort == null || escortee == null || portal == null || !portal.Spawned)
            {
                return false;
            }
            if (escort.Map != escortee.Map || escort.Map != portal.Map)
            {
                return false;
            }
            if (escort.CurJobDef == JobDefOf.EnterPortal || escortee.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }
            if (!escort.CanReach(portal, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            if (!escort.Position.InHorDistOf(portal.Position, 2.5f)
                || !escortee.Position.InHorDistOf(portal.Position, 3f))
            {
                return false;
            }
            escortee.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, portal), JobCondition.InterruptForced);
            escort.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, portal), JobCondition.InterruptForced);
            return true;
        }
    }
}
