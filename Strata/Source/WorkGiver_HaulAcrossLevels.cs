using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Hauls items to a linked level whose storage beats anything available on
    // their own level, honoring storage priority across the whole level graph.
    // Runs just above HaulGeneral (priorityInType 16 vs 15) so a Critical
    // stockpile downstairs wins over a Low one here, exactly like vanilla
    // priority does within one map. Only claims things whose best storage is
    // on another level; everything else falls through to normal hauling.
    public class WorkGiver_HaulAcrossLevels : WorkGiver_Scanner
    {
        private const int MaxCellsScannedPerGroup = 120;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            Map map = pawn.Map;
            foreach (Thing t in map.listerHaulables.ThingsPotentiallyNeedingHauling())
            {
                yield return t;
            }
            // Materialize both before yielding: ReachableLevels reuses a shared
            // buffer that HasJobOnThing clobbers between yields. Demand must
            // not be gated on maxLinked - a fresh level below with blueprints
            // and no stockpiles yet is exactly when demand matters most.
            StoragePriority maxLinked = MaxStoragePriorityOnLinkedLevels(map);
            HashSet<ThingDef> wanted = LevelDemand.DefsWantedByLinkedLevels(map);
            if (maxLinked == StoragePriority.Unstored && wanted.Count == 0)
            {
                yield break;
            }
            // listerHaulables drops an item once it sits in storage with no
            // better cell on its own map, so priority upgrades to another
            // level have to scan stored things directly.
            if (maxLinked > StoragePriority.Unstored)
            {
                List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
                for (int i = 0; i < groups.Count; i++)
                {
                    SlotGroup group = groups[i];
                    if (group.Settings.Priority >= maxLinked)
                    {
                        continue;
                    }
                    foreach (Thing t in group.HeldThings)
                    {
                        yield return t;
                    }
                }
            }
            // Materials another level's construction is short of - loose or in
            // storage at any priority, neither of which is guaranteed to appear
            // in the yields above.
            foreach (ThingDef def in wanted)
            {
                List<Thing> ofDef = map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < ofDef.Count; i++)
                {
                    yield return ofDef[i];
                }
            }
        }

        private static StoragePriority MaxStoragePriorityOnLinkedLevels(Map from)
        {
            StoragePriority max = StoragePriority.Unstored;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(from))
            {
                List<SlotGroup> groups = link.map.haulDestinationManager.AllGroupsListForReading;
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].Settings.Priority > max)
                    {
                        max = groups[i].Settings.Priority;
                    }
                }
            }
            return max;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryFindHaulTarget(pawn, t, forced, out _, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!TryFindHaulTarget(pawn, t, forced, out MapPortal portal, out Map destMap))
            {
                return null;
            }

            HaulToLevelTargets.Remember(pawn, destMap, pawn.Map);
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, t, portal);
            job.count = t.stackCount;
            return job;
        }

        private static bool TryFindHaulTarget(
            Pawn pawn,
            Thing t,
            bool forced,
            out MapPortal portal,
            out Map destMap)
        {
            portal = null;
            destMap = null;
            if (t == null || !t.Spawned || t.Map != pawn.Map)
            {
                return false;
            }
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
            {
                return false;
            }
            // Construction demand outranks storage priority: a level short of
            // materials for its blueprints pulls them like a temporary Critical
            // stockpile. The pawn's own level keeps first claim - while local
            // construction is short of this def, it never gets exported.
            if (LevelDemand.MissingOn(pawn.Map, t.def) > 0)
            {
                return false;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (LevelDemand.MissingOn(link.map, t.def) > 0
                    && LevelDemand.AnySiteReachable(link.map, t.def, link.arrivalCell))
                {
                    MapPortal step = UsableStep(pawn, link);
                    if (step != null)
                    {
                        portal = step;
                        destMap = link.map;
                        return true;
                    }
                }
            }

            StoragePriority current = StoreUtility.CurrentStoragePriorityOf(t);
            // The best this level can offer; a linked level only gets the job
            // if it strictly beats it, so ties stay local (shorter trip) and
            // vanilla hauling handles them.
            StoragePriority localBest = current;
            if (StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, current, pawn.Faction, out IntVec3 localCell, needAccurateResult: false))
            {
                localBest = localCell.GetSlotGroup(pawn.Map)?.Settings?.Priority ?? localBest;
            }
            // Take the level with the highest accepting priority; BFS order is
            // nearest-first, so ties go to the closest level.
            MapPortal best = null;
            Map bestMap = null;
            StoragePriority bestPriority = localBest;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                StoragePriority p = BestAcceptingPriority(link.map, t, bestPriority, link.arrivalCell);
                if (p > bestPriority)
                {
                    MapPortal step = UsableStep(pawn, link);
                    if (step != null)
                    {
                        best = step;
                        bestMap = link.map;
                        bestPriority = p;
                    }
                }
            }

            if (best == null)
            {
                return false;
            }

            portal = best;
            destMap = bestMap;
            return true;
        }

        // The best portal for this pawn toward the link's level - nearest,
        // powered elevators preferred - that is actually usable right now.
        private static MapPortal UsableStep(Pawn pawn, LevelGraph.LevelLink link)
        {
            MapPortal step = LevelGraph.BestFirstStep(pawn.Map, link.map, pawn.Position) ?? link.firstStep;
            if (step.Spawned && step.IsEnterable(out _)
                && pawn.CanReach(step, PathEndMode.Touch, Danger.Some))
            {
                return step;
            }
            return null;
        }

        // The highest storage-group priority above 'above' that accepts the
        // thing and has a cell with room that is walkable from the arrival
        // landing - a freshly broken-through landing sits in a sealed rock
        // bubble, and cargo must not be shipped somewhere it can only pile up.
        // Final placement is deferred haul delivery after portal arrival.
        private static StoragePriority BestAcceptingPriority(Map map, Thing t, StoragePriority above, IntVec3 arrivalCell)
        {
            if (!arrivalCell.IsValid || !arrivalCell.InBounds(map))
            {
                return StoragePriority.Unstored;
            }
            StoragePriority best = StoragePriority.Unstored;
            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                StoragePriority priority = group.Settings.Priority;
                if (priority <= above || priority <= best || !group.Settings.AllowedToAccept(t))
                {
                    continue;
                }
                List<IntVec3> cells = group.CellsList;
                int scan = Math.Min(cells.Count, MaxCellsScannedPerGroup);
                for (int j = 0; j < scan; j++)
                {
                    if (CellHasRoomFor(cells[j], map, t)
                        && map.reachability.CanReach(arrivalCell, cells[j], PathEndMode.Touch,
                            TraverseParms.For(TraverseMode.PassDoors)))
                    {
                        best = priority;
                        break;
                    }
                }
            }
            return best;
        }

        private static bool CellHasRoomFor(IntVec3 cell, Map map, Thing t)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing other = things[i];
                if (other.def.EverStorable(willMinifyIfPossible: false))
                {
                    return other.CanStackWith(t) && other.stackCount < other.def.stackLimit;
                }
            }
            return true;
        }
    }
}
