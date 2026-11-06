using System;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#else
using HarmonyLib;
#endif
using RimWorld;
using Verse;

namespace Stormproof
{
    // WeatherDecider.curWeatherDuration is private. If a RimWorld update renames
    // it, forecasts and the storm caller degrade instead of throwing.
    internal static class WeatherDeciderAccess
    {
#if NET8_0_OR_GREATER
        // .NET 10 (RimWorld 1.7): direct field access, no reflection. A missing
        // field throws MissingFieldException on first use; the probe turns that
        // into one warning and a quiet forecaster.
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "curWeatherDuration")]
        private static extern ref int DurationField(WeatherDecider decider);

        private static bool probed;
        private static bool available;

        private static bool Available(WeatherDecider decider)
        {
            if (probed)
            {
                return available;
            }
            probed = true;
            try
            {
                _ = DurationField(decider);
                available = true;
            }
            catch (Exception e) when (e is MissingFieldException || e is MissingMemberException)
            {
                Log.Warning("[Stormproof] WeatherDecider.curWeatherDuration not found; weather forecasts disabled. " + e.Message);
                available = false;
            }
            return available;
        }

        internal static bool TryGetDuration(WeatherDecider decider, out int duration)
        {
            duration = 0;
            if (decider == null || !Available(decider))
            {
                return false;
            }
            duration = DurationField(decider);
            return true;
        }

        internal static bool TrySetDuration(WeatherDecider decider, int duration)
        {
            if (decider == null || !Available(decider))
            {
                return false;
            }
            DurationField(decider) = duration;
            return true;
        }
#else
        private static readonly AccessTools.FieldRef<WeatherDecider, int> DurationRef = Resolve();

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
#endif
    }
}
