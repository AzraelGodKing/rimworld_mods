using RimWorld;
using Verse;

namespace Strata
{
    // Updraft filters must sit in the same room as a stairwell up or elevator landing.
    public class PlaceWorker_UpdraftFilter : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            Room room = center.GetRoom(map);
            if (room == null || room.PsychologicallyOutdoors)
            {
                return "Must be built indoors.";
            }
            if (!SmokeRiseUtility.RoomContainsLevelExit(room, map))
            {
                return "Must be built in a stairwell or elevator room.";
            }
            return true;
        }
    }
}
