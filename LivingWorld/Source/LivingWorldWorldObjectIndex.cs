using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Way-camp tiles and the live traffic count, rebuilt from the world only when a
    /// Living World object is added or removed (or another game is loaded), instead
    /// of scanning every world object from hot paths such as Need_Rest.
    /// </summary>
    internal static class LivingWorldWorldObjectIndex
    {
        private static readonly HashSet<PlanetTile> campTiles = new HashSet<PlanetTile>();
        private static int campCount;
        private static int trafficCount;
        private static Game builtFor;
        private static bool dirty = true;

        public static void MarkDirty()
        {
            dirty = true;
        }

        public static bool IsCampTile(PlanetTile tile)
        {
            EnsureBuilt();
            return campTiles.Contains(tile);
        }

        public static int CampCount
        {
            get
            {
                EnsureBuilt();
                return campCount;
            }
        }

        public static int TrafficCount
        {
            get
            {
                EnsureBuilt();
                return trafficCount;
            }
        }

        private static void EnsureBuilt()
        {
            Game game = Current.Game;
            if (!dirty && game == builtFor)
            {
                return;
            }
            builtFor = game;
            dirty = false;
            campTiles.Clear();
            campCount = 0;
            trafficCount = 0;
            List<WorldObject> all = Find.World?.worldObjects?.AllWorldObjects;
            if (all == null)
            {
                // World not ready yet; try again next call.
                dirty = true;
                return;
            }
            for (int i = 0; i < all.Count; i++)
            {
                switch (all[i])
                {
                    case WorldObject_LivingWorldWayCamp camp:
                        campTiles.Add(camp.Tile);
                        campCount++;
                        break;
                    case WorldObject_LivingWorldTraffic:
                        trafficCount++;
                        break;
                }
            }
        }
    }
}
