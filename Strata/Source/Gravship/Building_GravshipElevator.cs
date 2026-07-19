using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Odyssey-only powered elevator shaft under the gravship. Same travel/pack
    // rules as gravship stairs; descending still needs a live power grid.
    public class Building_GravshipElevatorDown : Building_ElevatorDown, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship => Spawned && StrataGravshipUtility.CellOnGravship(Map, Position);

        public override string EnterString => "Take elevator below decks";

        public override string EnteringString => "taking the elevator below decks";

        protected override Map GeneratePocketMapInt()
        {
            Map existing = StrataGravshipUtility.ExistingGravshipLevelBelow(Map, this);
            if (existing != null)
            {
                IntVec3 landing = FindLandingCell(existing);
                if (landing.IsValid)
                {
                    StrataPortalUtility.SpawnLanding(def.portal.exitDef, landing, existing);
                    Messages.Message("Strata_GravshipConnectedBelow".Translate(), this, MessageTypeDefOf.PositiveEvent);
                    return existing;
                }
            }
            if (!BypassFirstLevelResearch && !LevelExcavationUtility.CanOpenNewLevelBelow(Map, out string reason, this))
            {
                Messages.Message(reason, this, MessageTypeDefOf.RejectInput, historical: false);
                return null;
            }
            return PocketMapUtility.GeneratePocketMap(
                new IntVec3(Map.Size.x, 1, Map.Size.z),
                def.portal.pocketMapGenerator, null, Map);
        }

        protected override string LevelInspectState()
        {
            string state = base.LevelInspectState();
            state += IsOnGravship
                ? "\nGravship elevator: linked floors will travel with the ship"
                : "\nGravship elevator: not on substructure (build on the ship)";
            return state;
        }
    }

    public class Building_GravshipElevatorUp : Building_ElevatorUp, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship =>
            entrance is IStrataGravshipPortal gp && gp.IsOnGravship;

        public override string EnterString => "Take elevator up to the ship";

        public override string EnteringString => "taking the elevator up to the ship";

        public override Map GetOtherMap()
        {
            if (StrataGravshipPortalTravel.EntranceOnCurrentStack(entrance, Map))
            {
                return base.GetOtherMap();
            }

            return StrataGravshipPortalTravel.ResolveGravshipHostForLanding(Map)
                ?? base.GetOtherMap();
        }

        public override IntVec3 GetDestinationLocation()
        {
            if (StrataGravshipPortalTravel.EntranceOnCurrentStack(entrance, Map))
            {
                return base.GetDestinationLocation();
            }

            Map host = StrataGravshipPortalTravel.ResolveGravshipHostForLanding(Map);
            return StrataGravshipPortalTravel.ResolveExitCell(host, entrance);
        }
    }

    public class Building_GravshipElevatorBuildUp : Building_ElevatorBuildUp, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship => Spawned && StrataGravshipUtility.CellOnGravship(Map, Position);

        public override string EnterString => "Take elevator to upper decks";

        public override string EnteringString => "taking the elevator to upper decks";

        protected override Map GeneratePocketMapInt()
        {
            Map existing = StrataGravshipUtility.ExistingGravshipLevelAbove(Map, this);
            if (existing != null)
            {
                IntVec3 landing = FindLandingCell(existing);
                if (landing.IsValid)
                {
                    // Exact ship footprint — no colony-style plaza beyond the hull.
                    StrataPortalUtility.SpawnLanding(def.portal.exitDef, landing, existing);
                    Messages.Message("Strata_GravshipConnectedAbove".Translate(), this, MessageTypeDefOf.PositiveEvent);
                    return existing;
                }
            }
            if (!LevelBuildUpUtility.CanOpenNewLevelAbove(Map, out string reason, this))
            {
                Messages.Message(reason, this, MessageTypeDefOf.RejectInput, historical: false);
                return null;
            }
            return PocketMapUtility.GeneratePocketMap(
                new IntVec3(Map.Size.x, 1, Map.Size.z),
                def.portal.pocketMapGenerator, null, Map);
        }

        protected override string LevelInspectState()
        {
            string state = base.LevelInspectState();
            state += IsOnGravship
                ? "\nGravship elevator: linked floors will travel with the ship"
                : "\nGravship elevator: not on substructure (build on the ship)";
            state += "\nUpper deck grows with gravship substructure on this map";
            return state;
        }

        protected override IEnumerable<Gizmo> ExtraGizmos()
        {
            yield break;
        }
    }

    public class Building_GravshipElevatorBuildUpLanding : Building_ElevatorBuildUpLanding, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship =>
            entrance is IStrataGravshipPortal gp && gp.IsOnGravship;

        public override string EnterString => "Take elevator down to the ship";

        public override string EnteringString => "taking the elevator down to the ship";

        public override Map GetOtherMap()
        {
            if (StrataGravshipPortalTravel.EntranceOnCurrentStack(entrance, Map))
            {
                return base.GetOtherMap();
            }

            return StrataGravshipPortalTravel.ResolveGravshipHostForLanding(Map)
                ?? base.GetOtherMap();
        }

        public override IntVec3 GetDestinationLocation()
        {
            if (StrataGravshipPortalTravel.EntranceOnCurrentStack(entrance, Map))
            {
                return base.GetDestinationLocation();
            }

            Map host = StrataGravshipPortalTravel.ResolveGravshipHostForLanding(Map);
            return StrataGravshipPortalTravel.ResolveExitCell(host, entrance);
        }
    }
}
