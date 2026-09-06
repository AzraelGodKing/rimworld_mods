using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class RivalryUtility
    {
        private const int MinLevel = 8;
        private const int LevelBand = 2;
        private const int CheckInterval = 2500;
        private static readonly List<Pawn> Scratch = new List<Pawn>();

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableMentoring) return;
            if (!TickPhase.Due(0)) return;

            Scratch.Clear();
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonistsSpawned == null) continue;
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                    Scratch.Add(p);
            }

            int n = Scratch.Count;
            if (n < 2) return;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                    TryFormRivalry(Scratch[i], Scratch[j]);
            }
        }

        public static bool AreRivals(Pawn a, Pawn b)
        {
            if (a?.relations == null || b == null) return false;
            return a.relations.DirectRelationExists(DC_DefOf.DC_Rival, b);
        }

        public static Pawn FirstLivingRival(Pawn pawn)
        {
            if (pawn?.relations == null) return null;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != DC_DefOf.DC_Rival || rel.otherPawn == null || rel.otherPawn.Dead)
                    continue;
                return rel.otherPawn;
            }
            return null;
        }

        public static void TryFormRivalry(Pawn a, Pawn b)
        {
            if (a?.skills == null || b?.skills == null || a.relations == null || b.relations == null)
                return;
            if (a.relations.DirectRelationExists(DC_DefOf.DC_Rival, b)) return;
            if (MentorshipUtility.IsLineagePair(a, b)) return;
            if (a.relations.OpinionOf(b) >= 0 || b.relations.OpinionOf(a) >= 0) return;

            SkillDef shared = FindSharedStrongSkill(a, b);
            if (shared == null) return;

            a.relations.AddDirectRelation(DC_DefOf.DC_Rival, b);
            if (!b.relations.DirectRelationExists(DC_DefOf.DC_Rival, a))
                b.relations.AddDirectRelation(DC_DefOf.DC_Rival, a);

            Messages.Message(
                "DC_RivalFormed".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B"),
                    shared.LabelCap.Named("SKILL")),
                new LookTargets(a, b),
                MessageTypeDefOf.NeutralEvent,
                false);
        }

        private static SkillDef FindSharedStrongSkill(Pawn a, Pawn b)
        {
            SkillDef best = null;
            int bestLevel = 0;
            foreach (SkillRecord sa in a.skills.skills)
            {
                if (sa.TotallyDisabled || sa.Level < MinLevel) continue;
                SkillRecord sb = b.skills.GetSkill(sa.def);
                if (sb == null || sb.TotallyDisabled || sb.Level < MinLevel) continue;
                if (System.Math.Abs(sa.Level - sb.Level) > LevelBand) continue;
                int avg = (sa.Level + sb.Level) / 2;
                if (best == null || avg > bestLevel)
                {
                    best = sa.def;
                    bestLevel = avg;
                }
            }
            return best;
        }

        public static bool HasRivalBoost(Pawn pawn, SkillDef skill)
        {
            if (pawn?.relations == null || skill == null) return false;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != DC_DefOf.DC_Rival || rel.otherPawn == null || rel.otherPawn.Dead)
                    continue;
                SkillRecord mine = pawn.skills?.GetSkill(skill);
                SkillRecord theirs = rel.otherPawn.skills?.GetSkill(skill);
                if (mine == null || theirs == null) continue;
                if (mine.Level >= MinLevel && theirs.Level >= MinLevel
                    && System.Math.Abs(mine.Level - theirs.Level) <= LevelBand + 2)
                    return true;
            }
            return false;
        }
    }
}
