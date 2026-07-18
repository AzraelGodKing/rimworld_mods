using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class CompProperties_StormCaller : CompProperties
    {
        public int cooldownTicks = 300000;     // five in-game days
        public int stormDurationTicks = 30000; // half an in-game day

        public CompProperties_StormCaller()
        {
            compClass = typeof(CompStormCaller);
        }
    }

    // An ionospheric agitator: dumps charge into the sky and drags a rainy
    // thunderstorm over the map on demand. Feed for storm spires, douse for
    // wildfires - on a long cooldown so it can't replace a real power grid.
    public class CompStormCaller : ThingComp
    {
        private static readonly AccessTools.FieldRef<WeatherDecider, int> DurationRef =
            AccessTools.FieldRefAccess<WeatherDecider, int>("curWeatherDuration");

        private CompPowerTrader powerComp;
        private int lastCallTick = -999999;

        public CompProperties_StormCaller Props => (CompProperties_StormCaller)props;

        public bool Powered => powerComp != null && powerComp.PowerOn;

        public int CooldownRemaining =>
            Mathf.Max(0, lastCallTick + Props.cooldownTicks - Find.TickManager.TicksGame);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "Stormproof_StormCaller_Label".Translate(),
                defaultDesc = "Stormproof_StormCaller_Desc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Stormproof/Buildings/StormCaller"),
                action = CallStorm,
            };
            if (!Powered)
            {
                cmd.Disable("Stormproof_StormCaller_NoPower".Translate());
            }
            else if (CooldownRemaining > 0)
            {
                cmd.Disable("Stormproof_RechargingReadyIn".Translate(CooldownRemaining.ToStringTicksToPeriod()));
            }
            yield return cmd;
        }

        private void CallStorm()
        {
            Map map = parent.Map;
            map.weatherManager.TransitionTo(StormproofDefOf.RainyThunderstorm);
            map.weatherManager.curWeatherAge = 0;
            DurationRef(map.weatherDecider) = Props.stormDurationTicks;
            lastCallTick = Find.TickManager.TicksGame;
            FleckMaker.ThrowLightningGlow(parent.DrawPos, map, 3f);
            Messages.Message(
                "Stormproof_StormCaller_Discharging".Translate(parent.LabelShort),
                parent, MessageTypeDefOf.NeutralEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastCallTick, "stormproof_lastCallTick", -999999);
        }

        public override string CompInspectStringExtra()
        {
            if (!Powered)
            {
                return "Stormproof_OfflineNeedsPower".Translate();
            }
            return CooldownRemaining > 0
                ? "Stormproof_RechargingReadyIn".Translate(CooldownRemaining.ToStringTicksToPeriod())
                : "Stormproof_StormCaller_Charged".Translate();
        }
    }
}
