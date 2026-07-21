using Verse;

namespace Strata
{
    /// <summary>
    /// V3 hard cap: ±2 from the stack root (colony surface or gravship host) unless Unlimited is on.
    /// Extra shafts that join an already-open floor skip this (handled by callers before calling).
    /// </summary>
    public static class StrataLevelCap
    {
        public const int HardMaxOffset = 2;

        public static bool Unlimited =>
            StrataMod.Settings != null && StrataMod.Settings.unlimitedLevelsEnabled;

        public static int MaxOffset => Unlimited ? int.MaxValue : HardMaxOffset;

        /// <summary>True if opening a brand-new level below <paramref name="map"/> is within the cap.</summary>
        public static bool AllowsNewLevelBelow(Map map, out string reason)
        {
            reason = null;
            if (Unlimited || map == null)
            {
                return true;
            }
            // Surface/host = 0; B3 = 3 → next would be 4 (ok); B4 = 4 → next 5 (blocked).
            if (StrataDepth.Of(map) >= HardMaxOffset)
            {
                reason = "Strata_LevelCap_MaxBelow".Translate(HardMaxOffset);
                return false;
            }
            return true;
        }

        /// <summary>True if opening a brand-new level above <paramref name="map"/> is within the cap.</summary>
        public static bool AllowsNewLevelAbove(Map map, out string reason)
        {
            reason = null;
            if (Unlimited || map == null)
            {
                return true;
            }
            int height = StrataMapUtility.IsUpperLevel(map)
                ? StrataDepth.CountLevelsAboveSurface(map)
                : 0;
            if (height >= HardMaxOffset)
            {
                reason = "Strata_LevelCap_MaxAbove".Translate(HardMaxOffset);
                return false;
            }
            return true;
        }
    }
}
