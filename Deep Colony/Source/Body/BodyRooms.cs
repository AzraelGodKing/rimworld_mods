using RimWorld;
using Verse;

namespace DeepColony
{
    internal static class BodyRooms
    {
        // Medical beds make an infirmary. Vanilla hospital counts — do not
        // make the player rebuild a ward they already have.
        public static bool IsInfirmary(Room room)
        {
            if (room?.Role == null)
            {
                return false;
            }
            string name = room.Role.defName;
            return name == "DC_Infirmary" || name == "Hospital";
        }
    }
}
