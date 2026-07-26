using RimWorld;
using Verse;

namespace Strata
{
    // The ancient stairhead found on a sunken ruin site. Same vanilla MapPortal
    // machinery as the colony stairwell, but it leads down into a pre-carved,
    // pre-infested warren instead of a solid stratum to mine, and it carries
    // none of the colony extras (no power shaft, no seal, no sibling adoption).
    public class Building_RuinStairsDown : MapPortal
    {
        public override string EnterString => "Descend into the ruin";

        public override string EnteringString => "descending into the ruin";

        // The warren below is hostile ground: arrive weapons-out.
        public override bool AutoDraftOnEnter => true;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map level = PocketMapExists ? PocketMap : null;
            bool openedMove = false;
            if (!StrataPortalUtility.PortalMoveInProgress)
            {
                StrataPortalUtility.BeginPortalMove();
                openedMove = true;
            }
            bool prevAllow = Thing.allowDestroyNonDestroyable;
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                base.Destroy(mode);
            }
            finally
            {
                Thing.allowDestroyNonDestroyable = prevAllow;
                if (openedMove)
                {
                    StrataPortalUtility.EndPortalMove();
                }
            }
            // If the stairhead is wrecked, an empty warren collapses with it.
            // A warren with colony presence stays alive: the landing below
            // keeps working and pawns emerge where the stairhead stood.
            if (level != null && Find.Maps.Contains(level)
                && !StrataPortalUtility.LinkedLevelHasColonyPresence(level))
            {
                PocketMapUtility.DestroyPocketMap(level);
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string state = PocketMapExists
                ? "The warren below has been opened."
                : "Stale air drifts up from the darkness below.";
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }
    }
}
