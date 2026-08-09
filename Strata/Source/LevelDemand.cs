using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // What a level is short of: construction materials, bill ingredients,
    // auto-refuel fuel, and items that would upgrade into higher-priority
    // storage here from a linked floor. The cross-level haul workgiver treats
    // another level's shortage as a pull. Cached per map on a slow cadence.
    public static class LevelDemand
    {
        private const int CacheTicks = 250;

        internal class Entry
        {
            public int tick;
            public readonly Dictionary<ThingDef, int> missing = new Dictionary<ThingDef, int>();
            // Construction / bills / refuel only — may pull from any local priority.
            public readonly Dictionary<ThingDef, int> hardMissing = new Dictionary<ThingDef, int>();
            public readonly Dictionary<ThingDef, List<IntVec3>> sites = new Dictionary<ThingDef, List<IntVec3>>();

            public void AddShortfall(ThingDef def, int amount, IntVec3 site, bool hardNeed = false)
            {
                missing.TryGetValue(def, out int total);
                missing[def] = total + amount;
                if (hardNeed)
                {
                    hardMissing.TryGetValue(def, out int hardTotal);
                    hardMissing[def] = hardTotal + amount;
                }
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

        // Construction, bill ingredients, or auto-refuel — may export from any
        // storage priority on the source floor. Storage-upgrade pulls are excluded.
        public static int HardMissingOn(Map map, ThingDef def)
        {
            return Get(map).hardMissing.TryGetValue(def, out int missing) ? missing : 0;
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

        // Every def any level linked to 'from' is short of. Snapshot the
        // ReachableLevels buffer first: Get → Build → AddStorageUpgradePulls
        // also calls ReachableLevels, which clears the shared list mid-foreach
        // (Collection was modified).
        public static HashSet<ThingDef> DefsWantedByLinkedLevels(Map from)
        {
            var wanted = new HashSet<ThingDef>();
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(from));
            for (int i = 0; i < links.Count; i++)
            {
                foreach (KeyValuePair<ThingDef, int> kv in Get(links[i].map).missing)
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
            if (entry.missing.Count > 0)
            {
                // What the level already has counts against construction/bill
                // shortage. Raw per-level counts: the combined-readout toggle
                // must never make a level look supplied by another level's stock.
                // Refuel/storage pulls are added after this so stockpiled fuel
                // on the reactor floor does not hide empty reactors, and apparel
                // sitting in a Normal dump does not hide a Critical armory.
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

                    if (entry.hardMissing.TryGetValue(def, out int hard))
                    {
                        int hardLeft = hard - have;
                        if (hardLeft > 0)
                        {
                            entry.hardMissing[def] = hardLeft;
                        }
                        else
                        {
                            entry.hardMissing.Remove(def);
                        }
                    }
                }
            }

            AddRefuelShortfalls(entry, map);
            AddStorageUpgradePulls(entry, map);
            return entry;
        }

        // Auto-refuel buildings (reactors, generators, etc.) pull their fuel
        // filter defs from linked floors. Added after inventory subtract so
        // uranium in a local stockpile does not cancel the reactor's need.
        private static void AddRefuelShortfalls(Entry entry, Map map)
        {
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing thing = buildings[i];
                if (thing.Faction != Faction.OfPlayer || thing is not ThingWithComps twc)
                {
                    continue;
                }

                CompRefuelable refuel = twc.GetComp<CompRefuelable>();
                if (refuel == null || !refuel.ShouldAutoRefuelNow)
                {
                    continue;
                }

                int need = refuel.GetFuelCountToFullyRefuel();
                if (need <= 0)
                {
                    continue;
                }

                ThingFilter filter = refuel.Props?.fuelFilter;
                if (filter == null)
                {
                    continue;
                }

                foreach (ThingDef def in filter.AllowedThingDefs)
                {
                    if (def == null || def.IsCorpse)
                    {
                        continue;
                    }

                    entry.AddShortfall(def, need, thing.Position, hardNeed: true);
                }
            }
        }

        // Higher-priority stockpiles on this map pull matching items that sit
        // in worse (or no) storage on linked floors — e.g. Critical armory on
        // A0 pulling weapons/clothes from a Normal crafting dump on A+.
        private static void AddStorageUpgradePulls(Entry entry, Map map)
        {
            const int MaxThingsPerLinkedMap = 100;
            List<SlotGroup> localGroups = map.haulDestinationManager.AllGroupsListForReading;
            if (localGroups.Count == 0)
            {
                return;
            }

            // Snapshot: nested demand Get/Build must not clobber this walk.
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(map));
            for (int i = 0; i < links.Count; i++)
            {
                Map other = links[i].map;
                if (other == null || other == map)
                {
                    continue;
                }

                int scanned = 0;
                ICollection<Thing> haulables = other.listerHaulables.ThingsPotentiallyNeedingHauling();
                foreach (Thing haulable in haulables)
                {
                    if (scanned >= MaxThingsPerLinkedMap)
                    {
                        break;
                    }

                    scanned++;
                    ConsiderOneStorageCandidate(entry, map, localGroups, haulable);
                }

                List<SlotGroup> otherGroups = other.haulDestinationManager.AllGroupsListForReading;
                for (int g = 0; g < otherGroups.Count && scanned < MaxThingsPerLinkedMap; g++)
                {
                    foreach (Thing t in otherGroups[g].HeldThings)
                    {
                        if (scanned >= MaxThingsPerLinkedMap)
                        {
                            break;
                        }

                        scanned++;
                        ConsiderOneStorageCandidate(entry, map, localGroups, t);
                    }
                }
            }
        }

        private static void ConsiderOneStorageCandidate(
            Entry entry,
            Map destMap,
            List<SlotGroup> destGroups,
            Thing t)
        {
            if (t == null || !t.Spawned || t.def.category != ThingCategory.Item)
            {
                return;
            }

            StoragePriority current = StoreUtility.CurrentStoragePriorityOf(t);
            if (!TryBestAcceptingCell(destMap, destGroups, t, current, out IntVec3 cell, out _))
            {
                return;
            }

            entry.AddShortfall(t.def, t.stackCount, cell);
        }

        private static bool TryBestAcceptingCell(
            Map map,
            List<SlotGroup> groups,
            Thing t,
            StoragePriority above,
            out IntVec3 cell,
            out StoragePriority priority)
        {
            cell = IntVec3.Invalid;
            priority = StoragePriority.Unstored;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                StoragePriority p = group.Settings.Priority;
                if (p <= above || p <= priority || !group.Settings.AllowedToAccept(t))
                {
                    continue;
                }

                List<IntVec3> cells = group.CellsList;
                int scan = System.Math.Min(cells.Count, 120);
                for (int j = 0; j < scan; j++)
                {
                    IntVec3 c = cells[j];
                    if (!StrataStorageSoftCompat.CellIsGoodStore(c, map, t))
                    {
                        continue;
                    }

                    cell = c;
                    priority = p;
                    break;
                }
            }

            return cell.IsValid;
        }

        private static void AddConstructibles(Entry entry, Map map, List<Thing> things)
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || thing.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                // Install/reinstall blueprints are not built from materials —
                // TotalMaterialCost() always Log.Errors. Prefer type-name checks
                // so a stale/ref isinst mismatch cannot leak through.
                if (IsInstallBlueprint(thing))
                {
                    continue;
                }
                if (thing is not IConstructible constructible)
                {
                    continue;
                }
                List<ThingDefCountClass> cost = constructible.TotalMaterialCost();
                if (cost == null)
                {
                    continue;
                }
                for (int j = 0; j < cost.Count; j++)
                {
                    ThingDef def = cost[j].thingDef;
                    int needed = constructible.ThingCountNeeded(def);
                    if (needed <= 0)
                    {
                        continue;
                    }
                    entry.AddShortfall(def, needed, thing.Position, hardNeed: true);
                }
            }
        }

        internal static bool IsInstallBlueprint(Thing thing)
        {
            if (thing is Blueprint_Install)
            {
                return true;
            }
            // Blueprint but not a material-cost build blueprint (install / mods).
            if (thing is Blueprint && thing is not Blueprint_Build)
            {
                return true;
            }
            System.Type type = thing.GetType();
            return type != null && type.Name == "Blueprint_Install";
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
