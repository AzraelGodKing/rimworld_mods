using RimWorld;
using Verse;

namespace Strata
{
    // Mood bonus for colonists sleeping in an impressive bedroom underground.
    public class ThoughtWorker_StrataDeepBedroom : ThoughtWorker
    {
        private const float MinImpressiveness = 50f;

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Awake() || !StrataMapUtility.IsUnderground(pawn.Map))
            {
                return ThoughtState.Inactive;
            }
            Building_Bed bed = pawn.CurrentBed();
            if (bed == null || !bed.def.building.bed_humanlike)
            {
                return ThoughtState.Inactive;
            }
            Room room = bed.GetRoom();
            if (room == null || room.IsDoorway)
            {
                return ThoughtState.Inactive;
            }
            float impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
            if (impressiveness < MinImpressiveness)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(impressiveness >= 120f ? 1 : 0);
        }
    }
}
