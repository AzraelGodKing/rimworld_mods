using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Vanilla decides whether a temporary map (a site, an ambush) can be torn
    // down by asking only that map's own pawns. Pawns standing on a pocket
    // level UNDER the map are invisible to that check, so a sunken ruin's
    // surface could despawn - destroying the warren below - while colonists
    // are still down there. Count pawns on child pocket maps as blocking.
    // The check chains naturally through stacked levels because each pocket
    // map's own AnyPawnBlockingMapRemoval re-enters this patch.
    [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval), MethodType.Getter)]
    public static class Patch_PocketMapRemoval
    {
        public static void Postfix(Map ___map, ref bool __result)
        {
            if (__result || ___map == null || Find.Maps == null)
            {
                return;
            }
            foreach (Map map in Find.Maps)
            {
                if (map != ___map && map.Parent is PocketMapParent pocket
                    && pocket.sourceMap == ___map
                    && map.mapPawns.AnyPawnBlockingMapRemoval)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
