using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // TargetA = portal, TargetB = prisoner or animal. The warden walks to the
    // prisoner wherever they are held, leads them to the stairwell, and both
    // enter — no prisoner beds near the landing required.
    public class JobDriver_StrataEscortToPortal : JobDriver
    {
        private const float PortalEnterDistWarden = 2.5f;

        private const float PortalEnterDistEscortee = 3.5f;

        private const float EscortLeashDist = 12f;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.A).Thing;

        private Pawn Escortee => (Pawn)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Escortee, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Portal, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            this.FailOn(() => !Escortee.Spawned || Escortee.Map != pawn.Map);

            if (Escortee.IsPrisonerOfColony && (Escortee.Downed || Escortee.CarriedBy != null))
            {
                foreach (Toil toil in MakeCarryToPortal())
                {
                    yield return toil;
                }
                yield break;
            }

            if (Escortee.IsPrisonerOfColony)
            {
                foreach (Toil toil in MakePrisonerEscortToPortal())
                {
                    yield return toil;
                }
                yield break;
            }

            foreach (Toil toil in MakeAnimalFollowToPortal())
            {
                yield return toil;
            }
        }

        private IEnumerable<Toil> MakeCarryToPortal()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            Toil carry = ToilMaker.MakeToil("CarryEscortee");
            carry.initAction = delegate
            {
                if (Escortee.Spawned && Escortee.Map == pawn.Map)
                {
                    pawn.carryTracker.TryStartCarry(Escortee);
                }
            };
            carry.defaultCompleteMode = ToilCompleteMode.Instant;
            carry.AddFailCondition(() => pawn.carryTracker.CarriedThing != Escortee);
            yield return carry;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return EnterPortalToil();
        }

        private IEnumerable<Toil> MakePrisonerEscortToPortal()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            Toil march = ToilMaker.MakeToil("LeadPrisonerToPortal");
            march.initAction = SchedulePortalApproach;
            march.tickAction = delegate
            {
                if (Find.TickManager.TicksGame % 45 == 0)
                {
                    SchedulePortalApproach();
                }
            };
            march.defaultCompleteMode = ToilCompleteMode.Never;
            march.AddEndCondition(() => ReadyToEnterPortal() ? JobCondition.Succeeded : JobCondition.Ongoing);
            yield return march;
            yield return EnterPortalTogetherToil();
        }

        private IEnumerable<Toil> MakeAnimalFollowToPortal()
        {
            Toil follow = ToilMaker.MakeToil("StartAnimalFollow");
            follow.initAction = delegate
            {
                Job followJob = JobMaker.MakeJob(JobDefOf.Follow, pawn);
                followJob.expiryInterval = 6000;
                Escortee.jobs.StartJob(followJob, JobCondition.InterruptForced);
            };
            follow.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return follow;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return EnterPortalTogetherToil();
        }

        private void SchedulePortalApproach()
        {
            if (!Portal.Spawned || !Escortee.Spawned || Escortee.Map != pawn.Map)
            {
                return;
            }
            TryOrderGotoPortal(Escortee);
            if (Escortee.Position.InHorDistOf(pawn.Position, EscortLeashDist)
                || pawn.Position.InHorDistOf(Portal.Position, PortalEnterDistWarden + 1f))
            {
                TryOrderGotoPortal(pawn);
            }
            else if (pawn.jobs.curJob == job)
            {
                pawn.pather.StopDead();
            }
        }

        private bool ReadyToEnterPortal()
        {
            return Portal.Spawned && Escortee.Spawned && pawn.Spawned
                && Escortee.Map == pawn.Map && pawn.Map == Portal.Map
                && pawn.Position.InHorDistOf(Portal.Position, PortalEnterDistWarden)
                && Escortee.Position.InHorDistOf(Portal.Position, PortalEnterDistEscortee)
                && Escortee.CanReach(Portal, PathEndMode.Touch, Danger.Some);
        }

        private void TryOrderGotoPortal(Pawn traveler)
        {
            if (traveler == null || !traveler.Spawned || traveler.CurJobDef == JobDefOf.EnterPortal)
            {
                return;
            }
            if (traveler.Position.InHorDistOf(Portal.Position, PortalEnterDistEscortee))
            {
                return;
            }
            Job gotoPortal = JobMaker.MakeJob(JobDefOf.Goto, Portal, Portal.Position);
            traveler.jobs.StartJob(gotoPortal, JobCondition.InterruptForced);
        }

        private Toil EnterPortalToil()
        {
            Toil enter = ToilMaker.MakeToil("EnterPortalCarrying");
            enter.initAction = delegate
            {
                if (Portal.Spawned && Portal.IsEnterable(out _))
                {
                    pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, Portal), JobCondition.InterruptForced);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            return enter;
        }

        private Toil EnterPortalTogetherToil()
        {
            Toil enter = ToilMaker.MakeToil("EnterPortalTogether");
            enter.initAction = delegate
            {
                if (!Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }
                Escortee.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, Portal), JobCondition.InterruptForced);
                pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, Portal), JobCondition.InterruptForced);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            return enter;
        }
    }
}
