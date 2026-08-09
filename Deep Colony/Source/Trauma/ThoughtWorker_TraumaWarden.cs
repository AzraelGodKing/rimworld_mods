using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>B21 — captivity trauma + active Warden work → mood penalty (settings-gated).</summary>
    public class ThoughtWorker_TraumaWarden : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!DeepColonySettings.Get.enableTrauma) return ThoughtState.Inactive;
            if (!DeepColonySettings.Get.enableTraumaPenalties) return ThoughtState.Inactive;
            if (!TraumaUtility.HasTrauma(p, DC_DefOf.DC_Trauma_Captivity)) return ThoughtState.Inactive;
            if (p.workSettings == null) return ThoughtState.Inactive;
            if (!p.workSettings.WorkIsActive(WorkTypeDefOf.Warden)) return ThoughtState.Inactive;
            return ThoughtState.ActiveDefault;
        }
    }
}
