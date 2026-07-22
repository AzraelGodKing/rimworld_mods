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

        // Gravship stacks are hard-capped at ±1 (one underdeck, one tower deck):
        // deeper stacks multiply takeoff packing / landing rebuild work and were
        // the fragile path in travel bugs. Not affected by the Unlimited setting.
        public const int GravshipMaxOffset = 1;

        private static bool BlockedByGravshipCap(Map map, out string reason)
        {
            reason = null;
            if (!StrataGravshipUtility.OdysseyActive
                || !StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return false;
            }
            reason = "Strata_LevelCap_GravshipMax".Translate(GravshipMaxOffset);
            return true;
        }

        public static bool Unlimited =>
            StrataMod.Settings != null && StrataMod.Settings.unlimitedLevelsEnabled;

        public static int MaxOffset => Unlimited ? int.MaxValue : HardMaxOffset;

        /// <summary>True if opening a brand-new level below <paramref name="map"/> is within the cap.</summary>
        public static bool AllowsNewLevelBelow(Map map, out string reason)
        {
            reason = null;
            if (map == null)
            {
                return true;
            }
            if (BlockedByGravshipCap(map, out reason))
            {
                return false;
            }
            if (Unlimited)
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
            if (map == null)
            {
                return true;
            }
            if (BlockedByGravshipCap(map, out reason))
            {
                return false;
            }
            if (Unlimited)
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
