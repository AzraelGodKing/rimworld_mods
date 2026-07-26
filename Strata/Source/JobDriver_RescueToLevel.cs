using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // TargetA = patient, TargetB = first stair/portal, TargetC = destination bed
    // (may be on another map). Carry through the portal; PortalRelayChain
    // multi-hops and finishes with vanilla Rescue into the bed.
    public class JobDriver_RescueToLevel : JobDriver
    {
        private Pawn Patient => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        private Building_Bed Bed => job.GetTarget(TargetIndex.C).Thing as Building_Bed;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => !HealthAIUtility.WantsToBeRescued(Patient)
                && pawn.carryTracker.CarriedThing != Patient);
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            this.FailOn(() => Bed == null || !Bed.Spawned || Bed.Destroyed || Bed.ForPrisoners);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A)
                .FailOn(() => !pawn.CanReserve(Patient));

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOn(() => pawn.carryTracker.CarriedThing != Patient);

            Toil enter = ToilMaker.MakeToil("EnterStairsWithPatient");
            enter.initAction = delegate
            {
                if (Portal == null || !Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }

                if (pawn.carryTracker.CarriedThing != Patient || Bed?.Map == null)
                {
                    return;
                }

                PortalRelayChain.Mark(
                    pawn,
                    Bed.Map,
                    RelayPurpose.Rescue,
                    preferredBed: Bed);

                Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                pawn.jobs.StartJob(
                    enterJob,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            enter.AddFailCondition(() => pawn.carryTracker.CarriedThing != Patient);
            yield return enter;
        }
    }
}
