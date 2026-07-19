using RimWorld;
using Verse;

namespace Strata
{
    // Gravship stairs only place on gravship substructure (Odyssey).
    public class PlaceWorker_GravshipStairwell : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            if (!StrataGravshipUtility.OdysseyActive)
            {
                return "Strata_Place_RequiresOdyssey".Translate();
            }
            if (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return "Strata_Place_GravshipNotLinkedLevel".Translate();
            }
            if (StrataGravshipUtility.FindGravEngine(map) == null)
            {
                return "Strata_Place_NeedsGravEngine".Translate();
            }
            ThingDef thingDef = def as ThingDef;
            IntVec2 size = thingDef?.size ?? IntVec2.One;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, size))
            {
                if (!StrataGravshipUtility.CellOnGravship(map, cell))
                {
                    return "Strata_Place_MustBeSubstructure".Translate();
                }
            }

            // Direction-specific research / stack rules.
            if (thingDef?.defName == "Strata_GravshipStairsBuildUp"
                || thingDef?.defName == "Strata_GravshipElevatorBuildUp"
                || thingDef?.thingClass == typeof(Building_GravshipStairsBuildUp)
                || thingDef?.thingClass == typeof(Building_GravshipElevatorBuildUp))
            {
                if (!LevelBuildUpUtility.CanOpenNewLevelAbove(map, out string upReason, thingToIgnore))
                {
                    return upReason;
                }
            }
            else if (!LevelExcavationUtility.CanOpenNewLevelBelow(map, out string downReason, thingToIgnore))
            {
                return downReason;
            }
            return true;
        }
    }
}
