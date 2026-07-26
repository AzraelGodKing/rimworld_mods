using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class InheritanceUtility
    {
        private const float TraitInheritChance = 0.35f;
        private const float MajorPassionInheritChance = 0.35f;
        private const float MinorPassionInheritChance = 0.18f;
        private const float MajorFromMajorChance = 0.10f;
        private const int MaxInheritedTraits = 2;

        public static void TryApplyInheritance(Pawn pawn)
        {
            var gameComp = GameComp_DeepColony.Instance;
            if (gameComp == null) return;
            if (gameComp.HasProcessedInheritance(pawn)) return;

            gameComp.MarkInheritanceProcessed(pawn);

            var parents = GetColonistParents(pawn);
            if (parents.Count == 0) return;

            List<string> inheritanceLog = new List<string>();
            int inherited = 0;

            foreach (Pawn parent in parents)
            {
                if (parent.story?.traits == null) continue;
                foreach (Trait parentTrait in parent.story.traits.allTraits)
                {
                    if (inherited >= MaxInheritedTraits) break;
                    if (!Rand.Chance(TraitInheritChance)) continue;
                    if (pawn.story?.traits == null) continue;
                    if (pawn.story.traits.HasTrait(parentTrait.def)) continue;
                    if (pawn.story.traits.allTraits.Count >= 4) continue;

                    try
                    {
                        pawn.story.traits.GainTrait(
                            new Trait(parentTrait.def, parentTrait.Degree, forced: false));
                        inheritanceLog.Add("DC_InheritedTrait".Translate(
                            parentTrait.LabelCap.Named("TRAIT"),
                            parent.LabelShort.Named("PARENT")));
                        inherited++;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[DeepColony] InheritanceUtility: failed to apply trait " +
                                    $"{parentTrait.def.defName} to {pawn.LabelShort}: {ex.Message}");
                    }
                }
            }

            foreach (Pawn parent in parents)
            {
                if (parent.skills == null || pawn.skills == null) continue;
                foreach (SkillRecord parentSkill in parent.skills.skills)
                {
                    if (parentSkill.passion == Passion.None) continue;

                    SkillRecord childSkill = pawn.skills.GetSkill(parentSkill.def);
                    if (childSkill == null) continue;

                    if (parentSkill.passion == Passion.Major)
                    {
                        if (childSkill.passion == Passion.None && Rand.Chance(MajorPassionInheritChance))
                        {
                            childSkill.passion = Rand.Chance(MajorFromMajorChance)
                                ? Passion.Major
                                : Passion.Minor;
                            inheritanceLog.Add("DC_InheritedPassion".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                parent.LabelShort.Named("PARENT")));
                        }
                        else if (childSkill.passion == Passion.Minor && Rand.Chance(MajorFromMajorChance))
                        {
                            childSkill.passion = Passion.Major;
                            inheritanceLog.Add("DC_InheritedPassionMajor".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                parent.LabelShort.Named("PARENT")));
                        }
                    }
                    else if (parentSkill.passion == Passion.Minor)
                    {
                        if (childSkill.passion == Passion.None && Rand.Chance(MinorPassionInheritChance))
                        {
                            childSkill.passion = Passion.Minor;
                            inheritanceLog.Add("DC_InheritedPassion".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                parent.LabelShort.Named("PARENT")));
                        }
                    }
                }
            }

            if (inheritanceLog.Count == 0) return;

            string body = "DC_InheritanceLetter_Body".Translate(
                pawn.LabelShort.Named("PAWN")) + "\n\n" +
                string.Join("\n", inheritanceLog.Select(l => "  • " + l));

            Find.LetterStack.ReceiveLetter(
                "DC_InheritanceLetter_Label".Translate(pawn.LabelShort.Named("PAWN")),
                body,
                LetterDefOf.NeutralEvent,
                pawn);
        }

        private static List<Pawn> GetColonistParents(Pawn pawn)
        {
            var result = new List<Pawn>();
            if (pawn.relations == null) return result;

            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent) continue;
                Pawn parent = rel.otherPawn;
                if (parent == null || parent.Dead) continue;
                if (parent.Faction == Faction.OfPlayer
                    || (GameComp_DeepColony.Instance?.WasEverPlayerColonist(parent) ?? false)
                    || (GameComp_DeepColony.Instance?.HasProcessedInheritance(parent) ?? false))
                {
                    result.Add(parent);
                }
            }
            return result;
        }
    }
}
