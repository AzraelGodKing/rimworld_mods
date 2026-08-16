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

        // FloatMenuOptionProvider_WorkGivers.ScannerShouldSkip only considers a
        // scanner when PotentialWorkThingRequest accepts the click target OR the
        // thing appears in PotentialWorkThingsGlobal. Without this, haulables
        // that fall out of our global scan never even get HasJobOnThing.
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

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
                    foreach (Thing held in group.HeldThings)
                    {
                        yield return held;
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
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(from);
            for (int i = 0; i < links.Count; i++)
            {
                List<SlotGroup> groups = links[i].map.haulDestinationManager.AllGroupsListForReading;
                for (int g = 0; g < groups.Count; g++)
                {
                    if (groups[g].Settings.Priority > max)
                    {
                        max = groups[g].Settings.Priority;
                    }
                }
            }
            return max;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.haulAcrossLevelsEnabled)
            {
                return true;
            }
            return !LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryFindHaulTarget(pawn, t, forced, out _, out _, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!TryFindHaulTarget(pawn, t, forced, out MapPortal portal, out Map destMap, out IntVec3 storeCell))
            {
                return null;
            }

            HaulToLevelTargets.Remember(pawn, destMap, pawn.Map, preferArrivalNear: storeCell);
            Job job = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, t, portal);
            job.count = t.stackCount;
            return job;
        }

        private static bool TryFindHaulTarget(
            Pawn pawn,
            Thing t,
            bool forced,
            out MapPortal portal,
            out Map destMap,
            out IntVec3 storeCell)
        {
            portal = null;
            destMap = null;
            storeCell = IntVec3.Invalid;
            if (t == null || !t.Spawned || t.Map != pawn.Map)
            {
                return false;
            }
            // Auto: full vanilla "needs haul" gate (designations, forbidden, etc.).
            // Forced/prioritize: Fast only, same as WorkGiver_Haul.JobOnThing.
            if (forced)
            {
                if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: true))
                {
                    return false;
                }
            }
            else if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t, forced: false))
            {
                return false;
            }
            // Hard need keeps first claim on auto-haul; prioritize may still export.
            if (!forced && LevelDemand.HardMissingOn(pawn.Map, t.def) > 0)
            {
                return false;
            }
            if (!forced && LevelDemand.MissingOn(pawn.Map, t.def) > 0)
            {
                StoragePriority here = StoreUtility.CurrentStoragePriorityOf(t);
                if (HasLocalBetterStore(pawn, t, here, forced))
                {
                    return false;
                }
            }

            StoragePriority current = StoreUtility.CurrentStoragePriorityOf(t, forced);
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);

            // Demand pull: hard construction / bills, or storage-upgrade sites.
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (!forced && StrataStorageSoftCompat.IsDestCoolingDown(link.map, t.def))
                {
                    continue;
                }

                bool hardNeed = LevelDemand.HardMissingOn(link.map, t.def) > 0;
                bool softNeed = !hardNeed && LevelDemand.MissingOn(link.map, t.def) > 0;
                if (!hardNeed && !softNeed)
                {
                    continue;
                }
                if (!LevelDemand.AnySiteReachable(link.map, t.def, link.arrivalCell)
                    && !hardNeed)
                {
                    // Soft upgrade sites still need a reachable demand site; hard
                    // need can use any accepting store with a complete stair path.
                    continue;
                }

                StoragePriority minBeat = hardNeed ? StoragePriority.Unstored : current;
                if (!TryFindStoreWithPath(
                        pawn, link.map, t, minBeat, forced,
                        out StoragePriority p, out MapPortal step, out IntVec3 cell))
                {
                    continue;
                }
                if (!hardNeed && p <= current)
                {
                    continue;
                }

                portal = step;
                destMap = link.map;
                storeCell = cell;
                return true;
            }

            // Priority upgrade / "needs haul" export: same bar as vanilla
            // HaulToStorageJob — beat the best empty cell on this map. When this
            // map has no empty accepting spot (the grey "needs haul" case), any
            // linked store with a complete stair path wins, just like that
            // stockpile would if it were on this floor.
            StoragePriority localBest = current;
            bool localHasSpot = StoreUtility.TryFindBestBetterStorageFor(
                t, pawn, pawn.Map, current, pawn.Faction,
                out IntVec3 localCell, out IHaulDestination localDest, needAccurateResult: false);
            if (localHasSpot)
            {
                if (localCell.IsValid)
                {
                    localBest = localCell.GetSlotGroup(pawn.Map)?.Settings?.Priority ?? localBest;
                }
                else if (localDest != null)
                {
                    localBest = localDest.GetStoreSettings().Priority;
                }
            }
            else if (current == StoragePriority.Unstored || !t.IsInValidStorage())
            {
                // Needs haul / no empty spot here — any linked accepting store
                // with a complete path is fair game (vanilla same-map rule).
                localBest = StoragePriority.Unstored;
            }

            MapPortal bestStep = null;
            Map bestMap = null;
            IntVec3 bestCell = IntVec3.Invalid;
            StoragePriority bestPriority = localBest;
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (!forced && StrataStorageSoftCompat.IsDestCoolingDown(link.map, t.def))
                {
                    continue;
                }

                if (!TryFindStoreWithPath(
                        pawn, link.map, t, bestPriority, forced,
                        out StoragePriority p, out MapPortal step, out IntVec3 cell))
                {
                    continue;
                }
                if (p > bestPriority)
                {
                    bestStep = step;
                    bestMap = link.map;
                    bestCell = cell;
                    bestPriority = p;
                }
            }

            if (bestStep == null)
            {
                if (forced && links.Count > 0)
                {
                    JobFailReason.Is(HaulAIUtility.NoEmptyPlaceLowerTrans);
                }
                return false;
            }

            portal = bestStep;
            destMap = bestMap;
            storeCell = bestCell;
            return true;
        }

        // Same-map empty cell better than 'above' (soft demand keep-local).
        private static bool HasLocalBetterStore(Pawn pawn, Thing t, StoragePriority above, bool forced)
        {
            Map map = pawn.Map;
            Danger maxDanger = forced ? Danger.Deadly : Danger.Some;
            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                if (group.Settings.Priority <= above || !group.Settings.AllowedToAccept(t))
                {
                    continue;
                }
                List<IntVec3> cells = group.CellsList;
                int scan = Math.Min(cells.Count, MaxCellsScannedPerGroup);
                for (int j = 0; j < scan; j++)
                {
                    IntVec3 cell = cells[j];
                    if (StrataStorageSoftCompat.CellIsGoodStore(cell, map, t, pawn)
                        && pawn.CanReach(cell, PathEndMode.Touch, maxDanger))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Highest accepting priority above 'above' that has an empty cell AND a
        // complete path: pawn → enterable shaft → landing → store cell.
        private static bool TryFindStoreWithPath(
            Pawn pawn,
            Map map,
            Thing t,
            StoragePriority above,
            bool forced,
            out StoragePriority priority,
            out MapPortal step,
            out IntVec3 storeCell)
        {
            priority = StoragePriority.Unstored;
            step = null;
            storeCell = IntVec3.Invalid;
            if (pawn?.Map == null || map == null || t == null || pawn.Map == map)
            {
                return false;
            }

            Pawn storeCarrier = null;
            Danger maxDanger = forced ? Danger.Deadly : Danger.Some;
            StoragePriority bestPriority = StoragePriority.Unstored;
            MapPortal bestStep = null;
            IntVec3 bestCell = IntVec3.Invalid;

            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                StoragePriority groupPriority = group.Settings.Priority;
                if (groupPriority <= above || groupPriority < bestPriority
                    || !group.Settings.AllowedToAccept(t))
                {
                    continue;
                }
                if (group.parent is Thing parentThing
                    && parentThing.Faction != null
                    && parentThing.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                if (!group.parent.HaulDestinationEnabled)
                {
                    continue;
                }

                List<IntVec3> cells = group.CellsList;
                int scan = Math.Min(cells.Count, MaxCellsScannedPerGroup);
                for (int j = 0; j < scan; j++)
                {
                    IntVec3 cell = cells[j];
                    if (!StrataStorageSoftCompat.CellIsGoodStore(cell, map, t, storeCarrier))
                    {
                        continue;
                    }

                    MapPortal portal = FindStepWithCompletePath(pawn, map, cell, maxDanger);
                    if (portal == null)
                    {
                        continue;
                    }

                    if (groupPriority == bestPriority && bestStep != null
                        && pawn.Position.DistanceToSquared(portal.Position)
                            >= pawn.Position.DistanceToSquared(bestStep.Position))
                    {
                        continue;
                    }

                    bestPriority = groupPriority;
                    bestStep = portal;
                    bestCell = cell;
                    break;
                }
            }

            if (bestStep != null)
            {
                priority = bestPriority;
                step = bestStep;
                storeCell = bestCell;
                return true;
            }

            List<IHaulDestination> destinations =
                map.haulDestinationManager.AllHaulDestinationsListInPriorityOrder;
            for (int i = 0; i < destinations.Count; i++)
            {
                IHaulDestination dest = destinations[i];
                if (dest is ISlotGroupParent || !dest.HaulDestinationEnabled || !dest.Accepts(t))
                {
                    continue;
                }
                StoragePriority destPriority = dest.GetStoreSettings().Priority;
                if (destPriority <= above || destPriority < bestPriority)
                {
                    continue;
                }
                if (dest is not Thing destThing || !destThing.Spawned || destThing.Map != map)
                {
                    continue;
                }

                MapPortal portal = FindStepWithCompletePath(pawn, map, destThing.Position, maxDanger);
                if (portal == null)
                {
                    continue;
                }

                bestPriority = destPriority;
                bestStep = portal;
                bestCell = destThing.Position;
            }

            if (bestStep == null)
            {
                return false;
            }

            priority = bestPriority;
            step = bestStep;
            storeCell = bestCell;
            return true;
        }

        // Pawn can reach the shaft, shaft is enterable, and the landing can walk
        // to the store cell. Never pick an open ancient stair into a sealed bubble.
        private static MapPortal FindStepWithCompletePath(
            Pawn pawn,
            Map destMap,
            IntVec3 storeCell,
            Danger maxDanger)
        {
            MapPortal required = LevelGraph.BestFirstStepRequiringArrival(
                pawn.Map, destMap, pawn.Position, pawn, storeCell);
            if (required == null
                || !required.Spawned
                || !required.IsEnterable(out _)
                || !pawn.CanReach(required, PathEndMode.Touch, maxDanger))
            {
                return null;
            }

            // Direct hop onto destMap: re-verify landing → store (ancient shafts
            // are enterable but often dump into disconnected rock).
            Map other = null;
            try
            {
                other = required.GetOtherMap();
            }
            catch
            {
                other = null;
            }

            if (other == destMap)
            {
                IntVec3 arrival = required.GetDestinationLocation();
                if (!arrival.IsValid || !arrival.InBounds(destMap)
                    || !destMap.reachability.CanReach(
                        arrival, storeCell, PathEndMode.Touch,
                        TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, canBashDoors: false)))
                {
                    return null;
                }
            }

            return required;
        }
    }
}
