using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>C13 — optional prisoner counseling (slow recruit). Does not replace Warden chat.</summary>
    public class JobDriver_CounselPrisoner : JobDriver
    {
        private const int SessionTicks = 2800;
        private const int ProgressInterval = 500;
        private const int SessionsToTryRecruit = 5;

        private Pawn Patient => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Patient == null || !Patient.IsPrisonerOfColony);
            this.FailOn(() => Patient.InMentalState || Patient.Downed || Patient.Dead);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var counsel = new Toil
            {
                tickAction = () =>
                {
                    pawn.rotationTracker.FaceTarget(Patient);
                    if (pawn.IsHashIntervalTick(ProgressInterval))
                    {
                        if (TraumaUtility.HasAnyTrauma(Patient))
                            TraumaUtility.ApplyTherapy(pawn, Patient);
                        pawn.skills?.Learn(SkillDefOf.Social, 6f);
                    }
                },
                defaultDuration = SessionTicks,
                defaultCompleteMode = ToilCompleteMode.Delay,
                socialMode = RandomSocialMode.SuperActive
            };
            counsel.AddFailCondition(() =>
                Patient == null || !Patient.IsPrisonerOfColony || Patient.Dead);
            yield return counsel;

            yield return Toils_General.Do(FinishSession);
        }

        private void FinishSession()
        {
            if (Patient == null || pawn == null) return;

            if (TraumaUtility.HasAnyTrauma(Patient))
            {
                TraumaUtility.ApplyTherapy(pawn, Patient);
                ConfidantUtility.NotifyCounselSession(pawn, Patient);
            }

            var guest = Patient.guest;
            if (guest != null && !guest.Recruitable)
            {
                FamilyLoyaltyUtility.TryBreak(pawn, Patient);
                return;
            }

            if (guest != null && guest.resistance > 0f)
            {
                int social = pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
                float drop = 1.5f + social * 0.12f;
                guest.resistance = System.Math.Max(0f, guest.resistance - drop);
            }

            var patientComp = Patient.TryGetComp<Comp_DeepColony>();
            int sessions = patientComp?.IncrementCounselCount(pawn) ?? 0;

            if (sessions >= SessionsToTryRecruit
                && Patient.guest != null
                && Patient.guest.resistance <= 2f
                && (pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0) >= 8)
            {
                Patient.SetFaction(Faction.OfPlayer);
                Messages.Message(
                    "DC_PrisonerCounselRecruit".Translate(
                        Patient.LabelShort.Named("PAWN"),
                        pawn.LabelShort.Named("COUNSELOR")),
                    Patient, MessageTypeDefOf.PositiveEvent, false);
                return;
            }

            if (Patient.Spawned && Patient.Map != null)
            {
                MoteMaker.ThrowText(Patient.DrawPos, Patient.Map,
                    "DC_CounselComplete".Translate(), 4f);
            }
        }
    }
}
