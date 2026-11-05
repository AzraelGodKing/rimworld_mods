namespace Strata
{
    // Depth-scarce oxygen for underground levels, on plain values so
    // Tests/Strata.Tests can check it without the game.
    internal static class AtmosphereDepth
    {
        public const float OxygenFraction = 0.2095f;
        public const float MinDeepOxygenFraction = 0.07f;
        public const float DepthOxygenFalloff = 0.11f;

        // B1 breathes surface air; each level below loses 11% of it, down to 7% O2.
        public static float OxygenForDepth(int depth)
        {
            if (depth <= 1)
            {
                return OxygenFraction;
            }
            float scarcity = (depth - 1) * DepthOxygenFalloff;
            if (scarcity > 1f)
            {
                scarcity = 1f;
            }
            float o2 = OxygenFraction * (1f - scarcity);
            return o2 < MinDeepOxygenFraction ? MinDeepOxygenFraction : o2;
        }
    }
}
