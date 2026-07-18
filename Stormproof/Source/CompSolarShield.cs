using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_SolarShield : CompProperties
    {
        public float activePowerConsumption = 2500f;

        public CompProperties_SolarShield()
        {
            compClass = typeof(CompSolarShield);
        }
    }

    public class CompSolarShield : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_SolarShield Props => (CompProperties_SolarShield)props;

        private bool FlareActive =>
            parent.Spawned &&
            parent.Map.gameConditionManager.ConditionIsActive(StormproofDefOf.SolarFlare);

        // Protection must not depend on PowerOn: when a flare begins, the game
        // turns every trader off before we get a chance to veto it. Instead we
        // require the shield to be switched on, intact, wired to a grid that
        // still holds energy. If the grid runs dry mid-flare, protection drops
        // and the whole net goes dark until the flare ends.
        public bool Protecting =>
            FlareActive &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            (breakdownComp == null || !breakdownComp.BrokenDown) &&
            powerComp != null &&
            powerComp.PowerNet != null &&
            powerComp.PowerNet.CurrentStoredEnergy() > 1f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
            breakdownComp = parent.GetComp<CompBreakdownable>();
            StormproofRegistry.Shields.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.Shields.Remove(this);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(30) || powerComp == null)
            {
                return;
            }
            float wanted = FlareActive ? Props.activePowerConsumption : powerComp.Props.PowerConsumption;
            powerComp.PowerOutput = -wanted;
        }

        public override string CompInspectStringExtra()
        {
            if (!FlareActive)
            {
                return "Stormproof_SolarShield_Standby".Translate(Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? "Stormproof_SolarShield_Protecting".Translate(Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_SolarShield_Down".Translate();
        }
    }
}
