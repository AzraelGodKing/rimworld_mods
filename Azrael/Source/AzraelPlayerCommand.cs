using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Enqueue safe-removal map mutations from Mod Options; drain on GameComponentTick
    /// (same MP-safer pattern as DeepColonyPlayerCommand; no Multiplayer.API).
    /// </summary>
    internal static class AzraelPlayerCommand
    {
        private struct Entry
        {
            public HashSet<int> thingIds;
            public string display;
        }

        private static readonly List<Entry> queue = new List<Entry>();

        public static void EnqueueDeconstruct(List<Thing> things, string display)
        {
            if (things == null || things.Count == 0)
            {
                return;
            }
            var ids = new HashSet<int>();
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] != null)
                {
                    ids.Add(things[i].thingIDNumber);
                }
            }
            queue.Add(new Entry { thingIds = ids, display = display });
        }

        public static void Drain()
        {
            if (queue.Count == 0)
            {
                return;
            }
            List<Entry> batch = new List<Entry>(queue);
            queue.Clear();
            for (int i = 0; i < batch.Count; i++)
            {
                int n = SeriesRemoval.ApplyDeconstruct(FindBuildings(batch[i].thingIds));
                Messages.Message("Azrael_Removal_Designated".Translate(n, batch[i].display),
                    MessageTypeDefOf.TaskCompletion);
            }
        }

        public static void Clear()
        {
            queue.Clear();
        }

        private static List<Thing> FindBuildings(HashSet<int> ids)
        {
            var found = new List<Thing>();
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Thing> buildings = maps[m]?.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                if (buildings == null)
                {
                    continue;
                }
                for (int i = 0; i < buildings.Count; i++)
                {
                    Thing t = buildings[i];
                    if (t != null && ids.Contains(t.thingIDNumber))
                    {
                        found.Add(t);
                    }
                }
            }
            return found;
        }
    }
}
