using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C21 — colony-raised siblings: extra opinion (teach-gap is in MentorshipUtility).</summary>
    public class ThoughtWorker_SiblingBond : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!DeepColonySettings.Get.enableMentoring && !DeepColonySettings.Get.enableInheritance)
                return false;
            if (pawn == null || other == null || pawn == other) return false;
            if (!pawn.RaceProps.Humanlike || !other.RaceProps.Humanlike) return false;

            var a = pawn.TryGetComp<Comp_DeepColony>();
            var b = other.TryGetComp<Comp_DeepColony>();
            if (a == null || b == null) return false;
            if (!a.bornInColony || !b.bornInColony) return false;

            if (PawnRelationDefOf.Sibling == null) return false;
            if (!PawnRelationDefOf.Sibling.Worker.InRelation(pawn, other)) return false;
            return ThoughtState.ActiveDefault;
        }
    }
}
