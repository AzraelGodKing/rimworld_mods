using Verse;

namespace Strata
{
    /// <summary>Life-support gear: ship substructure or already-linked Strata gravship decks.</summary>
    public class PlaceWorker_GravshipLifeSupport : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            if (!StrataGravshipUtility.OdysseyActive)
            {
                return "Strata_Place_RequiresOdyssey".Translate();
            }
            if (StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return true;
            }
            if (StrataGravshipUtility.FindGravEngine(map) == null)
            {
                return "Strata_Place_NeedsGravEngine".Translate();
            }
            ThingDef thingDef = checkingDef as ThingDef;
            IntVec2 size = thingDef?.size ?? IntVec2.One;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, size))
            {
                if (!StrataGravshipUtility.CellOnGravship(map, cell))
                {
                    return "Strata_Place_MustBeSubstructure".Translate();
                }
            }
            return true;
        }
    }
}
