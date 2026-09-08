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
    }

    // Crosses the weather forecaster with live solar/wind so the monitor's
    // empty-in line is not "assume this sky forever". Cached; no per-tick work.
    internal static class GridForecastUtility
    {
        internal const int HorizonTicks = 2500 * 8;
        private const int StepTicks = 250;
        private const float AfterWeatherSkyMul = 0.9f;

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

        internal static GridForecast Project(Map map, PowerNet net, CompWeatherForecaster forecast,
            float stored, float capacity, float lowFraction, float criticalFraction)
        {
            var result = new GridForecast
            {
                HasForecaster = forecast != null,
                WeatherRemaining = forecast != null ? forecast.RemainingTicks() : 0,
                TicksToEmpty = -1,
                TicksToFull = -1,
                TicksToLow = -1,
                TicksToCritical = -1,
                NadirFraction = capacity <= 0f ? 0f : stored / capacity
            };

            if (map == null || net == null || capacity <= 0f)
            {
                return result;
            }

            SplitPlants(net, out float solarMax, out float windNow, out float windMax, out float constantWatts);
            float weatherMul = WeatherSkyMul(map);
            float windFrac = windMax > 0.01f ? Mathf.Clamp01(windNow / windMax) : 0f;
            float energy = stored;
            float nadir = stored;

            for (int elapsed = 0; elapsed < HorizonTicks; elapsed += StepTicks)
            {
                bool holds = forecast != null && elapsed < result.WeatherRemaining;
                float sky = ProjectedSky(map, elapsed, holds, weatherMul);
                float wind = holds ? windFrac : windFrac * 0.55f;
                float watts = solarMax * sky + windMax * wind + constantWatts;
                energy = Mathf.Clamp(energy + watts * CompPower.WattsToWattDaysPerTick * StepTicks, 0f, capacity);
                if (energy < nadir)
                {
                    nadir = energy;
                }

                float fraction = energy / capacity;
                int at = elapsed + StepTicks;
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
                    break;
                }
                if (result.TicksToFull < 0 && energy >= capacity - 0.05f && watts > 0f)
                {
                    result.TicksToFull = at;
                }
            }

            result.NadirFraction = nadir / capacity;
            return result;
        }

        private static void SplitPlants(PowerNet net,
            out float solarMax, out float windNow, out float windMax, out float constantWatts)
        {
            solarMax = 0f;
            windNow = 0f;
            windMax = 0f;
            float otherProd = 0f;
            float consumption = 0f;

            for (int i = 0; i < net.powerComps.Count; i++)
            {
                CompPowerTrader trader = net.powerComps[i];
                if (trader == null || !trader.PowerOn || trader.parent == null)
                {
                    continue;
                }

                float output = trader.PowerOutput;
                float nameplate = Mathf.Abs(trader.Props.PowerConsumption);
                if (trader.parent.GetComp<CompPowerPlantSolar>() != null)
                {
                    solarMax += nameplate * RoofedFactor(trader.parent);
                    continue;
                }
                if (trader.parent.GetComp<CompPowerPlantWind>() != null)
                {
                    windNow += Mathf.Max(output, 0f);
                    windMax += nameplate;
                    continue;
                }

                if (output > 0f)
                {
                    otherProd += output;
                }
                else
                {
                    consumption += -output;
                }
            }

            constantWatts = otherProd - consumption;
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

        private static float WeatherSkyMul(Map map)
        {
            float celestial = GenCelestial.CurCelestialSunGlow(map);
            float sky = map.skyManager.CurSkyGlow;
            if (celestial < 0.08f)
            {
                return 0.5f;
            }
            return Mathf.Clamp(sky / celestial, 0.08f, 1.4f);
        }

        private static float ProjectedSky(Map map, int ticksFromNow, bool weatherHolds, float weatherMul)
        {
            float celestial = GenCelestial.CelestialSunGlow(map, Find.TickManager.TicksAbs + ticksFromNow);
            float mul = weatherHolds ? weatherMul : AfterWeatherSkyMul;
            return Mathf.Clamp01(celestial * mul);
        }
    }
}
