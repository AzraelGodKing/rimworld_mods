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

    // Local firefighting emitter. Always snuffs fires in radius while powered;
    // during flashstorms it draws hard and pairs with storm spires.
    public class CompFireSuppressor : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private CompBreakdownable breakdownComp;
        private static readonly List<Thing> fireBuffer = new List<Thing>();

        public CompProperties_FireSuppressor Props =>
            (CompProperties_FireSuppressor)props;

        private bool HighDemand =>
            parent.Spawned &&
            (HazardProtection.ConditionActive(parent.Map, GameConditionDefOf.Flashstorm) ||
             HazardProtection.ConditionActive(parent.Map, StormproofDefOf.Stormproof_DryLightning));

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            (StormproofMod.Settings == null || StormproofMod.Settings.allowFireSuppressor) &&
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
            StormproofRegistry.FireSuppressors.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.FireSuppressors.Remove(this);
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
            fireBuffer.Clear();
            fireBuffer.AddRange(map.listerThings.ThingsOfDef(ThingDefOf.Fire));
            float radius = Props.suppressRadius;
            IntVec3 origin = parent.Position;
            for (int i = 0; i < fireBuffer.Count; i++)
            {
                Thing fire = fireBuffer[i];
                if (fire.Destroyed || fire.Map != map)
                {
                    continue;
                }
                if (fire.Position.DistanceTo(origin) <= radius)
                {
                    fire.Destroy(DestroyMode.Vanish);
                }
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
