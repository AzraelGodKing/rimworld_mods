using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    // AZR-205 first slice — remember player caravan tiles as "the road".
    public class GameComponent_LivingWorldRoad : GameComponent
    {
        private List<int> roadTiles = new List<int>();

        public GameComponent_LivingWorldRoad(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref roadTiles, "lwRoadTiles", LookMode.Value);
            roadTiles ??= new List<int>();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 != 0)
            {
                return;
            }
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c == null || !c.IsPlayerControlled)
                {
                    continue;
                }
                int tile = c.Tile;
                if (!roadTiles.Contains(tile))
                {
                    roadTiles.Add(tile);
                    while (roadTiles.Count > 80)
                    {
                        roadTiles.RemoveAt(0);
                    }
                }
            }
        }

        public IReadOnlyList<int> Tiles => roadTiles;
    }
}
