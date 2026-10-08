using RimWorld;
using Verse;

namespace Strata
{
    // Pre-existing stairhead scattered on some colony maps. Opens the first
    // underground level (solid stratum B1) without digging-down research, but
    // carries no power shaft — wire each level separately or build a real
    // stairwell / shaft conduit later for cross-level power.
    public class Building_AncientColonyStairsDown : Building_StairsDown
    {
        protected override bool BypassFirstLevelResearch => true;

        public override string EnterString => "Strata_Enter_AncientDown".Translate();

        public override string EnteringString => "Strata_Entering_AncientDown".Translate();

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string note = "Strata_Inspect_AncientStairs".Translate();
            return text.NullOrEmpty() ? note : text + "\n" + note;
        }
    }
}
