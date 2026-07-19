using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // TargetA = infant, TargetB = first stair/portal, TargetC = assigned crib
    // (may be on another map). Carry through the portal; PortalRelayChain
    // multi-hops and tucks the baby into the crib on arrival.
    public class JobDriver_BringBabyToLevel : JobDriver
    {
        private Pawn Baby => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        private Building_Bed Crib => job.GetTarget(TargetIndex.C).Thing as Building_Bed;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => !ModsConfig.BiotechActive);
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            this.FailOn(() => !ChildcareUtility.CanSuckle(Baby, out _));
            this.FailOn(() => Crib == null || !Crib.Spawned || Crib.Destroyed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A)
                .FailOn(() => !pawn.CanReserve(Baby));

            Toil carry = ToilMaker.MakeToil("CarryBaby");
            carry.initAction = delegate
            {
                if (Baby.Spawned && Baby.Map == pawn.Map)
                {
                    pawn.carryTracker.TryStartCarry(Baby);
                }
            };
            carry.defaultCompleteMode = ToilCompleteMode.Instant;
            carry.AddFailCondition(() => pawn.carryTracker.CarriedThing != Baby);
            yield return carry;

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOn(() => pawn.carryTracker.CarriedThing != Baby);

            Toil enter = ToilMaker.MakeToil("EnterStairsWithBaby");
            enter.initAction = delegate
            {
                if (Portal == null || !Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }

                if (pawn.carryTracker.CarriedThing != Baby || Crib?.Map == null)
                {
                    return;
                }

                PortalRelayChain.Mark(
                    pawn,
                    Crib.Map,
                    RelayPurpose.Childcare,
                    preferredBed: Crib);

                Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                pawn.jobs.StartJob(
                    enterJob,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            enter.AddFailCondition(() => pawn.carryTracker.CarriedThing != Baby);
            yield return enter;
        }
    }
}
