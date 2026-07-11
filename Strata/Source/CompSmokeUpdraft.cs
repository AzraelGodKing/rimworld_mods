using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_SmokeUpdraft : CompProperties
    {
        // Extra fraction of stairwell-room smoke pushed up the shaft per cycle while active.
        public float risePower = 0.25f;

        public bool requiresPower = true;

        public CompProperties_SmokeUpdraft()
        {
            compClass = typeof(CompSmokeUpdraft);
        }
    }

    // Powered updraft filter in a stairwell room — boosts smoke convection up the shaft.
    public class CompSmokeUpdraft : ThingComp
    {
        public CompProperties_SmokeUpdraft Props => (CompProperties_SmokeUpdraft)props;

        public bool Active
        {
            get
            {
                if (!Props.requiresPower)
                {
                    return true;
                }
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<SmokeMapComponent>()?.Updrafts.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<SmokeMapComponent>()?.Updrafts.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            if (!SmokeRiseUtility.RoomContainsLevelExit(parent.GetRoom(), parent.Map))
            {
                return "Must be in a stairwell or elevator room to vent smoke upward.";
            }
            return Active
                ? "Pulling smoke up the shaft."
                : "Needs power to boost updraft.";
        }
    }
}
