using Verse;

namespace Homesteader
{
    public class CompProperties_RootCellarCooling : CompProperties
    {
        /// <summary>
        /// Stored items never report ambient temperature above this (°C).
        /// Matches a cool earth cellar in summer; colder outdoor air still applies.
        /// </summary>
        public float maxTemperature = 5f;

        public CompProperties_RootCellarCooling()
        {
            compClass = typeof(CompRootCellarCooling);
        }
    }

    public class CompRootCellarCooling : ThingComp
    {
        public CompProperties_RootCellarCooling Props => (CompProperties_RootCellarCooling)props;

        public override string CompInspectStringExtra()
        {
            return "Root cellar: keeps stored food at or below "
                + Props.maxTemperature.ToStringTemperature();
        }
    }
}
