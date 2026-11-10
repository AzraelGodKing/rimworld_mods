using Verse;

namespace Strata
{
    public class Building_GasPipe : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            GasPipeUtility.Notify_PipesChanged(map);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            GasPipeUtility.Notify_PipesChanged(map);
        }

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
