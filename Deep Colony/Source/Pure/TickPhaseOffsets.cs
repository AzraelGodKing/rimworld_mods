#nullable enable
namespace DeepColony
{
    /// <summary>
    /// Phase offsets for the hourly sweeps that share one 2500-tick interval.
    /// Each sweep has its own offset so no two land on the same tick. Logic tests check
    /// that every value in <see cref="All"/> is distinct and inside the interval.
    /// </summary>
    internal static class TickPhaseOffsets
    {
        internal const int Interval = 2500;

        internal const int Elders = 83;
        internal const int FamilyLife = 167;
        internal const int PruneIds = 189;
        internal const int Heirlooms = 250;
        internal const int FamilyLetters = 334;
        internal const int FamilyJoin = 417;
        internal const int ExLoverReconcile = 501;
        internal const int TouchComfort = 668;
        internal const int FamilyEcho = 835;
        internal const int LaborWedge = 947;
        internal const int AnomalyOdyssey = 1002;
        internal const int Remembrance = 1169;
        internal const int Flashback = 1336;
        internal const int FactionEnvoy = 1503;
        internal const int EnvoyVisit = 1670;
        internal const int TraumaSystems = 1837;
        internal const int ToxicBuildup = 2004;
        internal const int DeathWindow = 2171;
        internal const int FactionDrift = 2338;

        internal static readonly int[] All =
        {
            Elders, FamilyLife, PruneIds, Heirlooms, FamilyLetters, FamilyJoin, ExLoverReconcile,
            TouchComfort, FamilyEcho, LaborWedge, AnomalyOdyssey, Remembrance, Flashback,
            FactionEnvoy, EnvoyVisit, TraumaSystems, ToxicBuildup, DeathWindow, FactionDrift
        };
    }
}
