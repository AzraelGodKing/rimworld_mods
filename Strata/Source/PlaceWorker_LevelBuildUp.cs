using RimWorld;
using Verse;

namespace Strata
{
    public class PlaceWorker_LevelBuildUp : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            if (!LevelBuildUpUtility.CanOpenNewLevelAbove(map, out string reason, thingToIgnore))
            {
                return reason;
            }
            return true;
        }
    }
}
