using RimWorld;
using Verse;

namespace Strata
{
    public class PlaceWorker_LevelExcavation : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            if (!LevelExcavationUtility.CanOpenNewLevelBelow(map, out string reason, thingToIgnore))
            {
                return reason;
            }
            return true;
        }
    }
}
