using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_AtmosphericBarrier : CompProperties
    {
        public float activePowerConsumption = 2000f;

        public CompProperties_AtmosphericBarrier()
        {
            compClass = typeof(CompAtmosphericBarrier);
        }
    }

    // Map-wide toxic / ash / haze hardener. While powered through a fallout or
    // similar atmospheric poison event, outdoor toxic damage and cell corrosion
    // are suppressed for the whole map.
    public class CompAtmosphericBarrier : ThingComp, IHazardDefender
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_AtmosphericBarrier Props =>
            (CompProperties_AtmosphericBarrier)props;

        private bool HazardActive =>
            parent.Spawned &&
            HazardProtection.AnyConditionActive(
                parent.Map,
                GameConditionDefOf.ToxicFallout,
                StormproofDefOf.Stormproof_ToxicSurge,
                StormproofDefOf.NoxiousHaze,
                StormproofDefOf.VolcanicAsh);

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
            StormproofRegistry.AtmosphericBarriers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.AtmosphericBarriers.Remove(this);
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
                return "Stormproof_AtmosphericBarrier_Standby".Translate(
                    Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? "Stormproof_AtmosphericBarrier_Protecting".Translate(
                    Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_AtmosphericBarrier_Down".Translate();
        }
    }
}
