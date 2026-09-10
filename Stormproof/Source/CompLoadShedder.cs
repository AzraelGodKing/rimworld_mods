using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    [StaticConstructorOnStartup]
    internal static class LoadShedderTex
    {
        public static readonly Texture2D CutoffLower = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
        public static readonly Texture2D CutoffRaise = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
        public static readonly Texture2D Schedule = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
    }

    // The breaker building itself only reports whether it currently transmits;
    // all the decision-making lives in CompLoadShedder.
    public class Building_LoadShedder : Building
    {
        private CompLoadShedder shedderComp;

        public override bool TransmitsPowerNow =>
            shedderComp == null || shedderComp.BreakerClosed;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            shedderComp = GetComp<CompLoadShedder>();
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }

    public class CompProperties_LoadShedder : CompProperties
    {
        public int checkIntervalTicks = 60;
        public float defaultCutoffFraction = 0.20f;
        // Reconnect this far above the cutoff so the breaker doesn't chatter.
        public float reconnectMargin = 0.20f;

        public CompProperties_LoadShedder()
        {
            compClass = typeof(CompLoadShedder);
        }
    }

    // An automatic breaker: wire it between the main grid and a low-priority
    // sub-grid. While the supply side's batteries hold charge it transmits like
    // a conduit; when they fall below the cutoff it trips open, shedding the
    // sub-grid so life support outlasts the lamps. Reconnects on its own once
    // the batteries recover.
    public class CompLoadShedder : ThingComp
    {
        private CompPower transmitterComp;
        private bool breakerClosed = true;
        private float cutoffFraction;
        private int shedMask;
        private bool scheduleEnabled;
        private bool forecastOverride;
        private ShedHoldMode holdMode = ShedHoldMode.Auto;

        public enum ShedHoldMode
        {
            Auto = 0,
            HoldRun = 1,
            HoldShed = 2
        }

        public CompProperties_LoadShedder Props => (CompProperties_LoadShedder)props;

        public bool BreakerClosed => breakerClosed;

        public bool ForecastOverride
        {
            get => forecastOverride;
            set => forecastOverride = value;
        }

        public bool HourSheds(int hour) => (shedMask & (1 << hour)) != 0;

        public void ToggleHour(int hour)
        {
            shedMask ^= 1 << hour;
            scheduleEnabled = true;
        }

        public void ClearSchedule()
        {
            shedMask = 0;
            scheduleEnabled = false;
        }

        private float ReconnectFraction =>
            Mathf.Min(cutoffFraction + Props.reconnectMargin, 0.95f);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            cutoffFraction = Props.defaultCutoffFraction;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            transmitterComp = parent.GetComp<CompPower>();
        }

        // The grid we watch. Closed: our own net (both sides are one net).
        // Open: we belong to no net, so of the nets touching our cardinal
        // neighbors, the one with the most battery capacity is the supply.
        private PowerNet SupplyNet()
        {
            if (breakerClosed && transmitterComp.PowerNet != null)
            {
                return transmitterComp.PowerNet;
            }
            PowerNet best = null;
            float bestCapacity = -1f;
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }
                PowerNet net = parent.Map.powerNetGrid.TransmittedPowerNetAt(cell);
                if (net == null || net == best)
                {
                    continue;
                }
                float capacity = net.batteryComps.Sum(b => b.Props.storedEnergyMax);
                if (capacity > bestCapacity)
                {
                    bestCapacity = capacity;
                    best = net;
                }
            }
            return best;
        }

        private static float StoredFraction(PowerNet net)
        {
            float capacity = net.batteryComps.Sum(b => b.Props.storedEnergyMax);
            return capacity <= 0f
                ? -1f
                : net.batteryComps.Sum(b => b.StoredEnergy) / capacity;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.checkIntervalTicks) || !parent.Spawned)
            {
                return;
            }
            PowerNet supply = SupplyNet();
            if (supply == null)
            {
                SetBreaker(true, quiet: true);
                return;
            }
            float fraction = StoredFraction(supply);
            if (fraction < 0f)
            {
                // No batteries anywhere: nothing to protect, act like a conduit.
                SetBreaker(true, quiet: true);
                return;
            }

            bool thresholdShed = fraction < cutoffFraction;
            bool thresholdReconnect = fraction >= ReconnectFraction;
            bool wantClosed = WantClosed(supply, thresholdShed, thresholdReconnect);
            bool quiet = wantClosed == breakerClosed || (!thresholdShed && !wantClosed);
            SetBreaker(wantClosed, quiet: quiet);
        }

        private bool WantClosed(PowerNet supply, bool thresholdShed, bool thresholdReconnect)
        {
            if (thresholdShed)
            {
                return false;
            }
            if (holdMode == ShedHoldMode.HoldShed)
            {
                return false;
            }
            if (holdMode == ShedHoldMode.HoldRun)
            {
                return !breakerClosed ? thresholdReconnect : true;
            }
            if (scheduleEnabled && HourSheds(GenLocalDate.HourOfDay(parent.Map)))
            {
                return false;
            }
            if (forecastOverride && StormImminent(supply))
            {
                return false;
            }
            if (!breakerClosed)
            {
                return thresholdReconnect;
            }
            return true;
        }

        private bool StormImminent(PowerNet supply)
        {
            Map map = parent.Map;
            if (map == null)
            {
                return false;
            }
            if (HazardProtection.ConditionActive(map, StormproofDefOf.SolarFlare)
                || HazardProtection.ConditionActive(map, StormproofDefOf.Stormproof_IonStorm)
                || HazardProtection.ConditionActive(map, StormproofDefOf.Stormproof_DryLightning)
                || HazardProtection.ConditionActive(map, GameConditionDefOf.Flashstorm))
            {
                return true;
            }
            if (CompWeatherForecaster.BringsLightning(map.weatherManager.curWeather))
            {
                return true;
            }
            CompWeatherForecaster forecast = GridForecastUtility.ForecasterOn(supply);
            if (forecast == null || !forecast.Active)
            {
                return false;
            }
            // WeatherDecider only names the *next* weather at the transition, so
            // the honest pre-empt is "current weather is about to break".
            return forecast.RemainingTicks() <= forecast.Props.warningLeadTicks;
        }

        private void SetBreaker(bool closed, bool quiet = false)
        {
            if (breakerClosed == closed)
            {
                return;
            }
            breakerClosed = closed;
            if (parent.Spawned)
            {
                parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(transmitterComp);
                FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);
                if (!quiet)
                {
                    Messages.Message(
                        closed
                            ? "Stormproof_LoadShedder_Reconnected".Translate(parent.LabelShort)
                            : "Stormproof_LoadShedder_Shed".Translate(parent.LabelShort, cutoffFraction.ToStringPercent()),
                        parent,
                        closed ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.CautionInput);
                }
            }
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_CutoffLowerLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_CutoffLowerDesc".Translate(),
                icon = LoadShedderTex.CutoffLower,
                action = () => cutoffFraction = Mathf.Max(0.05f, cutoffFraction - 0.05f),
            };
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_CutoffRaiseLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_CutoffRaiseDesc".Translate(),
                icon = LoadShedderTex.CutoffRaise,
                action = () => cutoffFraction = Mathf.Min(0.45f, cutoffFraction + 0.05f),
            };
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_ScheduleLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_ScheduleDesc".Translate(),
                icon = LoadShedderTex.Schedule,
                action = () => Find.WindowStack.Add(new Dialog_LoadSchedule(this)),
            };
            yield return new Command_Action
            {
                defaultLabel = HoldLabel(),
                defaultDesc = "Stormproof_LoadShedder_HoldDesc".Translate(),
                icon = LoadShedderTex.CutoffLower,
                action = () =>
                {
                    holdMode = holdMode == ShedHoldMode.Auto
                        ? ShedHoldMode.HoldRun
                        : holdMode == ShedHoldMode.HoldRun
                            ? ShedHoldMode.HoldShed
                            : ShedHoldMode.Auto;
                },
            };
        }

        private string HoldLabel()
        {
            switch (holdMode)
            {
                case ShedHoldMode.HoldRun:
                    return "Stormproof_LoadShedder_HoldRun".Translate();
                case ShedHoldMode.HoldShed:
                    return "Stormproof_LoadShedder_HoldShed".Translate();
                default:
                    return "Stormproof_LoadShedder_HoldAuto".Translate();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref breakerClosed, "stormproof_breakerClosed", true);
            Scribe_Values.Look(ref cutoffFraction, "stormproof_cutoffFraction", 0.20f);
            Scribe_Values.Look(ref shedMask, "stormproof_shedMask", 0);
            Scribe_Values.Look(ref scheduleEnabled, "stormproof_scheduleEnabled", false);
            Scribe_Values.Look(ref forecastOverride, "stormproof_forecastOverride", false);
            Scribe_Values.Look(ref holdMode, "stormproof_holdMode", ShedHoldMode.Auto);
        }

        public override string CompInspectStringExtra()
        {
            string state = breakerClosed
                ? "Stormproof_LoadShedder_Closed".Translate()
                : "Stormproof_LoadShedder_Open".Translate();
            PowerNet supply = parent.Spawned ? SupplyNet() : null;
            if (supply != null)
            {
                float fraction = StoredFraction(supply);
                state += fraction < 0f
                    ? "\n" + "Stormproof_LoadShedder_NoSupplyBatteries".Translate()
                    : "\n" + "Stormproof_LoadShedder_SupplyStatus".Translate(
                        fraction.ToStringPercent(),
                        cutoffFraction.ToStringPercent(),
                        ReconnectFraction.ToStringPercent());
            }
            if (scheduleEnabled)
            {
                state += "\n" + "Stormproof_LoadShedder_ScheduleStatus".Translate(
                    HourSheds(parent.Spawned ? GenLocalDate.HourOfDay(parent.Map) : 0)
                        ? "Stormproof_LoadShedder_HourShed".Translate()
                        : "Stormproof_LoadShedder_HourRun".Translate());
            }
            if (forecastOverride)
            {
                state += "\n" + "Stormproof_LoadShedder_ForecastOverride".Translate();
            }
            if (holdMode != ShedHoldMode.Auto)
            {
                state += "\n" + HoldLabel();
            }
            string brownout = BrownoutUtility.InspectLine(parent);
            if (brownout != null)
            {
                state += "\n" + brownout;
            }
            return state;
        }
    }
}
