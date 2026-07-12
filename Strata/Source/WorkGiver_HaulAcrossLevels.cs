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
            // listerHaulables drops an item once it sits in storage with no
            // better cell on its own map, so priority upgrades to another
            // level have to scan stored things directly. Materialize the max
            // linked priority before yielding: ReachableLevels reuses a shared
            // buffer that HasJobOnThing clobbers between yields.
            StoragePriority maxLinked = MaxStoragePriorityOnLinkedLevels(map);
            if (maxLinked == StoragePriority.Unstored)
            {
                yield break;
            }
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
            return FindTargetPortal(pawn, t, forced) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            MapPortal portal = FindTargetPortal(pawn, t, forced);
            if (portal == null)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, t, portal);
            job.count = t.stackCount;
            return job;
        }

        private static MapPortal FindTargetPortal(Pawn pawn, Thing t, bool forced)
        {
            if (t == null || !t.Spawned || t.Map != pawn.Map)
            {
                return null;
            }
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
            {
                return null;
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
            StoragePriority bestPriority = localBest;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                StoragePriority p = BestAcceptingPriority(link.map, t, bestPriority, link.arrivalCell);
                if (p > bestPriority
                    && link.firstStep.Spawned
                    && link.firstStep.IsEnterable(out _)
                    && pawn.CanReach(link.firstStep, PathEndMode.Touch, Danger.Some))
                {
                    best = link.firstStep;
                    bestPriority = p;
                }
            }
            return best;
        }

        // The highest storage-group priority above 'above' that accepts the
        // thing and has a cell with room that is walkable from the arrival
        // landing - a freshly broken-through landing sits in a sealed rock
        // bubble, and cargo must not be shipped somewhere it can only pile up.
        // Final placement is vanilla hauling after arrival.
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
