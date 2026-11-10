using System.Collections.Generic;
using Verse;

namespace Strata
{
    // Saved cross-floor jobs and links keep thing ids, not references. Looking an
    // id up walks every spawned thing, so remember the last hit per id and use it
    // while that thing is still spawned.
    public static class StrataThingLookup
    {
        private static readonly Dictionary<int, Thing> lastHit = new Dictionary<int, Thing>();

        public static void ResetSession()
        {
            lastHit.Clear();
        }

        // A spawned thing with this id on this map, or null.
        public static Thing FindOnMap(Map map, int thingId)
        {
            if (map == null || thingId < 0)
            {
                return null;
            }
            if (lastHit.TryGetValue(thingId, out Thing cached) && IsLive(cached, thingId))
            {
                return cached.Map == map ? cached : null;
            }
            Thing found = Scan(map, thingId);
            if (found != null)
            {
                lastHit[thingId] = found;
            }
            return found;
        }

        // A spawned thing with this id on any map, or null.
        public static Thing FindAcrossMaps(int thingId)
        {
            if (thingId < 0)
            {
                return null;
            }
            if (lastHit.TryGetValue(thingId, out Thing cached) && IsLive(cached, thingId))
            {
                return cached;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Thing found = Scan(maps[i], thingId);
                if (found != null)
                {
                    lastHit[thingId] = found;
                    return found;
                }
            }
            return null;
        }

        private static bool IsLive(Thing thing, int thingId)
        {
            return thing != null && thing.thingIDNumber == thingId && thing.Spawned && !thing.Destroyed
                && thing.Map != null && !thing.Map.Disposed;
        }

        private static Thing Scan(Map map, int thingId)
        {
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] != null && things[i].thingIDNumber == thingId)
                {
                    return things[i];
                }
            }
            return null;
        }
    }
}
