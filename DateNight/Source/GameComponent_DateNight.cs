using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    public class GameComponent_DateNight : GameComponent
    {
        private readonly Dictionary<int, bool> wasOnLovin = new Dictionary<int, bool>();

        public GameComponent_DateNight(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                List<Pawn> colonists = map?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int i = 0; i < colonists.Count; i++)
                {
                    TickPawn(colonists[i]);
                }
            }
        }

        private void TickPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            bool onLovin = DateNightUtility.IsLovinSchedule(pawn);
            int id = pawn.thingIDNumber;
            wasOnLovin.TryGetValue(id, out bool was);

            if (onLovin && !was)
            {
                DateNightUtility.NotifyEnteredLovinSchedule(pawn);
            }
            else if (!onLovin && was)
            {
                DateNightUtility.NotifyLeftLovinSchedule(pawn);
            }

            wasOnLovin[id] = onLovin;

            if (onLovin)
            {
                DateNightUtility.TickScheduledPawn(pawn);
            }
        }
    }
}
