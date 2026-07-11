using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Hauls items that have no valid storage on their own level to a linked
    // level whose storage accepts them. Runs below normal hauling priority, so
    // same-level hauling always wins when it can.
    public class WorkGiver_HaulAcrossLevels : WorkGiver_Scanner
    {
        private const int MaxCellsScannedPerGroup = 120;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
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
            // If this level can store it, leave the job to normal hauling.
            if (StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, current, pawn.Faction, out _, needAccurateResult: false))
            {
                return null;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (StorageAccepts(link.map, t, current)
                    && link.firstStep.Spawned
                    && link.firstStep.IsEnterable(out _)
                    && pawn.CanReach(link.firstStep, PathEndMode.Touch, Danger.Some))
                {
                    return link.firstStep;
                }
            }
            return null;
        }

        // Cheap destination check that never touches cross-map reachability:
        // a higher-priority storage group that accepts the thing and has a cell
        // with room. Final placement is vanilla hauling after arrival.
        private static bool StorageAccepts(Map map, Thing t, StoragePriority currentPriority)
        {
            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                if (group.Settings.Priority <= currentPriority || !group.Settings.AllowedToAccept(t))
                {
                    continue;
                }
                List<IntVec3> cells = group.CellsList;
                int scan = Math.Min(cells.Count, MaxCellsScannedPerGroup);
                for (int j = 0; j < scan; j++)
                {
                    if (CellHasRoomFor(cells[j], map, t))
                    {
                        return true;
                    }
                }
            }
            return false;
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
