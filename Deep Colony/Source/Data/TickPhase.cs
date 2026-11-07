#nullable enable
using Verse;

namespace DeepColony
{
    // One TicksGame clock, staggered so the hourly sweeps
    // do not all land on the same tick (offsets in TickPhaseOffsets).
    internal static class TickPhase
    {
        internal static bool Due(int offset)
        {
            return (Find.TickManager.TicksGame + offset) % TickPhaseOffsets.Interval == 0;
        }
    }
}
