using Verse;

namespace Strata
{
    // Shared rock-chamber carving for hidden gensteps and cave-breakthrough events.
    public static class ChamberCarveUtility
    {
        public static void CarveCircle(Map map, IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (cell.InBounds(map))
                {
                    cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
