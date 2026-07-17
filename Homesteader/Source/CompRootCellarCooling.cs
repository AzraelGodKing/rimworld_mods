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

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map?.GetComponent<RootCellarCoolingMapComponent>()?.Rebuild();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            map?.GetComponent<RootCellarCoolingMapComponent>()?.Rebuild();
        }

        public override string CompInspectStringExtra()
        {
            return "Root cellar: cools stored food (and adjacent indoor cells) to "
                + Props.maxTemperature.ToStringTemperature() + " or below";
        }
    }
}
