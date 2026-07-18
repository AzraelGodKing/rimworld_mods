using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_GasExchanger : CompProperties
    {
        // Extra fraction of buoyant gas pushed up and heavy gas pulled down
        // the shaft each cycle while active.
        public float transferPower = 0.20f;

        public bool requiresPower = true;

        public CompProperties_GasExchanger()
        {
            compClass = typeof(CompGasExchanger);
        }
    }

    // Powered shaft gas junction — actively drives O₂ up and CO₂ down through
    // an unsealed stairwell or elevator, faster than passive stratification.
    public class CompGasExchanger : ThingComp
    {
        public CompProperties_GasExchanger Props => (CompProperties_GasExchanger)props;

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
            parent.Map.GetComponent<AtmosphereMapComponent>()?.GasExchangers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<AtmosphereMapComponent>()?.GasExchangers.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            if (!SmokeRiseUtility.RoomContainsLevelExit(parent.GetRoom(), parent.Map))
            {
                return "Strata_GasExchangerShaft".Translate();
            }
            return Active
                ? "Strata_GasExchangerActive".Translate()
                : "Strata_GasExchangerNeedsPower".Translate();
        }
    }
}
