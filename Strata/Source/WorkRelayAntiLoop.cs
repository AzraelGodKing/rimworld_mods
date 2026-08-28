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
        }

        private static readonly Dictionary<int, List<BlacklistEntry>> blacklists =
            new Dictionary<int, List<BlacklistEntry>>();

        private static readonly HashSet<int> pendingEmptyCheck = new HashSet<int>();

        [StrataSessionReset]
        internal static void ResetSession()
        {
            blacklists.Clear();
            pendingEmptyCheck.Clear();
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

        public static void MarkPendingEmptyCheck(Pawn pawn)
        {
            if (pawn != null)
            {
                pendingEmptyCheck.Add(pawn.thingIDNumber);
            }
        }

        public static bool ConsumePendingEmptyCheck(Pawn pawn)
        {
            return pawn != null && pendingEmptyCheck.Remove(pawn.thingIDNumber);
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
                    };
                    return;
                }
            }
            list.Add(new BlacklistEntry
            {
                mapId = mapId,
                untilTick = now + BlacklistTicks,
            });
        }
    }
}
