using UnityEngine;
using Verse;
using RimWorld;

namespace Stormproof
{
    // Extreme heat event (+28°C). Climate stabilizer zeroes the offset.
    public class GameCondition_HeatDome : GameCondition
    {
        private const float TempOffsetCelsius = 28f;

        private static readonly SkyColorSet HeatDomeSkyColors = new SkyColorSet(
            new Color(1.00f, 0.70f, 0.42f),
            new Color(1.00f, 0.88f, 0.68f),
            new Color(0.92f, 0.55f, 0.32f),
            1.0f);

        public override int TransitionTicks => 120;

        public override float TemperatureOffset() => TempOffsetCelsius;

        public override float SkyTargetLerpFactor(Map map) =>
            GameConditionUtility.LerpInOutValue(this, TransitionTicks);

        public override SkyTarget? SkyTarget(Map map) =>
            new SkyTarget(0.95f, HeatDomeSkyColors, 1f, 1f);
    }

    // Extreme cold event (-28°C). Climate stabilizer zeroes the offset.
    public class GameCondition_PolarFront : GameCondition
    {
        private const float TempOffsetCelsius = -28f;

        private static readonly SkyColorSet PolarSkyColors = new SkyColorSet(
            new Color(0.62f, 0.72f, 0.92f),
            new Color(0.82f, 0.88f, 0.98f),
            new Color(0.50f, 0.58f, 0.78f),
            0.9f);

        public override int TransitionTicks => 120;

        public override float TemperatureOffset() => TempOffsetCelsius;

        public override float SkyTargetLerpFactor(Map map) =>
            GameConditionUtility.LerpInOutValue(this, TransitionTicks);

        public override SkyTarget? SkyTarget(Map map) =>
            new SkyTarget(0.75f, PolarSkyColors, 1f, 1f);
    }

    // Dry thunderstorm forced over the map - lightning without rain.
    // Storm spires harvest; fire suppressors and storm callers counter the fires.
    public class GameCondition_DryLightning : GameCondition
    {
        public override WeatherDef ForcedWeather() => StormproofDefOf.DryThunderstorm;

        public override float WeatherCommonalityFactor(WeatherDef weather, Map map)
        {
            if (weather == StormproofDefOf.DryThunderstorm)
            {
                return 1000f;
            }
            return weather.rainRate > 0.01f ? 0f : 1f;
        }
    }
}
