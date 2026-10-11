using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // WeatherDecider.curWeatherDuration is private. If a RimWorld update renames
    // it, forecasts and the storm caller degrade instead of throwing at type init.
    internal static class WeatherDeciderAccess
    {
        private static readonly AccessTools.FieldRef<WeatherDecider, int> DurationRef = Resolve();

        internal static bool Available => DurationRef != null;

        private static AccessTools.FieldRef<WeatherDecider, int> Resolve()
        {
            try
            {
                if (AccessTools.Field(typeof(WeatherDecider), "curWeatherDuration") == null)
                {
                    Log.Warning("[Stormproof] WeatherDecider.curWeatherDuration not found; weather forecasts disabled.");
                    return null;
                }
                return AccessTools.FieldRefAccess<WeatherDecider, int>("curWeatherDuration");
            }
            catch (Exception e)
            {
                Log.Warning("[Stormproof] WeatherDecider.curWeatherDuration unavailable; weather forecasts disabled. " + e.Message);
                return null;
            }
        }

        internal static bool TryGetDuration(WeatherDecider decider, out int duration)
        {
            duration = 0;
            if (DurationRef == null || decider == null)
            {
                return false;
            }
            duration = DurationRef(decider);
            return true;
        }

        internal static bool TrySetDuration(WeatherDecider decider, int duration)
        {
            if (DurationRef == null || decider == null)
            {
                return false;
            }
            DurationRef(decider) = duration;
            return true;
        }
    }
}
