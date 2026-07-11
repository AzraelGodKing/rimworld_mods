using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_ExhaustVent : CompProperties
    {
        // Fraction of a room's smoke this vent clears each cycle while powered.
        public float ventPower = 0.25f;

        public CompProperties_ExhaustVent()
        {
            compClass = typeof(CompExhaustVent);
        }
    }

    // Attached to the exhaust fan. Registers with the smoke simulation and
    // reports when it's powered and able to extract fumes.
    public class CompExhaustVent : ThingComp
    {
        public CompProperties_ExhaustVent Props => (CompProperties_ExhaustVent)props;

        public bool Active
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<SmokeMapComponent>()?.Vents.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<SmokeMapComponent>()?.Vents.Remove(this);
            base.PostDeSpawn(map, mode);
        }
    }
}
