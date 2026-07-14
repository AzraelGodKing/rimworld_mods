using RimWorld;
using Verse;

namespace Strata
{
    public class PlaceWorker_GasWell : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            ThingDef ventDef = DefDatabase<ThingDef>.GetNamedSilentFail("Strata_GasVent");
            if (ventDef == null || map.thingGrid.ThingAt(loc, ventDef) == null)
            {
                return "Must be placed on a natural gas vent.";
            }
            return true;
        }
    }
}
