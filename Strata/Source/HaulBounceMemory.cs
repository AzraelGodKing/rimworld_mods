using System.Collections.Generic;
using Verse;

namespace Strata
{
    // AZR-632: a stack carried to another floor must not be auto-hauled
    // straight back to the floor it came from. Each leg looked fine on its
    // own (a demand pull down, then a storage upgrade or "needs haul" export
    // up), so a hauler could ride the stairs with the same items all day.
    // Keyed by the carried thing's id; forced hauls and hard needs ignore it.
    internal static class HaulBounceMemory
    {
        private const int WindowTicks = 15000;

        private const int MaxEntries = 1024;

        private struct Entry
        {
            public int fromMapId;
            public int tick;
        }

        private static readonly Dictionary<int, Entry> carried = new Dictionary<int, Entry>();

        internal static void ResetSession()
        {
            carried.Clear();
        }

        internal static void NoteCarriedOff(Thing t, Map from)
        {
            if (t == null || from == null)
            {
                return;
            }
            if (carried.Count >= MaxEntries)
            {
                Prune(Find.TickManager.TicksGame);
            }
            carried[t.thingIDNumber] = new Entry { fromMapId = from.uniqueID, tick = Find.TickManager.TicksGame };
        }

        // True while 't' recently arrived from 'dest' by a cross-floor haul.
        internal static bool WouldBounce(Thing t, Map dest)
        {
            if (t == null || dest == null || !carried.TryGetValue(t.thingIDNumber, out Entry e))
            {
                return false;
            }
            int now = Find.TickManager.TicksGame;
            if (now - e.tick >= WindowTicks || now < e.tick)
            {
                carried.Remove(t.thingIDNumber);
                return false;
            }
            return e.fromMapId == dest.uniqueID;
        }

        private static void Prune(int now)
        {
            var stale = new List<int>();
            foreach (KeyValuePair<int, Entry> kv in carried)
            {
                if (now - kv.Value.tick >= WindowTicks || now < kv.Value.tick)
                {
                    stale.Add(kv.Key);
                }
            }
            for (int i = 0; i < stale.Count; i++)
            {
                carried.Remove(stale[i]);
            }
            if (carried.Count >= MaxEntries)
            {
                carried.Clear();
            }
        }
    }
}
