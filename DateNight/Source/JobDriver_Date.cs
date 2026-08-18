using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    public class JobDriver_Date : JobDriver
    {
        private const TargetIndex PartnerInd = TargetIndex.A;
        private const TargetIndex SpotInd = TargetIndex.B;
        private const int TicksBetweenHeartMotes = 180;
        private const int TicksBetweenChat = 320;

        private int ticksLeft;

        private Pawn Partner => job.GetTarget(PartnerInd).Pawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(PartnerInd);
            this.FailOn(() => pawn.Drafted);
            this.FailOn(() => !DateNightDateUtility.CanDate(pawn, Partner, force: true));

            Toil go = ToilMaker.MakeToil("DateNightGoSpot");
            go.initAction = () =>
            {
                LocalTargetInfo spot = job.GetTarget(SpotInd);
                if (!spot.IsValid)
                {
                    spot = Partner;
                    job.SetTarget(SpotInd, spot);
                }
                pawn.pather.StartPath(spot, PathEndMode.OnCell);
            };
            go.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return go;

            Toil hang = ToilMaker.MakeToil("DateNightDateHang");
            hang.socialMode = RandomSocialMode.SuperActive;
            hang.defaultCompleteMode = ToilCompleteMode.Never;
            hang.handlingFacing = true;
            hang.initAction = () =>
            {
                if (ticksLeft <= 0)
                {
                    ticksLeft = Rand.RangeInclusive(2000, 4000);
                }
                pawn.pather?.StopDead();
            };
            hang.tickAction = () =>
            {
                Pawn partner = Partner;
                if (partner == null || partner.Dead || !partner.Spawned)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (pawn.Position.DistanceToSquared(partner.Position) > 64)
                {
                    pawn.pather.StartPath(partner, PathEndMode.Touch);
                    return;
                }

                pawn.rotationTracker.FaceCell(partner.Position);

                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                    return;
                }

                if (pawn.IsHashIntervalTick(TicksBetweenChat) && pawn.interactions != null)
                {
                    pawn.interactions.TryInteractWith(partner, InteractionDefOf.Chitchat);
                }
                if (pawn.IsHashIntervalTick(TicksBetweenHeartMotes) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            };
            hang.AddFinishAction(() =>
            {
                if (ticksLeft <= 0)
                {
                    DateNightDateUtility.NotifyDateFinished(pawn, Partner);
                }
            });
            yield return hang;
        }
    }
}
