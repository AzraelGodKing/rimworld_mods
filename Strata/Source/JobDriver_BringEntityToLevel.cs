using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // TargetA = entity, TargetB = first stair/portal, TargetC = holding platform
    // on another map. Do not set CompHoldingPlatformTarget.targetHolder until
    // arrival — vanilla clears it when maps differ.
    public class JobDriver_BringEntityToLevel : JobDriver
    {
        private Thing Entity => job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        private Thing Platform => job.GetTarget(TargetIndex.C).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Entity, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnDestroyedOrNull(TargetIndex.C);
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            this.FailOn(() =>
            {
                CompEntityHolder holder = Platform?.TryGetComp<CompEntityHolder>();
                return holder == null || !holder.Available;
            });

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A)
                .FailOn(() => !pawn.CanReserve(Entity));

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOn(() => pawn.carryTracker.CarriedThing != Entity);

            Toil enter = ToilMaker.MakeToil("EnterStairsWithEntity");
            enter.initAction = delegate
            {
                if (Portal == null || !Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }

                if (pawn.carryTracker.CarriedThing != Entity || Platform?.Map == null)
                {
                    return;
                }

                PortalRelayChain.Mark(
                    pawn,
                    Platform.Map,
                    RelayPurpose.Containment,
                    preferredThing: Platform);

                Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                pawn.jobs.StartJob(
                    enterJob,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            enter.AddFailCondition(() => pawn.carryTracker.CarriedThing != Entity);
            yield return enter;
        }
    }
}
