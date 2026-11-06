using RimWorld;
using Verse;

namespace Stormproof
{
    public abstract class CompProperties_HazardDefender : CompProperties
    {
        public float activePowerConsumption;
    }

    // Shared body of the map-wide hazard buildings (atmospheric barrier, climate
    // stabilizer, sky restorer, drought condenser). Idle nameplate draw until
    // their hazard is on and they are allowed in settings, then full active draw.
    public abstract class CompHazardDefender : ThingComp, IHazardDefender, IHazardPowerRamp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        private CompProperties_HazardDefender Props => (CompProperties_HazardDefender)props;

        protected abstract GameConditionDef[] Hazards { get; }

        protected abstract bool AllowedInSettings { get; }

        protected abstract string StandbyKey { get; }

        protected abstract string ProtectingKey { get; }

        protected abstract string DownKey { get; }

        protected abstract void Register();

        protected abstract void Unregister(Map map);

        public bool HazardActive =>
            parent.Spawned && HazardProtection.AnyConditionActive(parent.Map, Hazards);

        // Wants its active draw: hazard on and not switched off in mod settings.
        private bool Demand => AllowedInSettings && HazardActive;

        public float ForecastDrawWatts =>
            Demand ? Props.activePowerConsumption : (powerComp?.Props.PowerConsumption ?? 0f);

        public bool Protecting =>
            Demand &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            (breakdownComp == null || !breakdownComp.BrokenDown) &&
            powerComp?.PowerNet != null &&
            powerComp.PowerNet.CurrentStoredEnergy() > 1f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
            breakdownComp = parent.GetComp<CompBreakdownable>();
            Register();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            Unregister(map);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(30) || powerComp == null)
            {
                return;
            }
            powerComp.PowerOutput = -(Demand ? Props.activePowerConsumption : powerComp.Props.PowerConsumption);
        }

        public override string CompInspectStringExtra()
        {
            if (!AllowedInSettings)
            {
                return "Stormproof_DisabledInSettings".Translate();
            }
            if (!HazardActive)
            {
                return StandbyKey.Translate(Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? ProtectingKey.Translate(Props.activePowerConsumption.ToString("F0"))
                : DownKey.Translate();
        }
    }
}
