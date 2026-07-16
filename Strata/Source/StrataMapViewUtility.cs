using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // True when the player is looking at a colony map, not the world tab.
    public static class StrataMapViewUtility
    {
        public static bool IsColonyMapView()
        {
            if (Find.CurrentMap == null)
            {
                return false;
            }
            if (Find.World?.renderer != null && Find.World.renderer.wantedMode != WorldRenderMode.None)
            {
                return false;
            }
            return true;
        }
    }
}
