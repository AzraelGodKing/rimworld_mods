using RimWorld;
using Verse;

namespace DeepColony
{
    public class RoomRoleWorker_Infirmary : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors)
            {
                return 0f;
            }
            int medicalBeds = 0;
            foreach (Building_Bed bed in room.ContainedBeds)
            {
                if (bed != null && bed.Medical)
                {
                    medicalBeds++;
                }
            }
            if (medicalBeds <= 0)
            {
                return 0f;
            }
            float clean = room.GetStat(RoomStatDefOf.Cleanliness);
            return medicalBeds * 100000f + clean * 50f;
        }
    }
}
