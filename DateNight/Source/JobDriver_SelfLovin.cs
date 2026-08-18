using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    public class JobDriver_SelfLovin : JobDriver
    {
        private const TargetIndex BedInd = TargetIndex.A;
        private const int TicksBetweenHeartMotes = 100;

        private int ticksLeft;

        private Building_Bed Bed => job.GetTarget(BedInd).Thing as Building_Bed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_Bed bed = Bed;
            if (bed == null)
            {
                return false;
            }
            int slots = System.Math.Max(1, bed.SleepingSlotsCount);
            return pawn.Reserve(bed, job, slots, 0, null, errorOnFailed);
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(BedInd);
            this.FailOn(() => !DateNightUtility.CanSelfLovin(pawn));
            this.FailOn(() => Bed != null && Bed.Medical);
            this.FailOn(() => LovePartnerRelationUtility.GetPartnerInMyBed(pawn) != null);
            this.KeepLyingDown(BedInd);

            yield return Toils_Bed.ClaimBedIfNonMedical(BedInd);
            yield return Toils_Bed.GotoBed(BedInd);

            Toil wait = Toils_LayDown.LayDown(
                BedInd,
                hasBed: true,
                lookForOtherJobs: false,
                canSleep: false,
                gainRestAndHealth: true);
            wait.socialMode = RandomSocialMode.Off;
            wait.AddPreTickAction(() =>
            {
                if (ticksLeft <= 0)
                {
                    ticksLeft = Rand.RangeInclusive(2000, 3500);
                }

                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                    return;
                }

                if (pawn.IsHashIntervalTick(TicksBetweenHeartMotes) && pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            });
            wait.AddFinishAction(() =>
            {
                if (ticksLeft <= 0)
                {
                    DateNightUtility.NotifySelfLovinFinished(pawn);
                }
            });
            yield return wait;
        }
    }
}
