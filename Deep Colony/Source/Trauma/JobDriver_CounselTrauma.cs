using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>
    /// Doctor work: sit with a traumatized colonist and advance their trauma recovery.
    /// </summary>
    public class JobDriver_CounselTrauma : JobDriver
    {
        private const int SessionTicks = 2500;
        private const int ProgressInterval = 500;

        private Pawn Patient => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Patient == null || !TraumaUtility.HasAnyTrauma(Patient));
            this.FailOn(() => Patient.InMentalState || Patient.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var counsel = new Toil
            {
                tickAction = () =>
                {
                    pawn.rotationTracker.FaceTarget(Patient);
                    if (pawn.IsHashIntervalTick(ProgressInterval))
                    {
                        TraumaUtility.ApplyTherapy(pawn, Patient);
                        pawn.skills?.Learn(SkillDefOf.Social, 8f);
                        Patient.skills?.Learn(SkillDefOf.Social, 3f);
                    }
                },
                defaultDuration = SessionTicks,
                defaultCompleteMode = ToilCompleteMode.Delay,
                socialMode = RandomSocialMode.SuperActive
            };
            counsel.AddFailCondition(() =>
                Patient == null ||
                !pawn.CanReach(Patient, PathEndMode.Touch, Danger.Deadly) ||
                !TraumaUtility.HasAnyTrauma(Patient));
            yield return counsel;

            yield return Toils_General.Do(() =>
            {
                TraumaUtility.ApplyTherapy(pawn, Patient);
                if (Patient is { Spawned: true, Map: not null })
                {
                    MoteMaker.ThrowText(Patient.DrawPos, Patient.Map,
                        "DC_CounselComplete".Translate(), 4f);
                }
            });
        }
    }
}
