using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    internal struct GridForecast
    {
        public bool HasForecaster;
        public int WeatherRemaining;
        public int TicksToEmpty;
        public int TicksToFull;
        public int TicksToLow;
        public int TicksToCritical;
        public float NadirFraction;
        public float NadirBrownout;
    }

    // Crosses the weather forecaster with live solar/wind so the monitor's
    // empty-in line is not "assume this sky forever". Cached; no per-tick work.
    internal static class GridForecastUtility
    {
        internal const int HorizonTicks = 2500 * 8;
        internal const int StepTicks = 250;
        internal const int CurveSteps = HorizonTicks / StepTicks;
        internal const float BrownoutStart = 0.40f;
        private const float AfterWeatherSkyMul = 0.9f;
        private const float BrownoutDrawCut = 0.40f;

        private struct ShedderSim
        {
            public CompLoadShedder Shedder;
            public float SideDraw;
            public bool StartClosed;
            public bool Closed;
        }

        private static readonly List<ShedderSim> shedderSims = new List<ShedderSim>();

        internal static CompWeatherForecaster ForecasterOn(PowerNet net)
        {
            if (net?.powerComps == null)
            {
                return null;
            }

            for (int i = 0; i < net.powerComps.Count; i++)
            {
                CompPowerTrader trader = net.powerComps[i];
                CompWeatherForecaster forecast = trader?.parent?.GetComp<CompWeatherForecaster>();
                if (forecast != null && forecast.Active)
                {
                    return forecast;
                }
            }

            return null;
        }

        // curve (optional, CurveSteps long) receives the charge fraction at the end of each step.
        internal static GridForecast Project(Map map, PowerNet net, CompWeatherForecaster forecast,
            float stored, float capacity, float lowFraction, float criticalFraction, float[] curve = null)
        {
            float severity = StormproofMod.Settings != null && StormproofMod.Settings.enableBrownout
                ? Mathf.Clamp01(StormproofMod.Settings.brownoutSeverity)
                : 0f;
            float startFraction = capacity <= 0f ? 0f : stored / capacity;
            var result = new GridForecast
            {
                HasForecaster = forecast != null,
                WeatherRemaining = forecast != null ? forecast.RemainingTicks() : 0,
                TicksToEmpty = -1,
                TicksToFull = -1,
                TicksToLow = -1,
                TicksToCritical = -1,
                NadirFraction = startFraction,
                NadirBrownout = BrownoutAt(startFraction, severity)
            };

            if (map == null || net == null || capacity <= 0f)
            {
                return result;
            }

            SplitPlants(net, out float solarMax, out float windNow, out float windMax,
                out float otherProd, out float nameplateDraw);
            float weatherMul = WeatherSkyMul(map);
            float windFrac = windMax > 0.01f ? Mathf.Clamp01(windNow / windMax) : 0f;
            float energy = stored;
            float nadir = stored;
            bool canFill = stored < capacity - 0.05f;
            CollectShedders(map, net);
            long ticksAbs = Find.TickManager.TicksAbs;
            float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;

            for (int elapsed = 0; elapsed < HorizonTicks; elapsed += StepTicks)
            {
                bool holds = forecast != null && elapsed < result.WeatherRemaining;
                float sky = ProjectedSky(map, elapsed, holds, weatherMul);
                float wind = holds ? windFrac : windFrac * 0.55f;
                float startFrac = energy / capacity;
                float shedDelta = StepShedders(startFrac, GenDate.HourOfDay(ticksAbs + elapsed, longitude));
                float brownout = BrownoutAt(startFrac, severity);
                float draw = Mathf.Max(0f, nameplateDraw + shedDelta) * (1f - BrownoutDrawCut * brownout);
                float watts = solarMax * sky + windMax * wind + otherProd - draw;
                energy = Mathf.Clamp(energy + watts * CompPower.WattsToWattDaysPerTick * StepTicks, 0f, capacity);
                if (energy < nadir)
                {
                    nadir = energy;
                }

                float fraction = energy / capacity;
                int at = elapsed + StepTicks;
                if (curve != null)
                {
                    curve[elapsed / StepTicks] = fraction;
                }
                if (result.TicksToCritical < 0 && fraction < criticalFraction)
                {
                    result.TicksToCritical = at;
                }
                if (result.TicksToLow < 0 && fraction < lowFraction)
                {
                    result.TicksToLow = at;
                }
                if (result.TicksToEmpty < 0 && energy <= 0.05f)
                {
                    result.TicksToEmpty = at;
                    if (curve != null)
                    {
                        for (int i = elapsed / StepTicks + 1; i < CurveSteps; i++)
                        {
                            curve[i] = 0f;
                        }
                    }
                    break;
                }
                if (canFill && result.TicksToFull < 0 && energy >= capacity - 0.05f && watts > 0f)
                {
                    result.TicksToFull = at;
                }
            }

            result.NadirFraction = nadir / capacity;
            result.NadirBrownout = BrownoutAt(result.NadirFraction, severity);
            return result;
        }

        // Fills curve and returns the projection for any battery-backed net; false without batteries.
        internal static bool TryProjectNet(Map map, PowerNet net, float[] curve, out GridForecast forecast)
        {
            forecast = default;
            if (map == null || net?.batteryComps == null)
            {
                return false;
            }
            float stored = 0f;
            float capacity = 0f;
            for (int i = 0; i < net.batteryComps.Count; i++)
            {
                stored += net.batteryComps[i].StoredEnergy;
                capacity += net.batteryComps[i].Props.storedEnergyMax;
            }
            if (capacity <= 0f)
            {
                return false;
            }
            forecast = Project(map, net, ForecasterOn(net), stored, capacity, 0.25f, 0.10f, curve);
            return true;
        }

        private static void CollectShedders(Map map, PowerNet net)
        {
            shedderSims.Clear();
            foreach (CompLoadShedder shedder in StormproofRegistry.On(StormproofRegistry.LoadShedders, map))
            {
                if (shedder.ForecastSupplyNet != net)
                {
                    continue;
                }
                float side = shedder.ShedSideDraw;
                if (side <= 0f)
                {
                    continue;
                }
                shedderSims.Add(new ShedderSim
                {
                    Shedder = shedder,
                    SideDraw = side,
                    StartClosed = shedder.BreakerClosed,
                    Closed = shedder.BreakerClosed
                });
            }
        }

        // Watts to add to today's draw: a shed sub-grid leaves, a reconnecting one rejoins.
        private static float StepShedders(float fraction, int hourOfDay)
        {
            float delta = 0f;
            for (int i = 0; i < shedderSims.Count; i++)
            {
                ShedderSim sim = shedderSims[i];
                sim.Closed = sim.Shedder.ProjectClosed(sim.Closed, fraction, hourOfDay);
                shedderSims[i] = sim;
                if (sim.Closed != sim.StartClosed)
                {
                    delta += sim.Closed ? sim.SideDraw : -sim.SideDraw;
                }
            }
            return delta;
        }

        internal static float BrownoutAt(float fraction, float severity)
        {
            if (severity <= 0f || fraction >= BrownoutStart)
            {
                return 0f;
            }
            return ((BrownoutStart - fraction) / BrownoutStart) * severity;
        }

        private static void SplitPlants(PowerNet net,
            out float solarMax, out float windNow, out float windMax,
            out float otherProd, out float nameplateDraw)
        {
            solarMax = 0f;
            windNow = 0f;
            windMax = 0f;
            otherProd = 0f;
            nameplateDraw = 0f;

            for (int i = 0; i < net.powerComps.Count; i++)
            {
                CompPowerTrader trader = net.powerComps[i];
                if (trader == null || !trader.PowerOn || trader.parent == null)
                {
                    continue;
                }

                float nameplate = Mathf.Abs(trader.Props.PowerConsumption);
                if (trader.parent.GetComp<CompPowerPlantSolar>() != null)
                {
                    solarMax += nameplate * RoofedFactor(trader.parent);
                    continue;
                }
                if (trader.parent.GetComp<CompPowerPlantWind>() != null)
                {
                    windNow += Mathf.Max(trader.PowerOutput, 0f);
                    windMax += nameplate;
                    continue;
                }

                if (trader.Props.PowerConsumption < 0f)
                {
                    otherProd += nameplate;
                }
                else
                {
                    // Hazard suppressors overwrite PowerOutput to active draw while
                    // their event is on; forecasts must use that, not idle nameplate.
                    nameplateDraw += ConsumerForecastDraw(trader.parent, nameplate);
                }
            }
        }

        internal static float ConsumerForecastDraw(ThingWithComps parent, float nameplate)
        {
            if (parent?.AllComps == null)
            {
                return nameplate;
            }
            for (int i = 0; i < parent.AllComps.Count; i++)
            {
                if (parent.AllComps[i] is IHazardPowerRamp ramp)
                {
                    return Mathf.Max(nameplate, ramp.ForecastDrawWatts);
                }
            }
            return nameplate;
        }

        private static float RoofedFactor(Thing thing)
        {
            int cells = 0;
            int roofed = 0;
            foreach (IntVec3 cell in thing.OccupiedRect())
            {
                cells++;
                if (thing.Map.roofGrid.Roofed(cell))
                {
                    roofed++;
                }
            }
            return cells <= 0 ? 1f : (float)(cells - roofed) / cells;
        }

        internal static float WeatherSkyMul(Map map)
        {
            float celestial = GenCelestial.CurCelestialSunGlow(map);
            if (celestial >= 0.08f)
            {
                float sky = map.skyManager.CurSkyGlow;
                float mul = Mathf.Clamp(sky / celestial, 0.08f, 1.4f);
                map.GetComponent<MapComponent_Stormproof>()?.RememberDaySkyMul(
                    map.weatherManager.curWeather, mul);
                return mul;
            }

            MapComponent_Stormproof comp = map.GetComponent<MapComponent_Stormproof>();
            float cached = comp != null
                ? comp.DaySkyMulFor(map.weatherManager.curWeather)
                : -1f;
            if (cached > 0f)
            {
                return cached;
            }
            return SkyMulFromWeather(map.weatherManager.curWeather);
        }

        internal static float SkyMulFromWeather(WeatherDef def)
        {
            if (def == null)
            {
                return 1f;
            }
            if (CompWeatherForecaster.BringsLightning(def))
            {
                return 0.35f;
            }
            string name = def.defName ?? "";
            if (name.IndexOf("Rain", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Fog", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0.55f;
            }
            if (name.IndexOf("Snow", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Blizzard", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0.50f;
            }
            if (name.IndexOf("Overcast", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0.70f;
            }
            return 1f;
        }

        private static float ProjectedSky(Map map, int ticksFromNow, bool weatherHolds, float weatherMul)
        {
            float celestial = GenCelestial.CelestialSunGlow(map, Find.TickManager.TicksAbs + ticksFromNow);
            float mul = weatherHolds ? weatherMul : AfterWeatherSkyMul;
            return Mathf.Clamp01(celestial * mul);
        }
    }
}
