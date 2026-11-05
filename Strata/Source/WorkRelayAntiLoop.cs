using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// Stops work-relay stair ping-pong: if a pawn arrives for Work and still
    /// finds no local job, blacklist that floor briefly so cheap probes cannot
    /// send them straight back.
    /// </summary>
    public static class WorkRelayAntiLoop
    {
        private const int BlacklistTicks = 5000;

        private const int MaxEntries = 512;

        private struct BlacklistEntry
        {
            public int mapId;
            public int untilTick;
            public int signalStamp;

            // Per WorkTypeDef.index work count when the floor was blacklisted,
            // -1 where that type had no work. Null when no board was built.
            public int[] counts;
        }

        private static readonly Dictionary<int, List<BlacklistEntry>> blacklists =
            new Dictionary<int, List<BlacklistEntry>>();

        internal static void ResetSession()
        {
            blacklists.Clear();
        }

        public static void ClearForMap(Map map)
        {
            if (map == null || blacklists.Count == 0)
            {
                return;
            }

            int mapId = map.uniqueID;
            List<int> emptyPawns = null;
            foreach (KeyValuePair<int, List<BlacklistEntry>> kv in blacklists)
            {
                List<BlacklistEntry> list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].mapId == mapId)
                    {
                        list.RemoveAt(i);
                    }
                }
                if (list.Count == 0)
                {
                    emptyPawns ??= new List<int>();
                    emptyPawns.Add(kv.Key);
                }
            }
            if (emptyPawns == null)
            {
                return;
            }
            for (int i = 0; i < emptyPawns.Count; i++)
            {
                blacklists.Remove(emptyPawns[i]);
            }
        }

        public static bool IsBlacklisted(Pawn pawn, Map map)
        {
            if (pawn == null || map == null
                || !blacklists.TryGetValue(pawn.thingIDNumber, out List<BlacklistEntry> list))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            int mapId = map.uniqueID;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].untilTick <= now)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (list[i].mapId == mapId)
                {
                    if (WorkGrew(pawn, map, list[i]))
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    return true;
                }
            }
            if (list.Count == 0)
            {
                blacklists.Remove(pawn.thingIDNumber);
            }
            return false;
        }

        public static void Blacklist(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
            {
                return;
            }
            if (blacklists.Count > MaxEntries)
            {
                blacklists.Clear();
            }

            int now = Find.TickManager.TicksGame;
            int mapId = map.uniqueID;
            WorkRelayBoard.TryGet(map, out WorkRelayBoard.BoardEntry board);
            int stamp = board?.signalStamp ?? 0;
            int[] counts = Snapshot(board);
            if (!blacklists.TryGetValue(pawn.thingIDNumber, out List<BlacklistEntry> list))
            {
                list = new List<BlacklistEntry>(2);
                blacklists[pawn.thingIDNumber] = list;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].mapId == mapId)
                {
                    list[i] = new BlacklistEntry
                    {
                        mapId = mapId,
                        untilTick = now + BlacklistTicks,
                        signalStamp = stamp,
                        counts = counts,
                    };
                    LogWaste(pawn, map);
                    return;
                }
            }
            list.Add(new BlacklistEntry
            {
                mapId = mapId,
                untilTick = now + BlacklistTicks,
                signalStamp = stamp,
                counts = counts,
            });
            LogWaste(pawn, map);
        }

        private static int[] Snapshot(WorkRelayBoard.BoardEntry board)
        {
            WorkRelayBoard.WorkTypeSignal[] byType = board?.byType;
            if (byType == null)
            {
                return null;
            }
            var counts = new int[byType.Length];
            for (int i = 0; i < byType.Length; i++)
            {
                counts[i] = byType[i].available ? byType[i].count : -1;
            }
            return counts;
        }

        // Lift the blacklist only when that floor gained work this pawn does:
        // a type that had none now has some, or its count went up. Any stamp
        // change used to lift it, so other pawns finishing jobs or a stack
        // being moved sent the pawn straight back to the floor it just left.
        private static bool WorkGrew(Pawn pawn, Map map, BlacklistEntry entry)
        {
            if (entry.signalStamp == 0 || entry.counts == null
                || !WorkRelayBoard.TryGet(map, out WorkRelayBoard.BoardEntry board)
                || board.signalStamp == 0 || board.signalStamp == entry.signalStamp
                || board.byType == null)
            {
                return false;
            }

            Pawn_WorkSettings work = pawn.workSettings;
            List<WorkTypeDef> types = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            int n = System.Math.Min(entry.counts.Length, board.byType.Length);
            for (int i = 0; i < n; i++)
            {
                WorkRelayBoard.WorkTypeSignal now = board.byType[i];
                if (!now.available || now.count <= entry.counts[i])
                {
                    continue;
                }
                WorkTypeDef type = i < types.Count ? types[i] : null;
                if (type == null || work == null || work.WorkIsActive(type))
                {
                    return true;
                }
            }
            return false;
        }

        private static void LogWaste(Pawn pawn, Map map)
        {
            if (StrataMod.Settings == null || !StrataMod.Settings.verboseLogging)
            {
                return;
            }
            string work = PortalRelayChain.IntendedWorkDefName(pawn);
            StrataLog.Verbose("[Strata] Empty work arrival for " + pawn.LabelShort
                + " on map " + map.uniqueID
                + (work != null ? " (intended " + work + ")" : "")
                + ". Blacklisted until the floor's work changes or 5000 ticks.");
        }
    }
}
