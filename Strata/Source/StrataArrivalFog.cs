using Verse;

namespace Strata
{
    // AZR-235 / AZR-349 — fog after deferred rock spawn, then open the arrival
    // chamber. Wall faces that border explored open stay visible; deep rock
    // stays fogged via StrataUndergroundFog.RefogDeepRock.
    internal static class StrataArrivalFog
    {
        public static void ApplyAfterRockFill(Map map)
        {
            StrataUndergroundFog.ApplyArrivalFog(map);
        }

        public static void FloodArrival(Map map)
        {
            if (map == null)
            {
                return;
            }
            IntVec3 root = StrataUndergroundFog.FindFogRoot(map);
            if (!root.IsValid || !root.InBounds(map))
            {
                root = map.Center;
            }
            FloodFillerFog.FloodUnfog(root, map);
            StrataUndergroundFog.RefogDeepRock(map);
        }
    }
}
