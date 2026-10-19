using System.Collections.Generic;
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
                    if (WorkChanged(map, list[i].signalStamp))
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
            int stamp = CurrentStamp(map);
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
            });
            LogWaste(pawn, map);
        }

        private static int CurrentStamp(Map map)
        {
            return WorkRelayBoard.TryGet(map, out WorkRelayBoard.BoardEntry entry) ? entry.signalStamp : 0;
        }

        // A rebuilt board with a different stamp means new work appeared.
        private static bool WorkChanged(Map map, int stamped)
        {
            if (stamped == 0 || !WorkRelayBoard.TryGet(map, out WorkRelayBoard.BoardEntry entry))
            {
                return false;
            }
            return entry.signalStamp != 0 && entry.signalStamp != stamped;
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
