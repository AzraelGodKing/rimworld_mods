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
                defaultLabel = "Call storm",
                defaultDesc = "Agitate the ionosphere to summon a rainy thunderstorm over this map. " +
                              "Lightning feeds storm spires and the rain douses fires. " +
                              "Recharges over five days.",
                icon = ContentFinder<Texture2D>.Get("Stormproof/Buildings/StormCaller"),
                action = CallStorm,
            };
            if (!Powered)
            {
                cmd.Disable("No power.");
            }
            else if (CooldownRemaining > 0)
            {
                cmd.Disable("Recharging: ready in " + CooldownRemaining.ToStringTicksToPeriod() + ".");
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
                parent.LabelShort + " discharges into the sky. A thunderstorm is forming.",
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
                return "Offline: needs power.";
            }
            return CooldownRemaining > 0
                ? "Recharging: ready in " + CooldownRemaining.ToStringTicksToPeriod() + "."
                : "Charged: ready to call a storm.";
        }
    }
}
