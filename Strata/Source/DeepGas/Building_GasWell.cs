using Verse;

namespace Strata
{
    public class Building_GasWell : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Thing vent = Map?.thingGrid.ThingAt(Position, ThingDef.Named("Strata_GasVent"));
            if (vent != null && vent != this)
            {
                vent.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
