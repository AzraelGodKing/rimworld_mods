#nullable enable
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// The captain's first pick under <c>Nemesis_HuntTarget</c>: when the lord allows kidnap and
    /// the fixation target is down, carry them off the map (AZR-554). Vanilla kidnap takes any
    /// downed colonist; this one only takes the target.
    /// </summary>
    public class JobGiver_NemesisKidnapTarget : ThinkNode_JobGiver
    {
        protected override Job? TryGiveJob(Pawn pawn)
        {
            if (!(pawn.GetLord()?.LordJob is LordJob_NemesisHunt hunt) || !hunt.Kidnap) return null;
            Pawn? target = pawn.mindState?.duty?.focus.Pawn;
            if (target == null || !target.Spawned || target.Map != pawn.Map || !target.Downed || target.Dead)
                return null;
            if (target.ParentHolder is Pawn_CarryTracker) return null;
            if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some)) return null;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 exit, TraverseMode.ByPawn, true)) return null;

            Job job = JobMaker.MakeJob(JobDefOf.Kidnap, target, exit);
            job.count = 1;
            return job;
        }
    }

    /// <summary>
    /// Fight enemies, but lock onto the duty's focus pawn (the fixation target) whenever it is
    /// standing on this map. Falls back to vanilla target picking otherwise.
    /// </summary>
    public class JobGiver_NemesisFightTarget : JobGiver_AIFightEnemies
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Pawn? target = pawn.mindState?.duty?.focus.Pawn;
            if (target != null && target.Spawned && target.Map == pawn.Map && !target.Dead && !target.Downed
                && pawn.mindState != null && pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                pawn.mindState.enemyTarget = target;
            return base.TryGiveJob(pawn);
        }
    }
}
