using Verse;

namespace Strata
{
    public class CompProperties_SmokeDuct : CompProperties
    {
        public CompProperties_SmokeDuct()
        {
            compClass = typeof(CompSmokeDuct);
        }
    }

    // Marks a floor duct tile for the smoke-routing graph.
    public class CompSmokeDuct : ThingComp
    {
    }
}
