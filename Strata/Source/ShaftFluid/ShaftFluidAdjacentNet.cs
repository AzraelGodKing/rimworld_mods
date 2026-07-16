using System.Collections.Generic;
using Verse;

namespace Strata
{
    // Shaft-side pipe tap — like CompPowerShaft on shaft power conduits, the junction
    // joins the local net through its own pipe comp; adjacent segments are a fallback.
    internal static class ShaftFluidAdjacentNet
    {
        internal static object FirstNet(Thing junction, System.Func<ThingWithComps, object> tryGetNet)
        {
            if (junction?.Map == null || tryGetNet == null)
            {
                return null;
            }

            Map map = junction.Map;
            if (junction is ThingWithComps self)
            {
                object own = tryGetNet(self);
                if (own != null)
                {
                    return own;
                }
            }

            foreach (IntVec3 cell in TouchingCells(junction))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is ThingWithComps twc && twc != junction)
                    {
                        object net = tryGetNet(twc);
                        if (net != null)
                        {
                            return net;
                        }
                    }
                }
            }

            return null;
        }

        private static IEnumerable<IntVec3> TouchingCells(Thing thing)
        {
            var cells = new HashSet<IntVec3>();
            CellRect rect = thing.OccupiedRect();
            foreach (IntVec3 cell in rect)
            {
                cells.Add(cell);
            }

            foreach (IntVec3 cell in rect.ExpandedBy(1))
            {
                cells.Add(cell);
            }

            return cells;
        }
    }
}
