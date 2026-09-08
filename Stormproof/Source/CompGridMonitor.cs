using System.Linq;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class CompProperties_GridMonitor : CompProperties
    {
        public int checkIntervalTicks = 250;
        public float lowFraction = 0.25f;
        public float criticalFraction = 0.10f;
        // Warnings re-arm once charge climbs back above this.
        public float rearmFraction = 0.35f;

        public CompProperties_GridMonitor()
        {
            compClass = typeof(CompGridMonitor);
        }
    }

    // A wall of dials for the whole power net: live production, consumption,
    // battery charge, and how long until the batteries run dry (or fill up).
    // Raises a warning when stored charge drops low and an urgent alert when
    // it goes critical, so a slow overnight drain never surprises you again.
    public class CompGridMonitor : ThingComp
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;
        private bool warnedLow;
        private bool warnedCritical;
        private bool warnedForecastLow;
        private bool warnedForecastCritical;
        private GridForecast cachedForecast;
        private int forecastCachedTick = -1;

        public CompProperties_GridMonitor Props => (CompProperties_GridMonitor)props;

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
        }

        private float StoredEnergy(PowerNet net) =>
            net.batteryComps.Sum(b => b.StoredEnergy);

        private float StorageCapacity(PowerNet net) =>
            net.batteryComps.Sum(b => b.Props.storedEnergyMax);

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.checkIntervalTicks) || !Active)
            {
                return;
            }
            PowerNet net = powerComp.PowerNet;
            if (net == null)
            {
                return;
            }
            float capacity = StorageCapacity(net);
            if (capacity <= 0f)
            {
                return;
            }
            float stored = StoredEnergy(net);
            float fraction = stored / capacity;
            if (fraction >= Props.rearmFraction)
            {
                warnedLow = false;
                warnedCritical = false;
            }
            else if (!warnedCritical && fraction < Props.criticalFraction)
            {
                warnedCritical = true;
                warnedLow = true;
                Messages.Message(
                    "Stormproof_GridMonitor_Critical".Translate(parent.LabelShort, fraction.ToStringPercent()),
                    parent, MessageTypeDefOf.NegativeEvent);
            }
            else if (!warnedLow && fraction < Props.lowFraction)
            {
                warnedLow = true;
                Messages.Message(
                    "Stormproof_GridMonitor_Low".Translate(parent.LabelShort, fraction.ToStringPercent()),
                    parent, MessageTypeDefOf.CautionInput);
            }

            RefreshForecast(net, stored, capacity);
            if (cachedForecast.HasForecaster)
            {
                if (cachedForecast.NadirFraction >= Props.rearmFraction)
                {
                    warnedForecastLow = false;
                    warnedForecastCritical = false;
                }
                else if (!warnedForecastCritical && cachedForecast.TicksToCritical >= 0
                    && fraction >= Props.criticalFraction)
                {
                    warnedForecastCritical = true;
                    warnedForecastLow = true;
                    Messages.Message(
                        "Stormproof_GridMonitor_ForecastCritical".Translate(
                            parent.LabelShort,
                            cachedForecast.TicksToCritical.ToStringTicksToPeriod()),
                        parent, MessageTypeDefOf.NegativeEvent);
                }
                else if (!warnedForecastLow && cachedForecast.TicksToLow >= 0
                    && fraction >= Props.lowFraction)
                {
                    warnedForecastLow = true;
                    Messages.Message(
                        "Stormproof_GridMonitor_ForecastLow".Translate(
                            parent.LabelShort,
                            cachedForecast.TicksToLow.ToStringTicksToPeriod()),
                        parent, MessageTypeDefOf.CautionInput);
                }
            }
        }

        private void RefreshForecast(PowerNet net, float stored, float capacity)
        {
            int tick = Find.TickManager.TicksGame;
            if (forecastCachedTick == tick)
            {
                return;
            }
            forecastCachedTick = tick;
            cachedForecast = GridForecastUtility.Project(
                parent.Map, net, GridForecastUtility.ForecasterOn(net),
                stored, capacity, Props.lowFraction, Props.criticalFraction);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref warnedLow, "stormproof_monitorWarnedLow", false);
            Scribe_Values.Look(ref warnedCritical, "stormproof_monitorWarnedCritical", false);
            Scribe_Values.Look(ref warnedForecastLow, "stormproof_monitorWarnedForecastLow", false);
            Scribe_Values.Look(ref warnedForecastCritical, "stormproof_monitorWarnedForecastCritical", false);
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Stormproof_OfflineNeedsPower".Translate();
            }
            PowerNet net = powerComp.PowerNet;
            if (net == null)
            {
                return "Stormproof_NotConnectedPowerNet".Translate();
            }
            float production = net.powerComps
                .Where(c => c.PowerOn && c.PowerOutput > 0f)
                .Sum(c => c.PowerOutput);
            float consumption = -net.powerComps
                .Where(c => c.PowerOn && c.PowerOutput < 0f)
                .Sum(c => c.PowerOutput);
            float gainWdPerTick = net.CurrentEnergyGainRate();
            float gainWatts = gainWdPerTick / CompPower.WattsToWattDaysPerTick;
            float stored = StoredEnergy(net);
            float capacity = StorageCapacity(net);

            string netGain = (gainWatts >= 0f ? "+" : "") + gainWatts.ToString("F0") + " W";
            string s = "Stormproof_GridMonitor_Header".Translate(
                production.ToString("F0"), consumption.ToString("F0"), netGain);
            if (capacity <= 0f)
            {
                return s + "\n" + "Stormproof_GridMonitor_NoBatteries".Translate();
            }
            s += "\n" + "Stormproof_GridMonitor_Stored".Translate(
                stored.ToString("F0"), capacity.ToString("F0"), (stored / capacity).ToStringPercent());
            if (gainWdPerTick < 0f && stored > 0f)
            {
                int ticksToEmpty = (int)(stored / -gainWdPerTick);
                s += "\n" + "Stormproof_GridMonitor_EmptyIn".Translate(ticksToEmpty.ToStringTicksToPeriod());
            }
            else if (gainWdPerTick > 0f && stored < capacity)
            {
                int ticksToFull = (int)((capacity - stored) / (gainWdPerTick * 0.5f));
                s += "\n" + "Stormproof_GridMonitor_FullIn".Translate(ticksToFull.ToStringTicksToPeriod());
            }
            string brownout = BrownoutUtility.InspectLine(parent);
            if (brownout != null)
            {
                s += "\n" + brownout;
            }
            RefreshForecast(net, stored, capacity);
            if (!cachedForecast.HasForecaster)
            {
                s += "\n" + "Stormproof_GridMonitor_NoForecaster".Translate();
            }
            else if (cachedForecast.TicksToEmpty >= 0)
            {
                s += "\n" + "Stormproof_GridMonitor_ForecastEmptyIn".Translate(
                    cachedForecast.TicksToEmpty.ToStringTicksToPeriod());
            }
            else if (cachedForecast.TicksToFull >= 0)
            {
                s += "\n" + "Stormproof_GridMonitor_ForecastFullIn".Translate(
                    cachedForecast.TicksToFull.ToStringTicksToPeriod());
            }
            else
            {
                s += "\n" + "Stormproof_GridMonitor_ForecastHeld".Translate(
                    cachedForecast.NadirFraction.ToStringPercent());
            }
            return s;
        }
    }
}
