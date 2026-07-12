using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Pick up a thing, carry it to a stairwell, then hand off to the vanilla
    // EnterPortal job. carryThingAfterJob keeps the load in the pawn's hands
    // through the handoff; vanilla drops it at the far landing when the enter
    // job finishes, and normal hauling on that level puts it into storage.
    public class JobDriver_HaulToLevel : JobDriver
    {
        private Thing Item => job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.A);
            // The player can seal the shaft mid-haul; drop the job instead of
            // walking cargo to a door that won't open.
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            Toil enter = ToilMaker.MakeToil("EnterStairs");
            enter.initAction = delegate
            {
                if (Portal.Spawned && Portal.IsEnterable(out _))
                {
                    Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                    pawn.jobs.jobQueue.EnqueueFirst(enterJob, JobTag.Misc);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}
