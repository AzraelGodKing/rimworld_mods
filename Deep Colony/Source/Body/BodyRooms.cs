using RimWorld;
using Verse;

namespace DeepColony
{
    internal static class BodyRooms
    {
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
