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

        public override string EnterString => "Descend the ancient stairwell";

        public override string EnteringString => "descending the ancient stairwell";

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            const string note = "An ancient shaft — no power ties between levels. "
                + "Digging-down research is not required to open the first level below.";
            return text.NullOrEmpty() ? note : text + "\n" + note;
        }
    }
}
