using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_DroughtCondenser : CompProperties
    {
        public float activePowerConsumption = 1200f;

        public CompProperties_DroughtCondenser()
        {
            compClass = typeof(CompDroughtCondenser);
        }
    }

    // Atmospheric moisture recovery during droughts (Odyssey). While powered,
    // outdoor plant drought penalties are cancelled map-wide.
    public class CompDroughtCondenser : ThingComp, IHazardDefender
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_DroughtCondenser Props =>
            (CompProperties_DroughtCondenser)props;

        private bool HazardActive =>
            parent.Spawned &&
            HazardProtection.AnyConditionActive(
                parent.Map,
                StormproofDefOf.Drought,
                StormproofDefOf.DroughtInitial);

        public bool Protecting =>
            HazardActive &&
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
            StormproofRegistry.DroughtCondensers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.DroughtCondensers.Remove(this);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(30) || powerComp == null)
            {
                return;
            }
            float wanted = HazardActive
                ? Props.activePowerConsumption
                : powerComp.Props.PowerConsumption;
            powerComp.PowerOutput = -wanted;
        }

        public override string CompInspectStringExtra()
        {
            if (!HazardActive)
            {
                return "Stormproof_DroughtCondenser_Standby".Translate(
                    Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? "Stormproof_DroughtCondenser_Protecting".Translate(
                    Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_DroughtCondenser_Down".Translate();
        }
    }
}
