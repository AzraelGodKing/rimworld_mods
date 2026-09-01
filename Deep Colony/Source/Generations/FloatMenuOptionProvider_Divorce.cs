using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>AZR-97 — right-click a spouse to dissolve the marriage.</summary>
    public class FloatMenuOptionProvider_Divorce : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Pawn targetPawn, FloatMenuContext context)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) yield break;
            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || actor == targetPawn) yield break;
            if (!DivorceUtility.AreSpouses(actor, targetPawn)) yield break;

            if (!DivorceUtility.CanDivorce(actor, targetPawn, out string reason))
            {
                string label = "DC_DivorceFloat".Translate(targetPawn.LabelShort.Named("PAWN"));
                if (!reason.NullOrEmpty()) label = label + " (" + reason + ")";
                yield return new FloatMenuOption(label, null) { Disabled = true };
                yield break;
            }

            yield return new FloatMenuOption(
                "DC_DivorceFloat".Translate(targetPawn.LabelShort.Named("PAWN")),
                () => DivorceUtility.TryDivorce(actor, targetPawn));
        }
    }
}
