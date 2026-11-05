namespace Strata
{
    // The decision behind leaving local work for work on another floor, on
    // plain values so Tests/Strata.Tests can check it without the game.
    internal static class RelayChoice
    {
        // Lower priority numbers are more important (1 beats 4).
        public static bool Qualifies(
            int localPriority,
            int remotePriority,
            int depth,
            int maxDepth,
            bool emergencyTrip)
        {
            if (emergencyTrip)
            {
                return true;
            }
            // Strictly better only, so two floors with equal work never ping-pong.
            return maxDepth > 0 && depth <= maxDepth && remotePriority < localPriority;
        }

        // Whether a candidate floor beats the best found so far: emergencies
        // first, then the better priority, then the nearer floor.
        public static bool Wins(
            bool haveBest,
            bool emergencyTrip,
            int remotePriority,
            int depth,
            bool bestEmergency,
            int bestPriority,
            int bestDepth)
        {
            if (!haveBest)
            {
                return true;
            }
            if (emergencyTrip != bestEmergency)
            {
                return emergencyTrip;
            }
            if (remotePriority != bestPriority)
            {
                return remotePriority < bestPriority;
            }
            return depth < bestDepth;
        }
    }
}
