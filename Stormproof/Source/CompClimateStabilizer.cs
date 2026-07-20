using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_ClimateStabilizer : CompProperties
    {
        public float activePowerConsumption = 1800f;

        public CompProperties_ClimateStabilizer()
        {
            compClass = typeof(CompClimateStabilizer);
        }
    }

    // Cancels map-wide temperature offsets from heat waves, cold snaps,
    // volcanic winter, and Stormproof extreme climate events.
    public class CompClimateStabilizer : ThingComp, IHazardDefender
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_ClimateStabilizer Props =>
            (CompProperties_ClimateStabilizer)props;

        private bool HazardActive =>
            parent.Spawned &&
            HazardProtection.AnyConditionActive(
                parent.Map,
                GameConditionDefOf.HeatWave,
                GameConditionDefOf.ColdSnap,
                GameConditionDefOf.VolcanicWinter,
                StormproofDefOf.Stormproof_HeatDome,
                StormproofDefOf.Stormproof_PolarFront,
                StormproofDefOf.DeepFreeze);

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
            StormproofRegistry.ClimateStabilizers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.ClimateStabilizers.Remove(this);
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
                return "Stormproof_ClimateStabilizer_Standby".Translate(
                    Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? "Stormproof_ClimateStabilizer_Protecting".Translate(
                    Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_ClimateStabilizer_Down".Translate();
        }
    }
}
