using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // The bottom landing of a stairwell pair, spawned by map generation.
    // Hardened against a destroyed entrance so pawns can never be trapped:
    // if the stairs above are gone, climbing up still works and drops the
    // pawn at the old stairhead.
    public class Building_StairsUp : PocketMapExit
    {
        private IntVec3 lastKnownEntranceCell = IntVec3.Invalid;

        private Map SourceMap => (Map?.Parent as PocketMapParent)?.sourceMap;

        private bool EntranceValid => entrance != null && !entrance.Destroyed && entrance.Spawned;

        public override string EnterString => "Go upstairs";

        public override string EnteringString => "going upstairs";

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (Faction != Faction.OfPlayer)
            {
                SetFaction(Faction.OfPlayer);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                CacheEntranceCell();
            }
            Scribe_Values.Look(ref lastKnownEntranceCell, "strataLastKnownEntranceCell", IntVec3.Invalid);
        }

        private void CacheEntranceCell()
        {
            if (EntranceValid)
            {
                lastKnownEntranceCell = entrance.Position;
            }
        }

        public override Map GetOtherMap()
        {
            if (EntranceValid)
            {
                CacheEntranceCell();
                return entrance.Map;
            }
            return SourceMap;
        }

        public override IntVec3 GetDestinationLocation()
        {
            if (EntranceValid)
            {
                CacheEntranceCell();
                return base.GetDestinationLocation();
            }
            Map above = SourceMap;
            if (above == null)
            {
                return IntVec3.Invalid;
            }
            IntVec3 near = lastKnownEntranceCell.IsValid ? lastKnownEntranceCell : above.Center;
            if (CellFinder.TryFindRandomCellNear(near, above, 5, c => c.Standable(above) && !c.Fogged(above), out IntVec3 cell))
            {
                return cell;
            }
            return CellFinder.RandomCell(above);
        }

        public override bool IsEnterable(out string reason)
        {
            if ((entrance as Building_StairsDown)?.Sealed == true)
            {
                reason = "The stairwell is sealed.";
                return false;
            }
            if (EntranceValid)
            {
                return base.IsEnterable(out reason);
            }
            if (SourceMap == null)
            {
                reason = "The way up has collapsed.";
                return false;
            }
            reason = null;
            return true;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return "It's the only way up.";
        }
    }
}
