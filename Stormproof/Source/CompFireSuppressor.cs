using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_FireSuppressor : CompProperties
    {
        public float suppressRadius = 24.9f;
        public float activePowerConsumption = 800f;

        public CompProperties_FireSuppressor()
        {
            compClass = typeof(CompFireSuppressor);
        }
    }

    public class CompFireSuppressor : ThingComp, IHazardPowerRamp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;

        public CompProperties_FireSuppressor Props =>
            (CompProperties_FireSuppressor)props;

        private bool HighDemand =>
            parent.Spawned &&
            (HazardProtection.ConditionActive(parent.Map, GameConditionDefOf.Flashstorm) ||
             HazardProtection.ConditionActive(parent.Map, StormproofDefOf.Stormproof_DryLightning));

        public float ForecastDrawWatts =>
            HighDemand ? Props.activePowerConsumption : (powerComp?.Props.PowerConsumption ?? 0f);

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            StormproofMod.Settings.allowFireSuppressor &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            (breakdownComp == null || !breakdownComp.BrokenDown) &&
            powerComp != null &&
            powerComp.PowerOn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
            breakdownComp = parent.GetComp<CompBreakdownable>();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (powerComp != null && parent.IsHashIntervalTick(30))
            {
                float wanted = HighDemand
                    ? Props.activePowerConsumption
                    : powerComp.Props.PowerConsumption;
                powerComp.PowerOutput = -wanted;
            }

            if (!parent.IsHashIntervalTick(60) || !Active)
            {
                return;
            }

            Map map = parent.Map;
            float radiusSq = Props.suppressRadius * Props.suppressRadius;
            IntVec3 origin = parent.Position;
            MapComponent_Stormproof ledger = null;
            // Walk the live lister backwards: destroying a fire removes only that
            // entry, so no snapshot buffer is needed.
            List<Thing> fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            for (int i = fires.Count - 1; i >= 0; i--)
            {
                if (i >= fires.Count)
                {
                    continue;
                }
                Thing fire = fires[i];
                if (fire.Destroyed || (fire.Position - origin).LengthHorizontalSquared > radiusSq)
                {
                    continue;
                }
                fire.Destroy(DestroyMode.Vanish);
                ledger = ledger ?? map.GetComponent<MapComponent_Stormproof>();
                ledger?.NoteFireSnuffed();
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Stormproof_OfflineNeedsPower".Translate();
            }
            return HighDemand
                ? "Stormproof_FireSuppressor_Flashstorm".Translate(
                    Props.activePowerConsumption.ToString("F0"))
                : "Stormproof_FireSuppressor_Active".Translate(
                    Props.suppressRadius.ToString("F0"));
        }
    }
}
