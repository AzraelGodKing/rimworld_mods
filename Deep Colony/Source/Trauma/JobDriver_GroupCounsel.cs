using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>A07 — counsel every traumatized colonist sharing the patient's room.</summary>
    public class JobDriver_GroupCounsel : JobDriver
    {
        private const int SessionTicks = 3200;
        private const int ProgressInterval = 500;

        private Pawn Focal => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Focal == null || !TraumaUtility.HasAnyTrauma(Focal));
            this.FailOn(() => TraumaUtility.CountTraumatizedInRoom(Focal) < 2);
            this.FailOn(() => Focal.InMentalState || Focal.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var counsel = new Toil
            {
                tickAction = () =>
                {
                    pawn.rotationTracker.FaceTarget(Focal);
                    if (pawn.IsHashIntervalTick(ProgressInterval))
                    {
                        TraumaUtility.ApplyTherapyToRoom(pawn, Focal);
                        pawn.skills?.Learn(SkillDefOf.Social, 10f);
                    }
                },
                defaultDuration = SessionTicks,
                defaultCompleteMode = ToilCompleteMode.Delay,
                socialMode = RandomSocialMode.SuperActive
            };
            counsel.AddFailCondition(() =>
                Focal == null
                || !pawn.CanReach(Focal, PathEndMode.Touch, Danger.Deadly)
                || TraumaUtility.CountTraumatizedInRoom(Focal) < 2);
            yield return counsel;

            yield return Toils_General.Do(() =>
            {
                TraumaUtility.ApplyTherapyToRoom(pawn, Focal);
                Room room = Focal.GetRoom();
                if (room != null && Focal.Map != null)
                {
                    foreach (Pawn p in Focal.Map.mapPawns.FreeColonistsSpawned)
                    {
                        if (p == pawn || p.Dead) continue;
                        if (!TraumaUtility.HasAnyTrauma(p) && p != Focal) continue;
                        if (p.GetRoom() != room) continue;
                        ConfidantUtility.NotifyCounselSession(pawn, p);
                        p.skills?.Learn(SkillDefOf.Social, 2f);
                    }
                }

                if (Focal is { Spawned: true, Map: not null })
                {
                    MoteMaker.ThrowText(Focal.DrawPos, Focal.Map,
                        "DC_GroupCounselComplete".Translate(), 4f);
                }
            });
        }
    }
}
