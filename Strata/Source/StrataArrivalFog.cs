using Verse;

namespace Strata
{
    // AZR-235 — fog after deferred rock spawn, then open the arrival chamber.
    internal static class StrataArrivalFog
    {
        public static void ApplyAfterRockFill(Map map)
        {
            if (map == null || map.fogGrid == null)
            {
                return;
            }
            map.fogGrid.Refog(CellRect.WholeMap(map));
            FloodArrival(map);
        }

        public static void FloodArrival(Map map)
        {
            if (map == null)
            {
                return;
            }
            IntVec3 root = MapGenerator.PlayerStartSpot.IsValid
                ? MapGenerator.PlayerStartSpot
                : map.Center;
            FloodFillerFog.FloodUnfog(root, map);
        }
    }
}
