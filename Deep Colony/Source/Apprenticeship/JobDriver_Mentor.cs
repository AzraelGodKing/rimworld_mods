using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>
    /// Mentor stands with their apprentice and teaches for about an in-game hour.
    /// Active sessions raise the apprenticeship XP multiplier in Patch_SkillRecord_Learn.
    /// </summary>
    public class JobDriver_Mentor : JobDriver
    {
        private const int TeachDurationTicks = 4000;
        private const int SocialXpInterval = 250;

        private Pawn Apprentice => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Apprentice == null ||
                Apprentice.TryGetComp<Comp_DeepColony>()?.mentor != pawn);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var teach = new Toil
            {
                tickAction = () =>
                {
                    pawn.rotationTracker.FaceTarget(Apprentice);
                    ActiveMentoringSession.SetActive(pawn, Apprentice);

                    // Interval XP only — per-tick Learn was granting tens of thousands
                    // of Social XP per session.
                    if (pawn.IsHashIntervalTick(SocialXpInterval))
                    {
                        pawn.skills?.Learn(SkillDefOf.Social, 12f);
                        Apprentice.skills?.Learn(SkillDefOf.Social, 6f);
                    }
                },
                defaultDuration = TeachDurationTicks,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            teach.AddFailCondition(() =>
                Apprentice == null ||
                !pawn.CanReach(Apprentice, PathEndMode.Touch, Danger.Deadly));
            teach.AddFinishAction(() => ActiveMentoringSession.Clear(pawn, Apprentice));
            yield return teach;

            yield return Toils_General.Do(() =>
            {
                if (Apprentice is { Spawned: true, Map: not null })
                {
                    MoteMaker.ThrowText(Apprentice.DrawPos, Apprentice.Map,
                        "DC_MentoringComplete".Translate(), 4f);
                }
            });
        }
    }

    /// <summary>
    /// Tracks active mentor→apprentice teaching pairs (supports multiple concurrent sessions).
    /// </summary>
    public static class ActiveMentoringSession
    {
        // apprentice thingID → mentor thingID
        private static readonly Dictionary<int, int> activeByApprentice = new Dictionary<int, int>();

        public static void ResetSession()
        {
            activeByApprentice.Clear();
        }

        public static void SetActive(Pawn mentor, Pawn apprentice)
        {
            if (mentor == null || apprentice == null) return;
            activeByApprentice[apprentice.thingIDNumber] = mentor.thingIDNumber;
        }

        public static void Clear(Pawn mentor, Pawn apprentice = null)
        {
            if (apprentice != null)
            {
                if (activeByApprentice.TryGetValue(apprentice.thingIDNumber, out int mid)
                    && (mentor == null || mid == mentor.thingIDNumber))
                {
                    activeByApprentice.Remove(apprentice.thingIDNumber);
                }
                return;
            }

            if (mentor == null) return;
            var remove = new List<int>();
            foreach (var kv in activeByApprentice)
            {
                if (kv.Value == mentor.thingIDNumber) remove.Add(kv.Key);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                activeByApprentice.Remove(remove[i]);
            }
        }

        public static bool IsBeingMentored(Pawn apprentice)
        {
            return apprentice != null
                && activeByApprentice.ContainsKey(apprentice.thingIDNumber);
        }
    }
}
