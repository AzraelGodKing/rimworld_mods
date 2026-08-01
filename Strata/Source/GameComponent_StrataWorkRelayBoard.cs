using System.Collections.Generic;
using Verse;

namespace Strata
{
    /// <summary>
    /// Pushes per-map work boards ahead of pawn think so work relay only reads.
    /// </summary>
    public class GameComponent_StrataWorkRelayBoard : GameComponent
    {
        private const int ScanIntervalTicks = 300;

        private const int MaxRebuildsPerTick = 2;

        private int nextScanTick;

        private readonly List<Map> scratchMaps = new List<Map>();

        public GameComponent_StrataWorkRelayBoard(Game game)
        {
        }

        public override void GameComponentTick()
        {
            WorkRelayBoardBuilder.DrainCompleted();

            if (StrataMod.Settings == null || !StrataMod.Settings.WorkRelayActive)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now < nextScanTick)
            {
                return;
            }
            nextScanTick = now + ScanIntervalTicks;

            List<Map> maps = Find.Maps;
            if (maps == null || maps.Count == 0)
            {
                return;
            }

            scratchMaps.Clear();
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !LevelGraph.AnyLinkFrom(map))
                {
                    continue;
                }
                if (NeedsRebuild(map))
                {
                    scratchMaps.Add(map);
                }
            }

            int rebuilt = 0;
            for (int i = 0; i < scratchMaps.Count && rebuilt < MaxRebuildsPerTick; i++)
            {
                Map map = scratchMaps[i];
                if (WorkRelayBoardBuilder.HasPending(map.uniqueID))
                {
                    continue;
                }
                if (WorkRelayBoardBuilder.OffThreadActive)
                {
                    WorkRelayBoardBuilder.TryEnqueueOrPublish(map);
                }
                else
                {
                    WorkRelayBoardBuilder.BuildAndPublishSync(map);
                }
                rebuilt++;
            }
        }

        private static bool NeedsRebuild(Map map)
        {
            if (WorkRelayBoard.IsDirty(map))
            {
                return true;
            }
            if (!WorkRelayBoard.TryGetFresh(map, WorkRelaySignals.BoardFreshTicks, out _))
            {
                return true;
            }
            return false;
        }
    }
}
