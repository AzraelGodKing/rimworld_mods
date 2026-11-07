#nullable enable
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Public API for sister mods (Deep Colony) and anyone else (AZR-574).
    /// Call through reflection or a soft reference; every method is safe to call
    /// with null or dead pawns and outside a game.
    /// </summary>
    public static class DateNightApi
    {
        /// <summary>Bumped when a signature here changes.</summary>
        public const int ApiVersion = 1;

        /// <summary>The pawn is on a date job right now.</summary>
        public static bool IsOnDate(Pawn? pawn)
        {
            return pawn != null && pawn.CurJobDef != null && pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate;
        }

        /// <summary>
        /// The pawn's last finished date was within <paramref name="withinTicks"/>
        /// (default one day) and was not awkward or ruined.
        /// </summary>
        public static bool HadRecentGoodDate(Pawn? pawn, int withinTicks = GenDate.TicksPerDay)
        {
            if (pawn == null || Current.Game == null)
            {
                return false;
            }
            CoupleHistory? h = DateNightHistory.LatestFor(pawn);
            if (h == null || h.lastDateTick < 0
                || Find.TickManager.TicksGame - h.lastDateTick > withinTicks)
            {
                return false;
            }
            return h.lastQuality == null
                || (h.lastQuality != DateNightDefOf.DateNight_DateAwkward
                    && h.lastQuality != DateNightDefOf.DateNight_DateRuined);
        }

        /// <summary>Years since the couple's lover, fiancé or spouse relation began, or 0.</summary>
        public static float YearsTogether(Pawn? a, Pawn? b)
        {
            if (a == null || b == null || Current.Game == null)
            {
                return 0f;
            }
            DirectPawnRelation? rel = DateNightAnniversaries.GetLoveRelation(a, b);
            if (rel == null)
            {
                return 0f;
            }
            return (Find.TickManager.TicksGame - rel.startTicks) / (float)GenDate.TicksPerYear;
        }

        /// <summary>
        /// Tell Date Night a couple just broke up, so it reacts now instead of at
        /// its next 2500-tick check. Safe to call twice or for non-couples.
        /// </summary>
        public static void NotifyBreakup(Pawn? a, Pawn? b)
        {
            if (a == null || b == null || Current.Game == null)
            {
                return;
            }
            DateNightVenues.NotifyBreakup(a, b);
        }
    }
}
