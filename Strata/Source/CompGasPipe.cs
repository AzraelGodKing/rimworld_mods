using Verse;

namespace Strata
{
    public class CompProperties_GasPipe : CompProperties
    {
        public bool hidden;

        public CompProperties_GasPipe()
        {
            compClass = typeof(CompGasPipe);
        }
    }

    // Marks a conduit-layer gas pipe tile for the atmosphere routing graph.
    public class CompGasPipe : ThingComp
    {
        public CompProperties_GasPipe Props => (CompProperties_GasPipe)props;

        public bool Hidden => Props.hidden;
    }
}
