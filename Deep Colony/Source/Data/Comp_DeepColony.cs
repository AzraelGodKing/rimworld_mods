using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class CompProperties_DeepColony : CompProperties
    {
        public CompProperties_DeepColony() => compClass = typeof(Comp_DeepColony);
    }

    /// <summary>
    /// Per-pawn perk data and apprenticeship mentor link. Injected into humanlike defs at startup.
    /// </summary>
    public class Comp_DeepColony : ThingComp
    {
        public List<string> unlockedPerkDefNames = new List<string>();
        public int availablePerkPoints;

        public Pawn mentor;

        public Pawn Pawn => parent as Pawn;

        public bool HasPerk(PerkDef perk) =>
            unlockedPerkDefNames.Contains(perk.defName);

        public bool CanUnlock(PerkDef perk)
        {
            if (HasPerk(perk)) return false;
            if (availablePerkPoints <= 0) return false;
            var pawn = Pawn;
            if (pawn == null || pawn.skills == null) return false;
            if (pawn.skills.GetSkill(perk.skill).Level < perk.requiredLevel) return false;
            if (perk.HasPrerequisite)
            {
                var prereq = DefDatabase<PerkDef>.GetNamedSilentFail(perk.prerequisitePerk);
                if (prereq != null && !HasPerk(prereq)) return false;
            }
            return true;
        }

        public void UnlockPerk(PerkDef perk)
        {
            if (!CanUnlock(perk)) return;
            var pawn = Pawn;
            if (pawn == null) return;

            unlockedPerkDefNames.Add(perk.defName);
            availablePerkPoints--;
            ApplyPerkHediff(perk);

            Messages.Message(
                "DC_PerkUnlocked".Translate(pawn.LabelShort.Named("PAWN"), perk.LabelCap.Named("PERK")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public void NotifySkillLevelUp(SkillDef skill, int newLevel)
        {
            availablePerkPoints++;
            var pawn = Pawn;
            if (pawn == null) return;

            Messages.Message(
                "DC_PerkPointGained".Translate(pawn.LabelShort.Named("PAWN"), skill.LabelCap.Named("SKILL")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ReapplyPerkHediffs();
        }

        private void ReapplyPerkHediffs()
        {
            var pawn = Pawn;
            if (pawn?.health == null) return;

            for (int i = 0; i < unlockedPerkDefNames.Count; i++)
            {
                PerkDef perk = DefDatabase<PerkDef>.GetNamedSilentFail(unlockedPerkDefNames[i]);
                if (perk != null) ApplyPerkHediff(perk);
            }
        }

        private void ApplyPerkHediff(PerkDef perk)
        {
            var pawn = Pawn;
            if (pawn?.health == null || perk?.hediff == null) return;
            if (pawn.health.hediffSet.HasHediff(perk.hediff)) return;

            Hediff h = HediffMaker.MakeHediff(perk.hediff, pawn);
            pawn.health.AddHediff(h);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DC_ViewPerks".Translate(),
                defaultDesc = "DC_ViewPerksDesc".Translate(
                    availablePerkPoints,
                    unlockedPerkDefNames.Count),
                icon = ContentFinder<Texture2D>.Get("UI/PerkTree", false) ?? BaseContent.BadTex,
                action = () => Find.WindowStack.Add(new Window_PerkTree(pawn))
            };
        }

        public override string CompInspectStringExtra()
        {
            var pawn = Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) return null;

            var parts = new List<string>();
            if (availablePerkPoints > 0 || unlockedPerkDefNames.Count > 0)
            {
                parts.Add("DC_InspectPerks".Translate(availablePerkPoints, unlockedPerkDefNames.Count));
            }
            if (mentor != null)
            {
                parts.Add("DC_InspectMentor".Translate(mentor.LabelShort));
            }
            int apprentices = 0;
            if (pawn.relations != null)
            {
                foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
                {
                    if (rel.def == DC_DefOf.DC_MentorOf) apprentices++;
                }
            }
            if (apprentices > 0)
            {
                parts.Add("DC_InspectApprentices".Translate(apprentices));
            }
            if (TraumaUtility.HasAnyTrauma(pawn))
            {
                parts.Add("DC_InspectTrauma".Translate());
            }
            return parts.Count == 0 ? null : string.Join("\n", parts);
        }

        public override void PostExposeData()
        {
            Scribe_Collections.Look(ref unlockedPerkDefNames, "unlockedPerks", LookMode.Value);
            Scribe_Values.Look(ref availablePerkPoints, "availablePerkPoints", 0);
            Scribe_References.Look(ref mentor, "mentor");

            if (unlockedPerkDefNames == null) unlockedPerkDefNames = new List<string>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ReapplyPerkHediffs();
            }
        }
    }
}
