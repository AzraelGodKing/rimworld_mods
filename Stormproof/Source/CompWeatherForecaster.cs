using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_WeatherForecaster : CompProperties
    {
        public int warningLeadTicks = 2500; // one in-game hour

        public CompProperties_WeatherForecaster()
        {
            compClass = typeof(CompWeatherForecaster);
        }
    }

    // Watches the map's weather decider and gives the player advance notice:
    // a heads-up shortly before the current weather breaks, and an alert the
    // moment a lightning-bearing storm rolls in (so spire owners can top off).
    public class CompWeatherForecaster : ThingComp
    {
        // WeatherDecider.curWeatherDuration is private; the decider rolls the
        // *next* weather only at transition time, so the honest forecast is
        // "when the current weather ends", not what comes after.
        private static readonly AccessTools.FieldRef<WeatherDecider, int> DurationRef =
            AccessTools.FieldRefAccess<WeatherDecider, int>("curWeatherDuration");

        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private WeatherDef lastSeenWeather;
        private bool warnedThisCycle;

        public CompProperties_WeatherForecaster Props => (CompProperties_WeatherForecaster)props;

        public bool Active =>
            parent.Spawned &&
            !parent.Destroyed &&
            (flickComp == null || flickComp.SwitchIsOn) &&
            powerComp != null &&
            powerComp.PowerOn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            flickComp = parent.GetComp<CompFlickable>();
            lastSeenWeather = parent.Map?.weatherManager.curWeather;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(250) || !Active)
            {
                return;
            }
            Map map = parent.Map;
            WeatherDef cur = map.weatherManager.curWeather;

            if (cur != lastSeenWeather)
            {
                lastSeenWeather = cur;
                warnedThisCycle = false;
                if (BringsLightning(cur))
                {
                    Messages.Message(
                        "Stormproof_WeatherForecaster_LightningIncoming".Translate(parent.LabelShort, cur.label),
                        parent, MessageTypeDefOf.NeutralEvent);
                }
            }

            if (!warnedThisCycle && RemainingTicks(map) <= Props.warningLeadTicks)
            {
                warnedThisCycle = true;
                Messages.Message(
                    "Stormproof_WeatherForecaster_BreakingSoon".Translate(parent.LabelShort, cur.label),
                    parent, MessageTypeDefOf.SilentInput);
            }
        }

        private int RemainingTicks(Map map)
        {
            return DurationRef(map.weatherDecider) - map.weatherManager.curWeatherAge;
        }

        public static bool BringsLightning(WeatherDef def)
        {
            return def?.eventMakers != null && def.eventMakers.Count > 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref warnedThisCycle, "stormproof_warnedThisCycle", false);
            Scribe_Defs.Look(ref lastSeenWeather, "stormproof_lastSeenWeather");
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Stormproof_OfflineNeedsPower".Translate();
            }
            Map map = parent.Map;
            WeatherDef cur = map.weatherManager.curWeather;
            int remaining = RemainingTicks(map);
            string s = "Stormproof_WeatherForecaster_Current".Translate(cur.label);
            s += remaining > 0
                ? "\n" + "Stormproof_WeatherForecaster_HoldsFor".Translate(remaining.ToStringTicksToPeriod())
                : "\n" + "Stormproof_WeatherForecaster_AboutToChange".Translate();
            return s;
        }
    }
}
