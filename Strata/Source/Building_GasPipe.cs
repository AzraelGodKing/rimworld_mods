using Verse;

namespace Strata
{
    public class Building_GasPipe : Building
    {
        public static Building_GasPipe At(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return null;
            }
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing is Building_GasPipe pipe)
                {
                    return pipe;
                }
            }
            return null;
        }
    }
}
