using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class MentorshipUtility
    {
        public static int MinSkillLead => DeepColonySettings.Get.minSkillLead;

        public static bool IsLineagePair(Pawn a, Pawn b)
        {
            if (a?.relations == null || b?.relations == null) return false;

            // Parent is the only bloodline link stored as a *direct* relation.
            if (a.relations.DirectRelationExists(PawnRelationDefOf.Parent, b)
                || b.relations.DirectRelationExists(PawnRelationDefOf.Parent, a))
                return true;

            // Sibling / grandparent / grandchild are implied — use relation workers.
            if (PawnRelationDefOf.Sibling != null
                && PawnRelationDefOf.Sibling.Worker.InRelation(a, b))
                return true;
            if (PawnRelationDefOf.Grandparent != null
                && (PawnRelationDefOf.Grandparent.Worker.InRelation(a, b)
                    || PawnRelationDefOf.Grandparent.Worker.InRelation(b, a)))
                return true;

            return ShareAParent(a, b);
        }

        private static bool ShareAParent(Pawn a, Pawn b)
        {
            if (a.relations == null || b.relations == null) return false;
            foreach (DirectPawnRelation rel in a.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent || rel.otherPawn == null) continue;
                if (b.relations.DirectRelationExists(PawnRelationDefOf.Parent, rel.otherPawn))
                    return true;
            }
            return false;
        }

        public static int EffectiveSkillLead(Pawn mentor, Pawn apprentice)
        {
            int lead = MinSkillLead;
            if (IsLineagePair(mentor, apprentice))
                lead = System.Math.Max(1, lead - 1);
            return lead;
        }

        public static bool CanMentor(Pawn mentor, Pawn apprentice, out string reason)
        {
            return CanMentor(mentor, apprentice, null, out reason);
        }

        public static bool CanMentor(Pawn mentor, Pawn apprentice, SkillDef skill, out string reason)
        {
            reason = null;
            if (!DeepColonySettings.Get.enableMentoring)
            {
                reason = "DC_MentorInvalid".Translate();
                return false;
            }
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

            int lead = EffectiveSkillLead(mentor, apprentice);
            if (skill != null)
            {
                if (!MentorLeadsInSkill(mentor, apprentice, skill, lead))
                {
                    reason = "DC_MentorSkillGap".Translate(lead);
                    return false;
                }
            }
            else if (!MentorLeadsInAnySkill(mentor, apprentice, lead))
            {
                reason = "DC_MentorSkillGap".Translate(lead);
                return false;
            }

            return true;
        }

        public static bool MentorLeadsInAnySkill(Pawn mentor, Pawn apprentice)
        {
            return MentorLeadsInAnySkill(mentor, apprentice, EffectiveSkillLead(mentor, apprentice));
        }

        public static bool MentorLeadsInAnySkill(Pawn mentor, Pawn apprentice, int lead)
        {
            foreach (SkillRecord skill in mentor.skills.skills)
            {
                if (skill.TotallyDisabled) continue;
                if (MentorLeadsInSkill(mentor, apprentice, skill.def, lead)) return true;
            }
            return false;
        }

        public static bool MentorLeadsInSkill(Pawn mentor, Pawn apprentice, SkillDef skill, int lead)
        {
            if (skill == null || mentor.skills == null || apprentice.skills == null) return false;
            SkillRecord theirs = apprentice.skills.GetSkill(skill);
            SkillRecord mine = mentor.skills.GetSkill(skill);
            if (theirs == null || mine == null || theirs.TotallyDisabled || mine.TotallyDisabled)
                return false;
            return mine.Level >= theirs.Level + lead;
        }

        public static IEnumerable<SkillDef> SkillsMentorCanTeach(Pawn mentor, Pawn apprentice)
        {
            int lead = EffectiveSkillLead(mentor, apprentice);
            foreach (SkillRecord skill in mentor.skills.skills.OrderByDescending(s => s.Level))
            {
                if (skill.TotallyDisabled) continue;
                if (MentorLeadsInSkill(mentor, apprentice, skill.def, lead))
                    yield return skill.def;
            }
        }

        public static void TryGraduate(Pawn mentor, Pawn apprentice)
        {
            if (!DeepColonySettings.Get.enableMentoring) return;
            if (mentor == null || apprentice == null) return;

            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp?.mentor != mentor) return;

            SkillDef focus = apprenticeComp.GetMentoredSkill();
            int lead = EffectiveSkillLead(mentor, apprentice);
            if (focus != null)
            {
                // Graduate when within 1 level of mentor in the taught skill.
                SkillRecord m = mentor.skills?.GetSkill(focus);
                SkillRecord a = apprentice.skills?.GetSkill(focus);
                if (m == null || a == null) return;
                if (m.Level > a.Level + 1) return;
            }
            else if (MentorLeadsInAnySkill(mentor, apprentice, lead))
            {
                return;
            }

            Graduate(mentor, apprentice);
        }

        public static void Graduate(Pawn mentor, Pawn apprentice)
        {
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null) return;

            apprenticeComp.RecordTeacher(mentor);
            TryInheritMentorPassion(mentor, apprentice, apprenticeComp.GetMentoredSkill());
            ClearMentorRelation(mentor, apprentice, silent: true);

            string label = "DC_GraduationLetter_Label".Translate(
                apprentice.LabelShort.Named("APPRENTICE"));
            string body = "DC_GraduationLetter_Body".Translate(
                apprentice.LabelShort.Named("APPRENTICE"),
                mentor.LabelShort.Named("MENTOR"));

            Find.LetterStack.ReceiveLetter(label, body, LetterDefOf.PositiveEvent,
                new LookTargets(apprentice, mentor));

            Messages.Message(
                "DC_GraduationMessage".Translate(
                    apprentice.LabelShort.Named("APPRENTICE"),
                    mentor.LabelShort.Named("MENTOR")),
                new LookTargets(apprentice, mentor),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private static void TryInheritMentorPassion(Pawn mentor, Pawn apprentice, SkillDef focus)
        {
            if (mentor?.skills == null || apprentice?.skills == null) return;
            if (!Rand.Chance(0.35f)) return;

            SkillRecord best = null;
            if (focus != null)
            {
                best = mentor.skills.GetSkill(focus);
                if (best != null && (best.TotallyDisabled || best.passion == Passion.None))
                    best = null;
            }
            if (best == null)
            {
                foreach (SkillRecord sr in mentor.skills.skills)
                {
                    if (sr.TotallyDisabled || sr.passion == Passion.None) continue;
                    if (best == null || sr.Level > best.Level) best = sr;
                }
            }
            if (best == null) return;

            SkillRecord theirs = apprentice.skills.GetSkill(best.def);
            if (theirs == null || theirs.TotallyDisabled) return;
            if (theirs.passion != Passion.None) return;

            theirs.passion = Passion.Minor;
            Messages.Message(
                "DC_GraduationPassion".Translate(
                    apprentice.LabelShort.Named("APPRENTICE"),
                    best.def.LabelCap.Named("SKILL"),
                    mentor.LabelShort.Named("MENTOR")),
                apprentice, MessageTypeDefOf.PositiveEvent, false);
        }

        public static void ClearAllLinksInvolving(Pawn pawn)
        {
            if (pawn == null) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp?.mentor != null)
                ClearMentorRelation(comp.mentor, pawn, silent: true);

            if (pawn.relations == null) return;

            var toClear = new List<Pawn>();
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def == DC_DefOf.DC_MentorOf && rel.otherPawn != null)
                    toClear.Add(rel.otherPawn);
            }
            for (int i = 0; i < toClear.Count; i++)
                ClearMentorRelation(pawn, toClear[i], silent: true);
        }

        public static void SetMentorRelation(Pawn mentor, Pawn apprentice, SkillDef skill = null)
        {
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null) return;

            if (apprenticeComp.mentor != null)
                ClearMentorRelation(apprenticeComp.mentor, apprentice, silent: true);

            if (skill == null)
            {
                // Pick best lead skill
                skill = SkillsMentorCanTeach(mentor, apprentice).FirstOrDefault();
            }

            apprenticeComp.mentor = mentor;
            apprenticeComp.SetMentoredSkill(skill);
            apprenticeComp.RecordTeacher(mentor);

            if (!apprentice.relations.DirectRelationExists(DC_DefOf.DC_ApprenticeOf, mentor))
                apprentice.relations.AddDirectRelation(DC_DefOf.DC_ApprenticeOf, mentor);
            if (!mentor.relations.DirectRelationExists(DC_DefOf.DC_MentorOf, apprentice))
                mentor.relations.AddDirectRelation(DC_DefOf.DC_MentorOf, apprentice);

            string msg = skill != null
                ? "DC_MentorAssignedSkill".Translate(
                    mentor.LabelShort.Named("MENTOR"),
                    apprentice.LabelShort.Named("APPRENTICE"),
                    skill.LabelCap.Named("SKILL"))
                : "DC_MentorAssigned".Translate(mentor.LabelShort.Named("MENTOR"),
                    apprentice.LabelShort.Named("APPRENTICE"));
            Messages.Message(msg, apprentice, MessageTypeDefOf.NeutralEvent, false);
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
            {
                apprenticeComp.mentor = null;
                apprenticeComp.SetMentoredSkill(null);
                apprenticeComp.perkTeachProgress = 0;
                apprenticeComp.perkBeingTaughtDefName = null;
            }

            if (!silent)
            {
                Messages.Message(
                    "DC_MentorRemoved".Translate(
                        (mentor?.LabelShort ?? "someone").Named("MENTOR"),
                        apprentice.LabelShort.Named("APPRENTICE")),
                    apprentice, MessageTypeDefOf.NeutralEvent, false);
            }
        }

        /// <summary>Active mentoring session finished — advance perk-apprenticeship progress.</summary>
        public static void NotifyMentoringSessionComplete(Pawn mentor, Pawn apprentice)
        {
            if (!DeepColonySettings.Get.enableMentoring || !DeepColonySettings.Get.enablePerks)
                return;
            if (mentor == null || apprentice == null) return;

            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            var mentorComp = mentor.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null || mentorComp == null) return;
            if (apprenticeComp.mentor != mentor) return;

            SkillDef focus = apprenticeComp.GetMentoredSkill();
            PerkDef teachable = FindTier1PerkMentorCanTeach(mentorComp, apprenticeComp, focus);
            if (teachable == null) return;

            if (apprenticeComp.perkBeingTaughtDefName != teachable.defName)
            {
                apprenticeComp.perkBeingTaughtDefName = teachable.defName;
                apprenticeComp.perkTeachProgress = 0;
            }

            apprenticeComp.perkTeachProgress++;
            const int SessionsNeeded = 3;
            if (apprenticeComp.perkTeachProgress < SessionsNeeded)
            {
                Messages.Message(
                    "DC_PerkTeachProgress".Translate(
                        apprentice.LabelShort.Named("APPRENTICE"),
                        teachable.LabelCap.Named("PERK"),
                        apprenticeComp.perkTeachProgress,
                        SessionsNeeded),
                    apprentice, MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            // Grant free tier-1 perk
            if (!apprenticeComp.HasPerk(teachable)
                && apprentice.skills.GetSkill(teachable.skill).Level >= teachable.requiredLevel)
            {
                apprenticeComp.UnlockPerkFree(teachable);
                Messages.Message(
                    "DC_PerkTaught".Translate(
                        mentor.LabelShort.Named("MENTOR"),
                        apprentice.LabelShort.Named("APPRENTICE"),
                        teachable.LabelCap.Named("PERK")),
                    new LookTargets(mentor, apprentice),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }

            apprenticeComp.perkTeachProgress = 0;
            apprenticeComp.perkBeingTaughtDefName = null;
        }

        public static PerkDef FindTier1PerkMentorCanTeach(
            Comp_DeepColony mentorComp, Comp_DeepColony apprenticeComp, SkillDef focus)
        {
            foreach (string name in mentorComp.unlockedPerkDefNames)
            {
                PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(name);
                if (perk == null || perk.requiredLevel > 5) continue; // tier-1 only
                if (focus != null && perk.skill != focus) continue;
                if (apprenticeComp.HasPerk(perk)) continue;
                var pawn = apprenticeComp.Pawn;
                if (pawn?.skills == null) continue;
                if (pawn.skills.GetSkill(perk.skill).Level < perk.requiredLevel) continue;
                return perk;
            }
            return null;
        }

        public static float ChalkboardRoomMultiplier(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned) return 1f;
            // Biotech blackboard — soft-fail if DLC / def missing
            ThingDef blackboard = DefDatabase<ThingDef>.GetNamedSilentFail("Blackboard");
            if (blackboard == null) return 1f;
            Room room = pawn.GetRoom();
            if (room == null || room.PsychologicallyOutdoors) return 1f;
            foreach (Thing t in room.ContainedAndAdjacentThings)
            {
                if (t.def == blackboard)
                    return 1.15f;
            }
            return 1f;
        }
    }
}
