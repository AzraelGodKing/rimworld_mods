using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_SkyRestorer : CompProperties
    {
        public float activePowerConsumption = 1500f;

        public CompProperties_SkyRestorer()
        {
            compClass = typeof(CompSkyRestorer);
        }
    }

    // Restores usable daylight during eclipses, volcanic winters/ash, and
    // darkened skies so grow lamps and mood aren't the only counters.
    public class CompSkyRestorer : ThingComp, IHazardDefender
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_SkyRestorer Props => (CompProperties_SkyRestorer)props;

        private bool HazardActive =>
            parent.Spawned &&
            HazardProtection.AnyConditionActive(
                parent.Map,
                GameConditionDefOf.Eclipse,
                GameConditionDefOf.VolcanicWinter,
                StormproofDefOf.VolcanicAsh,
                StormproofDefOf.DarkenedSkies);

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
            StormproofRegistry.SkyRestorers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.SkyRestorers.Remove(this);
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
                return "Stormproof_SkyRestorer_Standby".Translate(
                    Props.activePowerConsumption.ToString("F0"));
            }
            return Protecting
                ? "Stormproof_SkyRestorer_Protecting".Translate(
                    Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_SkyRestorer_Down".Translate();
        }
    }
}
