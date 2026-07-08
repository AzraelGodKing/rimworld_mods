using RimWorld;
using Verse;

namespace Strata
{
    // The top half of a stairwell pair. Vanilla MapPortal handles pocket map
    // generation (via def.portal), the enter job, and the view-level gizmo.
    public class Building_StairsDown : MapPortal
    {
        public override bool AutoDraftOnEnter => false;

        public override string EnterString => "Go downstairs";

        public override string EnteringString => "going downstairs";

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (PocketMapExists && PocketMap.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return "Someone is still on the level below.";
            }
            return base.DeconstructibleBy(faction);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map level = PocketMapExists ? PocketMap : null;
            base.Destroy(mode);
            // Collapse an empty level with the stairs; a level with pawns on it
            // stays alive so they can still climb out via the stairwell below.
            if (level != null && Find.Maps.Contains(level) && !level.mapPawns.AnyPawnBlockingMapRemoval)
            {
                PocketMapUtility.DestroyPocketMap(level);
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string state = PocketMapExists ? "Level below: excavated" : "Level below: not yet opened";
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }
    }
}
