using System.Collections.Generic;
using System.Linq;
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
            if (!DeepColonySettings.Get.enableMentoring
                && !DeepColonySettings.Get.enableTrauma) yield break;

            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || actor == targetPawn) yield break;
            if (!actor.IsColonistPlayerControlled) yield break;

            var targetComp = targetPawn.TryGetComp<Comp_DeepColony>();
            if (targetComp == null) yield break;
            if (actor.skills == null) yield break;

            if (DeepColonySettings.Get.enableMentoring)
            {
                if (targetComp.mentor != actor)
                {
                    var skills = MentorshipUtility.SkillsMentorCanTeach(actor, targetPawn).ToList();
                    bool lineage = MentorshipUtility.IsLineagePair(actor, targetPawn);

                    if (skills.Count == 0)
                    {
                        string reason;
                        MentorshipUtility.CanMentor(actor, targetPawn, out reason);
                        string label = "DC_BecomeMentor".Translate(actor.LabelShort.Named("PAWN"),
                            targetPawn.LabelShort.Named("APPRENTICE"));
                        if (!reason.NullOrEmpty()) label = label + " (" + reason + ")";
                        yield return new FloatMenuOption(label, null) { Disabled = true };
                    }
                    else
                    {
                        // Family first as a hint on the submenu header option
                        foreach (SkillDef skill in skills)
                        {
                            SkillDef captured = skill;
                            string label = "DC_BecomeMentorSkill".Translate(
                                actor.LabelShort.Named("PAWN"),
                                targetPawn.LabelShort.Named("APPRENTICE"),
                                captured.LabelCap.Named("SKILL"));
                            if (lineage)
                                label = "DC_BecomeMentorSkillFamily".Translate(
                                    actor.LabelShort.Named("PAWN"),
                                    targetPawn.LabelShort.Named("APPRENTICE"),
                                    captured.LabelCap.Named("SKILL"));
                            yield return new FloatMenuOption(
                                label,
                                () => MentorshipUtility.SetMentorRelation(actor, targetPawn, captured));
                        }
                    }
                }
                else
                {
                    yield return new FloatMenuOption(
                        "DC_RemoveMentor".Translate(actor.LabelShort.Named("PAWN"),
                            targetPawn.LabelShort.Named("APPRENTICE")),
                        () => MentorshipUtility.ClearMentorRelation(actor, targetPawn));
                }
            }

            if (DeepColonySettings.Get.enableTrauma
                && TraumaUtility.HasAnyTrauma(targetPawn)
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

                if (TraumaUtility.CountTraumatizedInRoom(targetPawn) >= 2)
                {
                    yield return new FloatMenuOption(
                        "DC_GroupCounselFloat".Translate(targetPawn.LabelShort.Named("PAWN")),
                        () =>
                        {
                            Job job = JobMaker.MakeJob(DC_DefOf.DC_Job_GroupCounsel, targetPawn);
                            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        });
                }
            }
        }
    }
}
