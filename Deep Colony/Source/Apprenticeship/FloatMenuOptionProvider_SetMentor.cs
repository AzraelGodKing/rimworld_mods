using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    public class FloatMenuOptionProvider_SetMentor : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Pawn targetPawn, FloatMenuContext context)
        {
            if (!targetPawn.IsColonistPlayerControlled) yield break;

            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || actor == targetPawn) yield break;
            if (!actor.IsColonistPlayerControlled) yield break;

            var targetComp = targetPawn.TryGetComp<Comp_DeepColony>();
            if (targetComp == null) yield break;
            if (actor.skills == null) yield break;

            if (targetComp.mentor != actor)
            {
                string reason;
                bool ok = MentorshipUtility.CanMentor(actor, targetPawn, out reason);
                string label = "DC_BecomeMentor".Translate(actor.LabelShort.Named("PAWN"),
                    targetPawn.LabelShort.Named("APPRENTICE"));
                if (!ok && !reason.NullOrEmpty())
                    label = label + " (" + reason + ")";
                yield return new FloatMenuOption(
                    label,
                    ok ? () => MentorshipUtility.SetMentorRelation(actor, targetPawn) : null)
                {
                    Disabled = !ok
                };
            }
            else
            {
                yield return new FloatMenuOption(
                    "DC_RemoveMentor".Translate(actor.LabelShort.Named("PAWN"),
                        targetPawn.LabelShort.Named("APPRENTICE")),
                    () => MentorshipUtility.ClearMentorRelation(actor, targetPawn));
            }

            // Player-ordered counseling when target has trauma.
            if (TraumaUtility.HasAnyTrauma(targetPawn)
                && !actor.WorkTagIsDisabled(WorkTags.Social)
                && actor.CanReach(targetPawn, PathEndMode.Touch, Danger.Deadly)
                && actor.CanReserve(targetPawn))
            {
                yield return new FloatMenuOption(
                    "DC_CounselFloat".Translate(targetPawn.LabelShort.Named("PAWN")),
                    () =>
                    {
                        Job job = JobMaker.MakeJob(DC_DefOf.DC_Job_CounselTrauma, targetPawn);
                        actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
            }
        }
    }
}
