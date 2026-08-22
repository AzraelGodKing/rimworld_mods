using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C12 — a dying mentor can finish one last lesson if an apprentice is on the map.</summary>
    public static class DeathbedUtility
    {
        public static void NotifyMentorDying(Pawn mentor)
        {
            if (!DeepColonySettings.Get.enableMentoring) return;
            if (mentor == null || mentor.relations == null) return;
            if (!IsPlayerSide(mentor)) return;

            Map map = mentor.MapHeld;
            if (map == null) return;

            var toNotify = new System.Collections.Generic.List<Pawn>();
            foreach (DirectPawnRelation rel in mentor.relations.DirectRelations)
            {
                if (rel.def != DC_DefOf.DC_MentorOf || rel.otherPawn == null) continue;
                toNotify.Add(rel.otherPawn);
            }

            for (int i = 0; i < toNotify.Count; i++)
            {
                Pawn apprentice = toNotify[i];
                if (apprentice.Dead) continue;
                if (!IsPlayerSide(apprentice)) continue;
                if (apprentice.MapHeld != map && apprentice.MapHeld != mentor.MapHeld) continue;

                TryLastLesson(mentor, apprentice);
            }
        }

        private static bool IsPlayerSide(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.IsColonistPlayerControlled) return true;
            return pawn.Faction != null && pawn.Faction.IsPlayer;
        }

        private static void TryLastLesson(Pawn mentor, Pawn apprentice)
        {
            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            var mentorComp = mentor.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp == null || mentorComp == null) return;
            if (apprenticeComp.mentor != mentor) return;

            SkillDef focus = apprenticeComp.GetMentoredSkill();
            PerkDef teachable = MentorshipUtility.FindTier1PerkMentorCanTeach(
                mentorComp, apprenticeComp, focus);

            bool grantedPerk = false;
            if (DeepColonySettings.Get.enablePerks && teachable != null
                && !apprenticeComp.HasPerk(teachable)
                && apprentice.skills?.GetSkill(teachable.skill)?.Level >= teachable.requiredLevel)
            {
                apprenticeComp.UnlockPerkFree(teachable);
                grantedPerk = true;
            }
            else
            {
                MentorshipUtility.NotifyMentoringSessionComplete(mentor, apprentice);
            }

            apprenticeComp.RecordTeacher(mentor);

            string key = grantedPerk ? "DC_DeathbedPerk" : "DC_DeathbedLesson";
            Messages.Message(
                key.Translate(
                    mentor.LabelShort.Named("MENTOR"),
                    apprentice.LabelShort.Named("APPRENTICE"),
                    (teachable?.LabelCap ?? focus?.LabelCap ?? "skill").Named("GIFT")),
                new LookTargets(mentor, apprentice),
                MessageTypeDefOf.PositiveEvent,
                false);
        }
    }
}
