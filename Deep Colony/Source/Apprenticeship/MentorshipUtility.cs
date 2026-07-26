using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class MentorshipUtility
    {
        private const int MinSkillLead = 3;

        public static bool CanMentor(Pawn mentor, Pawn apprentice, out string reason)
        {
            reason = null;
            if (mentor == null || apprentice == null || mentor == apprentice)
            {
                reason = "DC_MentorInvalid".Translate();
                return false;
            }

            var mentorComp = mentor.TryGetComp<Comp_DeepColony>();
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (mentorComp == null || apprenticeComp == null)
            {
                reason = "DC_MentorInvalid".Translate();
                return false;
            }

            if (mentorComp.mentor == apprentice)
            {
                reason = "DC_MentorMutual".Translate();
                return false;
            }

            if (mentor.skills == null || apprentice.skills == null)
            {
                reason = "DC_MentorInvalid".Translate();
                return false;
            }

            if (!MentorLeadsInAnySkill(mentor, apprentice))
            {
                reason = "DC_MentorSkillGap".Translate(MinSkillLead);
                return false;
            }

            return true;
        }

        public static bool MentorLeadsInAnySkill(Pawn mentor, Pawn apprentice)
        {
            foreach (SkillRecord skill in mentor.skills.skills)
            {
                if (skill.TotallyDisabled) continue;
                SkillRecord theirs = apprentice.skills.GetSkill(skill.def);
                if (theirs == null || theirs.TotallyDisabled) continue;
                if (skill.Level >= theirs.Level + MinSkillLead) return true;
            }
            return false;
        }

        public static void ClearAllLinksInvolving(Pawn pawn)
        {
            if (pawn == null) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp?.mentor != null)
            {
                ClearMentorRelation(comp.mentor, pawn, silent: true);
            }

            if (pawn.relations == null) return;

            var toClear = new List<Pawn>();
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def == DC_DefOf.DC_MentorOf && rel.otherPawn != null)
                    toClear.Add(rel.otherPawn);
            }
            for (int i = 0; i < toClear.Count; i++)
            {
                ClearMentorRelation(pawn, toClear[i], silent: true);
            }
        }

        public static void SetMentorRelation(Pawn mentor, Pawn apprentice)
        {
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null) return;

            if (apprenticeComp.mentor != null)
            {
                ClearMentorRelation(apprenticeComp.mentor, apprentice, silent: true);
            }

            apprenticeComp.mentor = mentor;
            if (!apprentice.relations.DirectRelationExists(DC_DefOf.DC_ApprenticeOf, mentor))
                apprentice.relations.AddDirectRelation(DC_DefOf.DC_ApprenticeOf, mentor);
            if (!mentor.relations.DirectRelationExists(DC_DefOf.DC_MentorOf, apprentice))
                mentor.relations.AddDirectRelation(DC_DefOf.DC_MentorOf, apprentice);

            Messages.Message(
                "DC_MentorAssigned".Translate(mentor.LabelShort.Named("MENTOR"),
                    apprentice.LabelShort.Named("APPRENTICE")),
                apprentice, MessageTypeDefOf.NeutralEvent, false);
        }

        public static void ClearMentorRelation(Pawn mentor, Pawn apprentice, bool silent = false)
        {
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null) return;

            if (mentor != null)
            {
                apprentice.relations?.TryRemoveDirectRelation(DC_DefOf.DC_ApprenticeOf, mentor);
                mentor.relations?.TryRemoveDirectRelation(DC_DefOf.DC_MentorOf, apprentice);
                ActiveMentoringSession.Clear(mentor, apprentice);
            }

            if (apprenticeComp.mentor == mentor)
                apprenticeComp.mentor = null;

            if (!silent)
            {
                Messages.Message(
                    "DC_MentorRemoved".Translate(
                        (mentor?.LabelShort ?? "someone").Named("MENTOR"),
                        apprentice.LabelShort.Named("APPRENTICE")),
                    apprentice, MessageTypeDefOf.NeutralEvent, false);
            }
        }
    }
}
