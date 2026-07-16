using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // What a level is short of for its construction: blueprints and frames
    // wanting materials the level doesn't have. The cross-level haul workgiver
    // treats another level's shortage as a pull, so builders stop idling next
    // to a stairwell while the steel sits one floor up. Cached per map and
    // refreshed on a slow cadence; over-shipping self-corrects on the next
    // refresh once the delivered goods land and count toward the level's
    // inventory.
    public static class LevelDemand
    {
        private const int CacheTicks = 250;

        internal class Entry
        {
            public int tick;
            public readonly Dictionary<ThingDef, int> missing = new Dictionary<ThingDef, int>();
            public readonly Dictionary<ThingDef, List<IntVec3>> sites = new Dictionary<ThingDef, List<IntVec3>>();

            public void AddShortfall(ThingDef def, int amount, IntVec3 site)
            {
                missing.TryGetValue(def, out int total);
                missing[def] = total + amount;
                if (!sites.TryGetValue(def, out List<IntVec3> list))
                {
                    sites[def] = list = new List<IntVec3>();
                }
                list.Add(site);
            }
        }

        private static readonly Dictionary<Map, Entry> cache = new Dictionary<Map, Entry>();

        // How many more of this def the level's construction needs than it has.
        public static int MissingOn(Map map, ThingDef def)
        {
            return Get(map).missing.TryGetValue(def, out int missing) ? missing : 0;
        }

        // Whether any site that needs the def can be reached from a cell -
        // used with the arrival landing so materials are never shipped to a
        // level where they can only pile up.
        public static bool AnySiteReachable(Map map, ThingDef def, IntVec3 from)
        {
            if (!from.IsValid || !from.InBounds(map)
                || !Get(map).sites.TryGetValue(def, out List<IntVec3> sites))
            {
                return false;
            }
            for (int i = 0; i < sites.Count; i++)
            {
                if (map.reachability.CanReach(from, sites[i], PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors)))
                {
                    return true;
                }
            }
            return false;
        }

        // Every def any level linked to 'from' is short of. Materialized into
        // a fresh set because callers iterate it while LevelGraph's shared
        // buffer gets reused underneath them.
        public static HashSet<ThingDef> DefsWantedByLinkedLevels(Map from)
        {
            var wanted = new HashSet<ThingDef>();
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(from))
            {
                foreach (KeyValuePair<ThingDef, int> kv in Get(link.map).missing)
                {
                    wanted.Add(kv.Key);
                }
            }
            return wanted;
        }

        private static Entry Get(Map map)
        {
            if (cache.TryGetValue(map, out Entry entry)
                && Find.TickManager.TicksGame - entry.tick < CacheTicks)
            {
                return entry;
            }
            entry = Build(map);
            cache[map] = entry;
            PruneDeadMaps();
            return entry;
        }

        private static Entry Build(Map map)
        {
            var entry = new Entry { tick = Find.TickManager.TicksGame };
            AddConstructibles(entry, map, map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint));
            AddConstructibles(entry, map, map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame));
            BillIngredientUtility.AddShortfalls(entry, map);
            if (entry.missing.Count == 0)
            {
                return entry;
            }
            // What the level already has counts against the shortage. Raw
            // per-level counts: the combined-readout toggle must never make a
            // level look supplied by another level's stock.
            foreach (ThingDef def in new List<ThingDef>(entry.missing.Keys))
            {
                int have = def.CountAsResource
                    ? StrataResources.RawGetCount(map, def)
                    : CountOnMap(map, def);
                int missing = entry.missing[def] - have;
                if (missing > 0)
                {
                    entry.missing[def] = missing;
                }
                else
                {
                    entry.missing.Remove(def);
                    entry.sites.Remove(def);
                }
            }
            return entry;
        }

        private static void AddConstructibles(Entry entry, Map map, List<Thing> things)
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                // Install/reinstall blueprints reference a specific minified
                // building; TotalMaterialCost logs an error and returns nothing.
                // Cross-level haul for those uses vanilla install hauling.
                if (thing.Faction != Faction.OfPlayer || thing is Blueprint_Install
                    || !(thing is IConstructible constructible))
                {
                    continue;
                }
                List<ThingDefCountClass> cost = constructible.TotalMaterialCost();
                for (int j = 0; j < cost.Count; j++)
                {
                    ThingDef def = cost[j].thingDef;
                    int needed = constructible.ThingCountNeeded(def);
                    if (needed <= 0)
                    {
                        continue;
                    }
                    entry.missing.TryGetValue(def, out int total);
                    entry.missing[def] = total + needed;
                    if (!entry.sites.TryGetValue(def, out List<IntVec3> sites))
                    {
                        entry.sites[def] = sites = new List<IntVec3>();
                    }
                    sites.Add(thing.Position);
                }
            }
        }

        private static int CountOnMap(Map map, ThingDef def)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                count += things[i].stackCount;
            }
            return count;
        }

        private static void PruneDeadMaps()
        {
            if (cache.Count <= Find.Maps.Count)
            {
                return;
            }
            var dead = new List<Map>();
            foreach (Map key in cache.Keys)
            {
                if (!Find.Maps.Contains(key))
                {
                    dead.Add(key);
                }
            }
            for (int i = 0; i < dead.Count; i++)
            {
                cache.Remove(dead[i]);
            }
        }
    }
}
