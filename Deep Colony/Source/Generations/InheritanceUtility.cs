using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class InheritanceUtility
    {
        private const float MajorPassionInheritChance = 0.35f;
        private const float MinorPassionInheritChance = 0.18f;
        private const float MajorFromMajorChance = 0.10f;
        private const float GrandparentChanceMul = 0.45f;
        private const float TraditionChance = 0.40f;
        private const float AdoptiveChance = 0.28f;
        private const int MaxInheritedTraits = 2;

        public static void TryApplyInheritance(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            var gameComp = GameComp_DeepColony.Instance;
            if (gameComp == null) return;
            if (gameComp.HasProcessedInheritance(pawn)) return;

            gameComp.MarkInheritanceProcessed(pawn);

            // Dead parents still count for bloodline / surname / traits
            var parents = GetColonistParents(pawn, includeDead: true);
            TryApplyFounderSurname(pawn, GetParentsIncludingDead(pawn));

            List<string> inheritanceLog = new List<string>();

            if (parents.Count > 0 && !ShouldBackOffForBiotech(pawn))
            {
                ApplyTraitInheritance(pawn, parents, 1f, inheritanceLog);
                ApplyPassionInheritance(pawn, parents, 1f, inheritanceLog);
                TryFamilySkillTradition(pawn, parents, inheritanceLog);

                var grandparents = GetColonistGrandparents(pawn, parents);
                if (grandparents.Count > 0)
                {
                    ApplyTraitInheritance(pawn, grandparents, GrandparentChanceMul, inheritanceLog);
                    ApplyPassionInheritance(pawn, grandparents, GrandparentChanceMul, inheritanceLog);
                }
            }
            else if (parents.Count == 0)
            {
                TryAdoptivePassionEcho(pawn, inheritanceLog);
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

        private static bool ShouldBackOffForBiotech(Pawn pawn)
        {
            // Avoid double-dipping xenotype trait/gene identity when Biotech genes are present.
            if (pawn?.genes == null) return false;
            return pawn.genes.Xenogenes != null && pawn.genes.Xenogenes.Count > 0;
        }

        private static void ApplyTraitInheritance(
            Pawn pawn, List<Pawn> donors, float chanceMul, List<string> log)
        {
            int added = 0;
            float traitChance = DeepColonySettings.Get.traitInheritChance * chanceMul;

            foreach (Pawn donor in donors)
            {
                if (donor.story?.traits == null) continue;
                foreach (Trait parentTrait in donor.story.traits.allTraits)
                {
                    if (added >= MaxInheritedTraits) return;
                    if (ShouldSkipInheritedTrait(parentTrait.def)) continue;
                    if (!Rand.Chance(traitChance)) continue;
                    if (pawn.story?.traits == null) continue;
                    if (pawn.story.traits.HasTrait(parentTrait.def)) continue;
                    if (pawn.story.traits.allTraits.Count >= 4) continue;

                    try
                    {
                        pawn.story.traits.GainTrait(
                            new Trait(parentTrait.def, parentTrait.Degree, forced: false));
                        log.Add("DC_InheritedTrait".Translate(
                            parentTrait.LabelCap.Named("TRAIT"),
                            donor.LabelShort.Named("PARENT")));
                        added++;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[DeepColony] InheritanceUtility: failed to apply trait " +
                                    $"{parentTrait.def.defName} to {pawn.LabelShort}: {ex.Message}");
                    }
                }
            }
        }

        // Rank is stamped from current level; destiny traits are unique scenario
        // power. Growth / combat / utility Isekai_* traits still inherit.
        private static readonly HashSet<string> BlockedIsekaiDestinyTraits = new HashSet<string>
        {
            "Isekai_Protagonist",
            "Isekai_Antagonist",
            "Isekai_Reincarnated",
            "Isekai_Regressor",
            "Isekai_SummonedHero",
            "Isekai_SealedPower",
        };

        /// <summary>
        /// Skip ISEKAI RPG Leveling rank (F–SSS) and destiny traits. Aptitude
        /// traits (Natural Talent, Prodigy, Mighty, Lucky, …) can inherit.
        /// </summary>
        internal static bool ShouldSkipInheritedTrait(TraitDef def)
        {
            if (def == null) return true;

            string name = def.defName;
            if (name.NullOrEmpty()) return false;
            if (name.StartsWith("Isekai_Rank_", StringComparison.Ordinal)) return true;
            return BlockedIsekaiDestinyTraits.Contains(name);
        }

        private static void ApplyPassionInheritance(
            Pawn pawn, List<Pawn> donors, float chanceMul, List<string> log)
        {
            foreach (Pawn donor in donors)
            {
                if (donor.skills == null || pawn.skills == null) continue;
                foreach (SkillRecord parentSkill in donor.skills.skills)
                {
                    if (parentSkill.passion == Passion.None) continue;

                    SkillRecord childSkill = pawn.skills.GetSkill(parentSkill.def);
                    if (childSkill == null) continue;

                    if (parentSkill.passion == Passion.Major)
                    {
                        if (childSkill.passion == Passion.None
                            && Rand.Chance(MajorPassionInheritChance * chanceMul))
                        {
                            childSkill.passion = Rand.Chance(MajorFromMajorChance)
                                ? Passion.Major
                                : Passion.Minor;
                            log.Add("DC_InheritedPassion".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                donor.LabelShort.Named("PARENT")));
                        }
                        else if (childSkill.passion == Passion.Minor
                                 && Rand.Chance(MajorFromMajorChance * chanceMul))
                        {
                            childSkill.passion = Passion.Major;
                            log.Add("DC_InheritedPassionMajor".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                donor.LabelShort.Named("PARENT")));
                        }
                    }
                    else if (parentSkill.passion == Passion.Minor)
                    {
                        if (childSkill.passion == Passion.None
                            && Rand.Chance(MinorPassionInheritChance * chanceMul))
                        {
                            childSkill.passion = Passion.Minor;
                            log.Add("DC_InheritedPassion".Translate(
                                parentSkill.def.LabelCap.Named("SKILL"),
                                donor.LabelShort.Named("PARENT")));
                        }
                    }
                }
            }
        }

        /// <summary>A14 — preferred skill bias from ancestor's highest passion skill.</summary>
        private static void TryFamilySkillTradition(
            Pawn pawn, List<Pawn> parents, List<string> log)
        {
            if (pawn.skills == null || !Rand.Chance(TraditionChance)) return;

            SkillRecord tradition = null;
            Pawn source = null;
            foreach (Pawn parent in parents)
            {
                if (parent.skills == null) continue;
                foreach (SkillRecord sr in parent.skills.skills)
                {
                    if (sr.passion == Passion.None) continue;
                    if (tradition == null
                        || (int)sr.passion > (int)tradition.passion
                        || (sr.passion == tradition.passion && sr.Level > tradition.Level))
                    {
                        tradition = sr;
                        source = parent;
                    }
                }
            }
            if (tradition == null || source == null) return;

            SkillRecord child = pawn.skills.GetSkill(tradition.def);
            if (child == null) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp != null)
                comp.familyTraditionSkillDefName = tradition.def.defName;

            if (child.passion == Passion.None)
            {
                child.passion = Passion.Minor;
                log.Add("DC_FamilyTradition".Translate(
                    tradition.def.LabelCap.Named("SKILL"),
                    source.LabelShort.Named("PARENT")));
            }
        }

        /// <summary>A15 — orphan / no-parent colonist kids pick up a passion from caregiver.</summary>
        private static void TryAdoptivePassionEcho(Pawn pawn, List<string> log)
        {
            if (pawn?.skills == null || pawn.relations == null) return;
            if (!Rand.Chance(AdoptiveChance)) return;

            Pawn caregiver = null;
            int bestOpinion = 40;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn other in map.mapPawns.FreeColonists)
                {
                    if (other == pawn || other.Dead || other.skills == null) continue;
                    if (other.ageTracker != null
                        && other.ageTracker.AgeBiologicalYearsFloat
                            < pawn.ageTracker.AgeBiologicalYearsFloat + 10f)
                        continue;
                    int opinion = pawn.relations.OpinionOf(other);
                    if (opinion > bestOpinion)
                    {
                        bestOpinion = opinion;
                        caregiver = other;
                    }
                }
            }
            if (caregiver == null) return;

            SkillRecord best = null;
            foreach (SkillRecord sr in caregiver.skills.skills)
            {
                if (sr.TotallyDisabled || sr.passion == Passion.None) continue;
                if (best == null || sr.Level > best.Level) best = sr;
            }
            if (best == null) return;

            SkillRecord child = pawn.skills.GetSkill(best.def);
            if (child == null || child.passion != Passion.None) return;

            child.passion = Passion.Minor;
            log.Add("DC_AdoptivePassion".Translate(
                best.def.LabelCap.Named("SKILL"),
                caregiver.LabelShort.Named("PARENT")));
        }

        public static void TryApplyFounderSurname(Pawn pawn, List<Pawn> parents)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (pawn?.Name is not NameTriple childName) return;
            if (!childName.Last.NullOrEmpty()) return;

            string surname = null;
            if (parents != null)
            {
                foreach (Pawn parent in parents)
                {
                    if (parent.Name is NameTriple pt && !pt.Last.NullOrEmpty())
                    {
                        surname = pt.Last;
                        break;
                    }
                }
            }

            if (surname.NullOrEmpty())
                surname = GameComp_DeepColony.Instance?.GetFounderSurname();

            if (surname.NullOrEmpty()) return;

            pawn.Name = new NameTriple(childName.First, childName.Nick, surname);
            Messages.Message(
                "DC_SurnameApplied".Translate(pawn.LabelShort.Named("PAWN"), surname.Named("SURNAME")),
                pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        private static List<Pawn> GetColonistParents(Pawn pawn, bool includeDead)
        {
            var result = new List<Pawn>();
            if (pawn.relations == null) return result;

            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent) continue;
                Pawn parent = rel.otherPawn;
                if (parent == null) continue;
                if (!includeDead && parent.Dead) continue;
                if (IsColonistBloodline(parent))
                    result.Add(parent);
            }
            return result;
        }

        private static bool IsColonistBloodline(Pawn parent)
        {
            return parent.Faction == Faction.OfPlayer
                || (GameComp_DeepColony.Instance?.WasEverPlayerColonist(parent) ?? false)
                || (GameComp_DeepColony.Instance?.HasProcessedInheritance(parent) ?? false);
        }

        private static List<Pawn> GetColonistGrandparents(Pawn pawn, List<Pawn> parents)
        {
            var result = new List<Pawn>();
            foreach (Pawn parent in parents)
            {
                if (parent.relations == null) continue;
                foreach (DirectPawnRelation rel in parent.relations.DirectRelations)
                {
                    if (rel.def != PawnRelationDefOf.Parent) continue;
                    Pawn gp = rel.otherPawn;
                    if (gp == null || result.Contains(gp)) continue;
                    if (IsColonistBloodline(gp) || gp.Dead)
                        result.Add(gp);
                }
            }
            return result;
        }

        private static List<Pawn> GetParentsIncludingDead(Pawn pawn)
        {
            var result = new List<Pawn>();
            if (pawn.relations == null) return result;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent) continue;
                if (rel.otherPawn != null) result.Add(rel.otherPawn);
            }
            return result;
        }
    }
}
