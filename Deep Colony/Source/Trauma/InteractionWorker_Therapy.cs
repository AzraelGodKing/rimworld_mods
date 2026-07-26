using RimWorld;
using Verse;

namespace DeepColony
{
    public class InteractionWorker_Therapy : InteractionWorker
    {
        public override void Interacted(Pawn initiator, Pawn recipient,
            System.Collections.Generic.List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel,
            out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            TraumaUtility.ApplyTherapy(initiator, recipient);
        }

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (recipient?.needs?.mood?.thoughts == null) return 0f;
            if (!TraumaUtility.HasAnyTrauma(recipient)) return 0f;
            if (initiator.skills == null) return 0.04f;

            // Social skill gently increases how often therapy is offered.
            float social = initiator.skills.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            return 0.04f + social * 0.004f;
        }
    }
}
