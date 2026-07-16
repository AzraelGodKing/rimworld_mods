using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_GasAirlock : CompProperties
    {
        public CompProperties_GasAirlock()
        {
            compClass = typeof(CompGasAirlock);
        }
    }

    // Flickable gas seal — when engaged, blocks room-to-room gas equalization
    // through this cell (see AtmosphereMapComponent.ProcessDoorFlow).
    public class CompGasAirlock : ThingComp
    {
        public bool IsSealed
        {
            get
            {
                CompFlickable flick = parent.GetComp<CompFlickable>();
                return flick == null || flick.SwitchIsOn;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<AtmosphereMapComponent>()?.GasSeals.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<AtmosphereMapComponent>()?.GasSeals.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            return IsSealed
                ? "Sealed — gas cannot pass through."
                : "Open — gas can equalize through this opening.";
        }
    }
}
