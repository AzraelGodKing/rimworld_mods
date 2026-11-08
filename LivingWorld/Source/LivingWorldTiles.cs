using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Saved tiles are an int id plus an int layer id. Older saves have no layer
    /// (-1) and load on the surface. Distances are only measured on one layer.
    /// </summary>
    internal static class LivingWorldTiles
    {
        public static PlanetTile Make(int tileId, int layerId)
        {
            if (tileId < 0)
            {
                return PlanetTile.Invalid;
            }
            return layerId < 0 ? new PlanetTile(tileId) : new PlanetTile(tileId, layerId);
        }

        public static int IdOf(PlanetTile tile) => tile.Valid ? tile.tileId : -1;

        public static int LayerOf(PlanetTile tile) => tile.Valid ? tile.Layer?.LayerID ?? -1 : -1;

        /// <summary>Traversal distance, or -1 when either tile is invalid or they sit on different layers.</summary>
        public static int Distance(PlanetTile a, PlanetTile b, int maxDist = int.MaxValue)
        {
            if (!a.Valid || !b.Valid || a.Layer != b.Layer)
            {
                return -1;
            }
            return Find.WorldGrid.TraversalDistanceBetween(a, b, passImpassable: true, maxDist: maxDist);
        }
    }
}
