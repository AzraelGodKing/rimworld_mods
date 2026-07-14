using Verse;

namespace Strata
{
    public class CompProperties_DeepGasVent : CompProperties
    {
        public CompProperties_DeepGasVent()
        {
            compClass = typeof(CompDeepGasVent);
        }
    }

    // Natural gas vent left behind when a deep pocket is breached.
    public class CompDeepGasVent : ThingComp
    {
        public override string CompInspectStringExtra()
        {
            return "Pressurized natural gas vent. Build a gas well here to harvest it.";
        }
    }
}
