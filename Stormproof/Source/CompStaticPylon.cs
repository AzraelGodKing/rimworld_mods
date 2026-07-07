using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_StaticPylon : CompProperties
    {
        public float dischargeRadius = 6.9f;
        public int dischargeIntervalTicks = 180; // every 3 seconds
        public int stunTicks = 120;              // 2 second stun

        public CompProperties_StaticPylon()
        {
            compClass = typeof(CompStaticPylon);
        }
    }

    // While a thunderstorm rages, the pylon bleeds ambient static into anything
    // hostile nearby, periodically stunning raiders and mechanoids. Clear
    // weather leaves it inert - it needs a charged sky to work with.
    public class CompStaticPylon : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;

        public CompProperties_StaticPylon Props => (CompProperties_StaticPylon)props;

        public bool StormActive =>
            parent.Spawned &&
            CompWeatherForecaster.BringsLightning(parent.Map.weatherManager.curWeather);

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            powerComp != null &&
            powerComp.PowerOn &&
            StormActive;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.dischargeIntervalTicks) || !Active)
            {
                return;
            }
            Map map = parent.Map;
            bool discharged = false;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!pawn.HostileTo(Faction.OfPlayer) || pawn.Dead)
                {
                    continue;
                }
                if (pawn.Position.DistanceTo(parent.Position) > Props.dischargeRadius)
                {
                    continue;
                }
                pawn.stances?.stunner?.StunFor(Props.stunTicks, parent, addBattleLog: false);
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, map);
                discharged = true;
            }
            if (discharged)
            {
                FleckMaker.ThrowLightningGlow(parent.DrawPos, map, 1.2f);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (powerComp == null || !powerComp.PowerOn || (flickComp != null && !flickComp.SwitchIsOn))
            {
                return "Offline: needs power.";
            }
            return StormActive
                ? "DISCHARGING: hostiles nearby are being static-shocked."
                : "Standby: needs an active thunderstorm to draw charge from.";
        }
    }
}
