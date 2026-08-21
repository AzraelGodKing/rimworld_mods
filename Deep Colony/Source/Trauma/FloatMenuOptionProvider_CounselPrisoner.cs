using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    public class FloatMenuOptionProvider_CounselPrisoner : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Pawn targetPawn, FloatMenuContext context)
        {
            if (!DeepColonySettings.Get.enableTrauma) yield break;
            if (!DeepColonySettings.Get.enablePrisonerCounsel) yield break;
            if (targetPawn == null || !targetPawn.IsPrisonerOfColony) yield break;
            if (targetPawn.Dead || !targetPawn.Spawned || targetPawn.Downed) yield break;

            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || actor == targetPawn) yield break;
            if (!actor.IsColonistPlayerControlled) yield break;
            if (actor.WorkTagIsDisabled(WorkTags.Social)) yield break;
            if (!actor.CanReach(targetPawn, PathEndMode.Touch, Danger.Deadly)) yield break;
            if (!actor.CanReserve(targetPawn)) yield break;

            yield return new FloatMenuOption(
                "DC_CounselPrisonerFloat".Translate(targetPawn.LabelShort.Named("PAWN")),
                () =>
                {
                    Job job = JobMaker.MakeJob(DC_DefOf.DC_Job_CounselPrisoner, targetPawn);
                    actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }
}
