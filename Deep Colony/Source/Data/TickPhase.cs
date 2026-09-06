using Verse;

namespace DeepColony
{
    // AZR-123 — one TicksGame clock, staggered so fifteen sweeps
    // do not all land on the same hour tick.
    internal static class TickPhase
    {
        internal const int Interval = 2500;

        internal static bool Due(int offset)
        {
            return (Find.TickManager.TicksGame + offset) % Interval == 0;
        }
    }
}
