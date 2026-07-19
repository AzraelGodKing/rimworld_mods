using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // TargetA = prisoner or capturable pawn, TargetB = first stair/portal,
    // TargetC = prison bed (may be on another map). Carry through the portal;
    // PortalRelayChain multi-hops and finishes with Capture / escort-to-bed.
    public class JobDriver_BringPrisonerToLevel : JobDriver
    {
        private Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        private Building_Bed Bed => job.GetTarget(TargetIndex.C).Thing as Building_Bed;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            // CanBeCaptured can flip once carried — only require it before pickup.
            this.FailOn(() => !Prisoner.IsPrisonerOfColony
                && !Prisoner.CanBeCaptured()
                && pawn.carryTracker.CarriedThing != Prisoner);
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            this.FailOn(() => Bed == null || !Bed.Spawned || Bed.Destroyed || !Bed.ForPrisoners);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A)
                .FailOn(() => !pawn.CanReserve(Prisoner));

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOn(() => pawn.carryTracker.CarriedThing != Prisoner);

            Toil enter = ToilMaker.MakeToil("EnterStairsWithPrisoner");
            enter.initAction = delegate
            {
                if (Portal == null || !Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }

                if (pawn.carryTracker.CarriedThing != Prisoner || Bed?.Map == null)
                {
                    return;
                }

                // Mark BEFORE EnterPortal — vanilla drops carried pawns on enter
                // unless Patch_EnterPortalKeepCarriedPawn sees this intent.
                PortalRelayChain.Mark(
                    pawn,
                    Bed.Map,
                    RelayPurpose.Warden,
                    preferredBed: Bed);

                Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                pawn.jobs.StartJob(
                    enterJob,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            enter.AddFailCondition(() => pawn.carryTracker.CarriedThing != Prisoner);
            yield return enter;
        }
    }
}
