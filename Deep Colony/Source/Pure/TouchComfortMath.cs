#nullable enable
namespace DeepColony
{
    public enum TouchComfortTier : byte
    {
        Distant = 0,
        Familiar = 1,
        AtEase = 2,
        FineWithTouch = 3,
        Intimate = 4
    }

    /// <summary>
    /// Touch-comfort maths with no game types, so logic tests can run it.
    /// <c>TouchAverseUtility</c> passes in the live settings.
    /// </summary>
    internal static class TouchComfortMath
    {
        internal const float ComfortMax = 1f;
        internal const float OpinionGainFloor = 0f;

        internal static float ClampThreshold(float threshold)
        {
            if (threshold < 0.20f) return 0.20f;
            if (threshold > 0.90f) return 0.90f;
            return threshold;
        }

        internal static float TierMin(TouchComfortTier tier, float romanceThreshold)
        {
            float t = ClampThreshold(romanceThreshold);
            switch (tier)
            {
                case TouchComfortTier.Distant: return 0f;
                case TouchComfortTier.Familiar: return t * 0.38f;
                case TouchComfortTier.AtEase: return t * 0.69f;
                case TouchComfortTier.FineWithTouch: return t;
                default: return t + (1f - t) * 0.57f;
            }
        }

        internal static TouchComfortTier TierOf(float comfort, float romanceThreshold)
        {
            if (comfort >= TierMin(TouchComfortTier.Intimate, romanceThreshold)) return TouchComfortTier.Intimate;
            if (comfort >= TierMin(TouchComfortTier.FineWithTouch, romanceThreshold)) return TouchComfortTier.FineWithTouch;
            if (comfort >= TierMin(TouchComfortTier.AtEase, romanceThreshold)) return TouchComfortTier.AtEase;
            if (comfort >= TierMin(TouchComfortTier.Familiar, romanceThreshold)) return TouchComfortTier.Familiar;
            return TouchComfortTier.Distant;
        }

        /// <summary>Comfort gained per check while sharing a room, so the romance
        /// threshold is reached in about <paramref name="days"/> days.</summary>
        internal static float GainPerInterval(float days, float romanceThreshold, int checkIntervalTicks)
        {
            if (days < 0.5f) days = 0.5f;
            float intervals = days * (60000f / checkIntervalTicks);
            float threshold = romanceThreshold;
            if (threshold < 0.05f) threshold = 0.05f;
            return threshold / intervals;
        }

        internal static float OpinionMultiplier(float opinion)
        {
            float m = 0.7f + opinion * 0.009f;
            if (m < 0.4f) return 0.4f;
            if (m > 1.6f) return 1.6f;
            return m;
        }

        internal static float NextComfort(
            float current,
            bool adjacent,
            bool sameRoom,
            float opinion,
            float speedMul,
            float gainPerInterval)
        {
            if (speedMul < 0.1f) speedMul = 0.1f;
            float next = current;
            if (adjacent || sameRoom)
            {
                if (opinion >= OpinionGainFloor)
                {
                    float baseGain = gainPerInterval;
                    if (adjacent) baseGain *= 2.5f;
                    next += baseGain * OpinionMultiplier(opinion) * speedMul;
                }
                else if (adjacent)
                {
                    next -= gainPerInterval * 0.35f * speedMul;
                }
            }
            else
            {
                next -= gainPerInterval * 0.05f;
            }
            if (next < 0f) return 0f;
            if (next > ComfortMax) return ComfortMax;
            return next;
        }
    }
}
