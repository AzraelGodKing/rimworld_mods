using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Odyssey-only stairwell that opens a Strata level under the gravship and
    // marks the stack as ship-linked for takeoff/landing follow.
    public class Building_GravshipStairsDown : Building_StairsDown, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship => Spawned && StrataGravshipUtility.CellOnGravship(Map, Position);

        public override string EnterString => "Go below decks";

        public override string EnteringString => "going below decks";

        // Never join a colony dig pocket — only other gravship shafts.
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
                ? "\nGravship shaft: linked floors will travel with the ship"
                : "\nGravship shaft: not on substructure (build on the ship)";
            return state;
        }
    }

    public class Building_GravshipStairsUp : Building_StairsUp, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship =>
            entrance is IStrataGravshipPortal gp && gp.IsOnGravship;

        public override string EnterString => "Go up to the ship";

        public override string EnteringString => "returning to the ship";

        // Prefer the landed ship host when entrance still points at a left-behind
        // shaft (GravAnchor) or a discarded departure map cell.
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

    // Opens an outdoor roof deck above the gravship (A+), ship-linked.
    public class Building_GravshipStairsBuildUp : Building_StairsBuildUp, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship => Spawned && StrataGravshipUtility.CellOnGravship(Map, Position);

        public override string EnterString => "Go to upper decks";

        public override string EnteringString => "going to upper decks";

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
                ? "\nGravship shaft: linked floors will travel with the ship"
                : "\nGravship shaft: not on substructure (build on the ship)";
            state += "\nUpper deck grows with gravship substructure on this map";
            return state;
        }

        protected override IEnumerable<Gizmo> ExtraGizmos()
        {
            yield break;
        }
    }

    public class Building_GravshipBuildUpLanding : Building_BuildUpLanding, IStrataGravshipPortal
    {
        public bool IsGravshipPortal => true;

        public bool IsOnGravship =>
            entrance is IStrataGravshipPortal gp && gp.IsOnGravship;

        public override string EnterString => "Go down to the ship";

        public override string EnteringString => "returning to the ship";

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
