using Verse;

namespace DeepColony
{
    /// <summary>
    /// Perk bonus hediff. Hidden on the health tab unless the player opts in.
    /// </summary>
    public class Hediff_Perk : HediffWithComps
    {
        public override bool Visible => DeepColonySettings.Get.showPerkHediffs;
    }
}
