using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
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

    public class CompLoadShedder : ThingComp
    {
        private CompPower transmitterComp;
        private bool breakerClosed = true;
        private float cutoffFraction;
        private int shedMask;
        private bool scheduleEnabled;
        private bool forecastOverride;
        private ShedHoldMode holdMode = ShedHoldMode.Auto;
        private float shedSideDraw;
        private int shedSideDrawTick = -1;
        private bool shedSideDrawStale;
        private const int ShedSideDrawInterval = 2500;
        // Reused by the shed-side flood fill (every 2500 ticks); emptied after use.
        private readonly HashSet<Thing> floodVisited = new HashSet<Thing>();
        private readonly HashSet<PowerNet> floodNets = new HashSet<PowerNet>();
        private readonly List<Thing> floodStart = new List<Thing>();
        private readonly List<Thing> floodNext = new List<Thing>();
        private readonly Queue<Thing> floodQueue = new Queue<Thing>();

        public enum ShedHoldMode
        {
            Auto = 0,
            HoldRun = 1,
            HoldShed = 2,
            // Auto, but only reconnects when the forecast says the supply grid
            // won't fall back to the cutoff within the forecast window (AZR-671).
            Smart = 3
        }

        private const int SmartCacheTicks = 250;
        private int smartCheckedTick = -1;
        private int smartTicksToCutoff = -1;

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
            StormproofRegistry.LoadShedders.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            StormproofRegistry.LoadShedders.Remove(this, map);
        }

        internal float CutoffFraction => cutoffFraction;

        internal PowerNet ForecastSupplyNet => parent.Spawned && transmitterComp != null ? SupplyNet() : null;

        // Nameplate draw of the consumers this breaker cuts off when it opens.
        internal float ShedSideDraw
        {
            get
            {
                if (shedSideDrawTick < 0 && parent.Spawned)
                {
                    RefreshShedSideDraw();
                }
                return shedSideDraw;
            }
        }

        // Forecast twin of WantClosed: no storm pre-empt, since that is not predictable.
        internal bool ProjectClosed(bool wasClosed, float fraction, int hourOfDay)
        {
            return DecideClosed(wasClosed, fraction < cutoffFraction, fraction >= ReconnectFraction,
                hourOfDay, stormImminent: false, supply: null);
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
                float capacity = BatteryMath.Capacity(net);
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
            BatteryMath.Sum(net, out float stored, out float capacity);
            return capacity <= 0f ? -1f : stored / capacity;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.checkIntervalTicks) || !parent.Spawned)
            {
                return;
            }
            // Nets rebuild after a breaker flip, so a stale split waits one check.
            if (shedSideDrawStale || shedSideDrawTick < 0
                || Find.TickManager.TicksGame - shedSideDrawTick >= ShedSideDrawInterval)
            {
                RefreshShedSideDraw();
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
            bool wantClosed = DecideClosed(breakerClosed, thresholdShed, thresholdReconnect,
                GenLocalDate.HourOfDay(parent.Map), forecastOverride && StormImminent(supply), supply);
            bool quiet = wantClosed == breakerClosed || (!thresholdShed && !wantClosed);
            SetBreaker(wantClosed, quiet: quiet);
        }

        // supply is null inside a forecast projection: Smart then acts like Auto,
        // so a projection never starts another projection.
        private bool DecideClosed(bool wasClosed, bool thresholdShed, bool thresholdReconnect,
            int hourOfDay, bool stormImminent, PowerNet supply)
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
                return !wasClosed ? thresholdReconnect : true;
            }
            if (scheduleEnabled && HourSheds(hourOfDay))
            {
                return false;
            }
            if (stormImminent)
            {
                return false;
            }
            if (!wasClosed)
            {
                if (thresholdReconnect && holdMode == ShedHoldMode.Smart && supply != null)
                {
                    return !SmartKeepsShed(supply);
                }
                return thresholdReconnect;
            }
            return true;
        }

        // Projects the supply grid as if we reconnect now. If it would sink back
        // under the cutoff inside the forecast window, stay shed. Needs a
        // weather forecaster on the supply grid; without one, Smart is Auto.
        private bool SmartKeepsShed(PowerNet supply)
        {
            int now = Find.TickManager.TicksGame;
            if (smartCheckedTick >= 0 && now - smartCheckedTick < SmartCacheTicks)
            {
                return smartTicksToCutoff >= 0;
            }
            smartCheckedTick = now;
            smartTicksToCutoff = -1;
            CompWeatherForecaster forecaster = GridForecastUtility.ForecasterOn(supply);
            if (forecaster == null)
            {
                return false;
            }
            BatteryMath.Sum(supply, out float stored, out float capacity);
            if (capacity <= 0f)
            {
                return false;
            }
            GridForecast forecast = GridForecastUtility.Project(parent.Map, supply, forecaster,
                stored, capacity, cutoffFraction, cutoffFraction);
            smartTicksToCutoff = forecast.TicksToCritical;
            return smartTicksToCutoff >= 0;
        }

        private void RefreshShedSideDraw()
        {
            shedSideDrawTick = Find.TickManager.TicksGame;
            shedSideDrawStale = false;
            shedSideDraw = breakerClosed ? ClosedShedSideDraw() : OpenShedSideDraw();
        }

        // Open: the shed side is every neighbouring net that isn't the supply.
        private float OpenShedSideDraw()
        {
            PowerNet supply = SupplyNet();
            float draw = 0f;
            HashSet<PowerNet> seen = floodNets;
            seen.Clear();
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }
                PowerNet net = parent.Map.powerNetGrid.TransmittedPowerNetAt(cell);
                if (net == null || net == supply || !seen.Add(net))
                {
                    continue;
                }
                for (int i = 0; i < net.powerComps.Count; i++)
                {
                    draw += ConsumerDraw(net.powerComps[i], requirePowered: false);
                }
            }
            seen.Clear();
            return draw;
        }

        // Closed: both sides share one net, so split it by flooding transmitters
        // from each neighbour without crossing this breaker. The side with the
        // most battery capacity is the supply (same rule as SupplyNet).
        // Three or more sides (AZR-669): when the breaker opens, every non-supply
        // side drops off the supply grid together, so their draws add up to the
        // shed draw. Sides joined by conduit elsewhere flood as one side (their
        // second neighbour finds its transmitters already visited), so a loop is
        // never counted twice.
        private float ClosedShedSideDraw()
        {
            Map map = parent.Map;
            HashSet<Thing> visited = floodVisited;
            visited.Clear();
            visited.Add(parent);
            int sides = 0;
            float total = 0f;
            float supplyDraw = 0f;
            float supplyCapacity = -1f;
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> start = floodStart;
                start.Clear();
                AddTransmittersAt(map, cell, visited, start);
                if (start.Count == 0)
                {
                    continue;
                }
                FloodSide(map, start, visited, out float capacity, out float draw);
                sides++;
                total += draw;
                if (capacity > supplyCapacity)
                {
                    supplyCapacity = capacity;
                    supplyDraw = draw;
                }
            }
            visited.Clear();
            floodStart.Clear();
            return sides < 2 ? 0f : total - supplyDraw;
        }

        private void FloodSide(Map map, List<Thing> start,
            HashSet<Thing> visited, out float capacity, out float draw)
        {
            capacity = 0f;
            draw = 0f;
            Queue<Thing> queue = floodQueue;
            queue.Clear();
            for (int i = 0; i < start.Count; i++)
            {
                queue.Enqueue(start[i]);
            }
            List<Thing> next = floodNext;
            while (queue.Count > 0)
            {
                Thing t = queue.Dequeue();
                CompPowerBattery battery = t.TryGetComp<CompPowerBattery>();
                if (battery != null)
                {
                    capacity += battery.Props.storedEnergyMax;
                }
                CompPower power = t.TryGetComp<CompPower>();
                draw += ConsumerDraw(power, requirePowered: true);
                if (power?.connectChildren != null)
                {
                    for (int i = 0; i < power.connectChildren.Count; i++)
                    {
                        draw += ConsumerDraw(power.connectChildren[i], requirePowered: true);
                    }
                }
                foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(t))
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    next.Clear();
                    AddTransmittersAt(map, cell, visited, next);
                    for (int i = 0; i < next.Count; i++)
                    {
                        queue.Enqueue(next[i]);
                    }
                }
            }
        }

        private static void AddTransmittersAt(Map map, IntVec3 cell,
            HashSet<Thing> visited, List<Thing> into)
        {
            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                CompPower power = t.TryGetComp<CompPower>();
                if (power != null && power.TransmitsPowerNow && visited.Add(t))
                {
                    into.Add(t);
                }
            }
        }

        private static float ConsumerDraw(CompPower power, bool requirePowered)
        {
            if (!(power is CompPowerTrader trader) || trader.parent == null
                || trader.Props.PowerConsumption <= 0f)
            {
                return 0f;
            }
            if (requirePowered ? !trader.PowerOn : !FlickedOn(trader.parent))
            {
                return 0f;
            }
            return GridForecastUtility.ConsumerForecastDraw(trader.parent, trader.Props.PowerConsumption);
        }

        private static bool FlickedOn(ThingWithComps thing)
        {
            CompFlickable flick = thing.GetComp<CompFlickable>();
            return flick == null || flick.SwitchIsOn;
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
            int remaining = forecast.RemainingTicks();
            return remaining >= 0 && remaining <= forecast.Props.warningLeadTicks;
        }

        private void SetBreaker(bool closed, bool quiet = false)
        {
            if (breakerClosed == closed)
            {
                return;
            }
            breakerClosed = closed;
            shedSideDrawStale = true;
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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_CutoffLowerLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_CutoffLowerDesc".Translate(),
                icon = StormproofTex.CutoffLower,
                action = () => cutoffFraction = Mathf.Max(0.05f, cutoffFraction - 0.05f),
            };
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_CutoffRaiseLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_CutoffRaiseDesc".Translate(),
                icon = StormproofTex.CutoffRaise,
                action = () => cutoffFraction = Mathf.Min(0.45f, cutoffFraction + 0.05f),
            };
            yield return new Command_Action
            {
                defaultLabel = "Stormproof_LoadShedder_ScheduleLabel".Translate(),
                defaultDesc = "Stormproof_LoadShedder_ScheduleDesc".Translate(),
                icon = StormproofTex.Schedule,
                action = () => Find.WindowStack.Add(new Dialog_LoadSchedule(this)),
            };
            yield return new Command_Action
            {
                defaultLabel = HoldLabel(),
                defaultDesc = "Stormproof_LoadShedder_HoldDesc".Translate(),
                icon = StormproofTex.Hold,
                defaultIconColor = HoldColor(),
                action = () =>
                {
                    holdMode = NextHold(holdMode);
                    smartCheckedTick = -1;
                },
            };
        }

        // Auto -> Smart -> Run -> Shed -> Auto.
        private static ShedHoldMode NextHold(ShedHoldMode mode)
        {
            switch (mode)
            {
                case ShedHoldMode.Auto:
                    return ShedHoldMode.Smart;
                case ShedHoldMode.Smart:
                    return ShedHoldMode.HoldRun;
                case ShedHoldMode.HoldRun:
                    return ShedHoldMode.HoldShed;
                default:
                    return ShedHoldMode.Auto;
            }
        }

        private Color HoldColor()
        {
            switch (holdMode)
            {
                case ShedHoldMode.Smart:
                    return new Color(0.55f, 0.85f, 1f);
                case ShedHoldMode.HoldRun:
                    return new Color(0.55f, 1f, 0.55f);
                case ShedHoldMode.HoldShed:
                    return new Color(1f, 0.5f, 0.45f);
                default:
                    return Color.white;
            }
        }

        private string HoldLabel()
        {
            switch (holdMode)
            {
                case ShedHoldMode.Smart:
                    return "Stormproof_LoadShedder_HoldSmart".Translate();
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
            if (holdMode == ShedHoldMode.Smart && !breakerClosed && smartTicksToCutoff >= 0)
            {
                state += "\n" + "Stormproof_LoadShedder_SmartHolding".Translate(
                    smartTicksToCutoff.ToStringTicksToPeriod());
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
