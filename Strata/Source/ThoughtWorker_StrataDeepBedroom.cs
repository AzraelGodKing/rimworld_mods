using RimWorld;
using Verse;

namespace Strata
{
    // Small mood bonus for colonists standing on an exposed roof deck (A+ upper
    // level terrain), awake and outdoors — suggests appreciating the view.
    // Does not require depthDimEnabled; the mood is independent of the visual cue.
    public class ThoughtWorker_StrataUpperDeckView : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Awake() || !StrataMapUtility.IsUpperLevel(pawn.Map))
            {
                return ThoughtState.Inactive;
            }
            // Triggers while outdoors on the upper level — not inside an enclosed
            // room built on top (walled workshop, bedroom, etc.). Room null or
            // UsesOutdoorTemperature means the pawn has open sky above them.
            Room room = pawn.GetRoom();
            return (room == null || room.UsesOutdoorTemperature)
                ? ThoughtState.ActiveAtStage(0)
                : ThoughtState.Inactive;
        }
    }


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
