using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    /// <summary>
    /// Cheap "is there plausibly work on this map for this pawn?" probes used by
    /// the work relay. Map signals live on <see cref="WorkRelayBoard"/> (scanner
    /// push + sync fallback); false positives cost a stair walk + blacklist.
    /// </summary>
    public static class WorkRelaySignals
    {
        public delegate bool WorkProbe(Pawn pawn, Map map);

        /// <summary>Max age for a published board entry before the scanner/rebuild refreshes.</summary>
        public const int BoardFreshTicks = 600;

        private const int WorkClaimCapMin = 3;

        private const int WorkClaimCapMax = 8;

        private static readonly List<WorkProbe> externalProbes = new List<WorkProbe>();

        private static readonly List<string> workSeekingGiverMarkers = new List<string>();

        private static int workVersion = 1;

        internal static int RegisteredWorkSeekingGiverMarkerCount => workSeekingGiverMarkers.Count;

        /// <summary>Bumps when designations / blueprints change so pawn scan cooldowns wake up.</summary>
        public static int WorkVersion => workVersion;

        public static void Notify_WorkChanged(Map map)
        {
            unchecked
            {
                workVersion++;
            }
            if (workVersion <= 0)
            {
                workVersion = 1;
            }
            WorkRelayBoard.Invalidate(map);
            if (map != null)
            {
                WorkRelayBoardBuilder.CancelMap(map.uniqueID);
                WorkRelayAntiLoop.ClearForMap(map);
            }
        }

        /// <summary>
        /// Mods: register a probe so cross-level work relay notices your jobs.
        /// Return true when <paramref name="map"/> has work this pawn could do.
        /// </summary>
        public static void RegisterWorkProbe(WorkProbe probe)
        {
            if (probe == null || externalProbes.Contains(probe))
            {
                return;
            }
            externalProbes.Add(probe);
        }

        public static void UnregisterWorkProbe(WorkProbe probe)
        {
            if (probe != null)
            {
                externalProbes.Remove(probe);
            }
        }

        /// <summary>
        /// Mods with a custom JobGiver (not JobGiver_Work): register a substring
        /// of the giver type's FullName so Patch_ExternalJobGiverRelay treats it
        /// as work-seeking when the local result is empty.
        /// </summary>
        public static void RegisterWorkSeekingJobGiverMarker(string typeNameContains)
        {
            if (string.IsNullOrEmpty(typeNameContains))
            {
                return;
            }
            for (int i = 0; i < workSeekingGiverMarkers.Count; i++)
            {
                if (string.Equals(workSeekingGiverMarkers[i], typeNameContains, StringComparison.Ordinal))
                {
                    return;
                }
            }
            workSeekingGiverMarkers.Add(typeNameContains);
        }

        public static void UnregisterWorkSeekingJobGiverMarker(string typeNameContains)
        {
            workSeekingGiverMarkers.Remove(typeNameContains);
        }

        [StrataSessionReset]
        internal static void ResetSession()
        {
            workVersion = 1;
            WorkRelayBoard.ResetSession();
            WorkRelayBoardBuilder.ResetSession();
        }

        public static bool IsRegisteredWorkSeekingGiver(ThinkNode_JobGiver giver)
        {
            return IsRegisteredWorkSeekingGiverType(giver?.GetType());
        }

        public static bool IsRegisteredWorkSeekingGiverType(Type type)
        {
            if (type == null || workSeekingGiverMarkers.Count == 0)
            {
                return false;
            }
            string name = type.FullName ?? type.Name;
            for (int i = 0; i < workSeekingGiverMarkers.Count; i++)
            {
                if (name.IndexOf(workSeekingGiverMarkers[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasWorkFor(Pawn pawn, Map map)
        {
            try
            {
                return HasWorkForInner(pawn, map);
            }
            catch (Exception e)
            {
                StrataFaultGuard.Report(StrataFaultGuard.System.WorkRelay, e.GetType().Name + ": " + e.Message);
                return false;
            }
        }

        private static bool HasWorkForInner(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
            {
                return false;
            }
            if (WorkRelayAntiLoop.IsBlacklisted(pawn, map))
            {
                return false;
            }

            Pawn_WorkSettings work = pawn.workSettings;
            if (work == null || !work.EverWork)
            {
                return StrataPawnUtility.IsMiscRobot(pawn)
                    && StrataPawnUtility.MiscRobotHasWorkOn(pawn, map);
            }

            WorkRelayBoard.BoardEntry signals = GetSignals(map);
            bool Active(WorkTypeDef type) => type != null && work.WorkIsActive(type);

            if (Active(WorkTypeDefOf.Construction) && signals.construction)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Growing) && signals.growing)
            {
                return true;
            }
            if (Active(StrataDefOf.Mining) && signals.mining)
            {
                return true;
            }
            if (Active(StrataDefOf.PlantCutting) && signals.plantCutting)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Hunting) && signals.hunting)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Hauling) && (signals.hauling || signals.haulExports))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Cleaning) && signals.cleaning)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Firefighter) && signals.firefighting)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Research) && signals.research)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Warden) && signals.warden)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Handling) && signals.handling)
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Doctor) && (PawnRelay.HasPatientsNeedingTend(map)
                || HasMedicalBedsNeedingPatients(map)))
            {
                return true;
            }
            if (signals.flick && (Active(WorkTypeDefOf.Hauling) || Active(WorkTypeDefOf.Construction)))
            {
                return true;
            }
            if (HasBillsFor(pawn, map))
            {
                return true;
            }
            if (signals.childcare && Active(WorkTypeDefOf.Childcare))
            {
                return true;
            }

            for (int i = 0; i < externalProbes.Count; i++)
            {
                try
                {
                    if (externalProbes[i](pawn, map))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Strata] Work probe threw: {e.Message}", e.GetHashCode());
                }
            }

            return false;
        }

        /// <summary>
        /// Soft stampede cap for work commute: more signal on the floor → more
        /// pawns allowed to head there (clamped 3–8).
        /// </summary>
        public static int WorkClaimCap(Map map)
        {
            if (map == null)
            {
                return WorkClaimCapMin;
            }
            WorkRelayBoard.BoardEntry signals = GetSignals(map);
            int score = signals.blueprintFrames;
            if (signals.mining)
            {
                score += 2;
            }
            if (signals.plantCutting)
            {
                score += 1;
            }
            if (signals.hunting)
            {
                score += 1;
            }
            if (signals.haulableCount > 0)
            {
                score += Math.Min(4, (signals.haulableCount + 4) / 5);
            }
            if (signals.haulExports)
            {
                score += 1;
            }
            if (signals.cleaning)
            {
                score += 1;
            }
            if (signals.firefighting)
            {
                score += 2;
            }
            if (signals.flick)
            {
                score += 1;
            }
            if (signals.growing)
            {
                score += 1;
            }
            if (signals.warden || signals.handling || signals.childcare)
            {
                score += 1;
            }
            return Math.Min(WorkClaimCapMax, Math.Max(WorkClaimCapMin, WorkClaimCapMin + score / 3));
        }

        /// <summary>Read published board; sync-build if missing/stale (relay never goes blind).</summary>
        internal static WorkRelayBoard.BoardEntry GetSignals(Map map)
        {
            if (WorkRelayBoard.TryGetFresh(map, BoardFreshTicks, out WorkRelayBoard.BoardEntry entry))
            {
                return entry;
            }
            return WorkRelayBoardBuilder.BuildAndPublishSync(map);
        }

        public static bool HasConstruction(Map map) => map != null && GetSignals(map).construction;

        public static bool HasMining(Map map) => map != null && GetSignals(map).mining;

        public static bool HasPlantCutting(Map map) => map != null && GetSignals(map).plantCutting;

        public static bool HasGrowing(Map map) => map != null && GetSignals(map).growing;

        public static bool HasHunting(Map map) => map != null && GetSignals(map).hunting;

        public static bool HasHauling(Map map) => map != null && GetSignals(map).hauling;

        public static bool HasCrossLevelHaulExports(Map map) =>
            map != null && GetSignals(map).haulExports;

        public static bool HasCleaning(Map map) => map != null && GetSignals(map).cleaning;

        public static bool HasFirefighting(Map map) => map != null && GetSignals(map).firefighting;

        public static bool HasResearch(Map map) => map != null && GetSignals(map).research;

        public static bool HasWardenWork(Map map) => map != null && GetSignals(map).warden;

        public static bool HasHandling(Map map) => map != null && GetSignals(map).handling;

        public static bool HasFlickWork(Map map) => map != null && GetSignals(map).flick;

        public static bool HasChildcare(Map map) => map != null && GetSignals(map).childcare;

        internal static bool ProbeConstruction(Map map)
        {
            if (AnyPlayerThing(map, ThingRequestGroup.Blueprint) || AnyPlayerThing(map, ThingRequestGroup.BuildingFrame))
            {
                return true;
            }
            DesignationManager d = map.designationManager;
            return d.AnySpawnedDesignationOfDef(DesignationDefOf.Deconstruct)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.Uninstall)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.SmoothWall)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.SmoothFloor)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.RemoveFloor);
        }

        internal static bool ProbeMining(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Mine);
        }

        internal static bool ProbePlantCutting(Map map)
        {
            DesignationManager d = map.designationManager;
            return d.AnySpawnedDesignationOfDef(DesignationDefOf.CutPlant)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.HarvestPlant);
        }

        internal static bool ProbeHunting(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Hunt);
        }

        internal static bool ProbeHauling(Map map, out int count)
        {
            count = map.listerHaulables.ThingsPotentiallyNeedingHauling().Count;
            return count > 0;
        }

        internal static bool ProbeCrossLevelHaulExports(Map map)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.haulAcrossLevelsEnabled)
            {
                return false;
            }
            if (!LevelGraph.AnyLinkFrom(map))
            {
                return false;
            }

            HashSet<ThingDef> wanted = LevelDemand.DefsWantedByLinkedLevels(map);
            if (wanted.Count > 0)
            {
                foreach (ThingDef def in wanted)
                {
                    List<Thing> things = map.listerThings.ThingsOfDef(def);
                    if (things != null && things.Count > 0)
                    {
                        return true;
                    }
                }
            }

            // Storage-priority exports: LevelDemand shortfalls can lag or miss
            // when the destination scan caps out. If this floor has things that
            // need hauling and a linked floor has any stockpile, wake work relay
            // so HaulAcrossLevels can decide (including "local full, B1 same
            // priority with room" — vanilla would haul there if it were one map).
            ICollection<Thing> haulables = map.listerHaulables.ThingsPotentiallyNeedingHauling();
            if (haulables != null && haulables.Count > 0)
            {
                StoragePriority maxLocal = MaxStoragePriorityOn(map);
                List<LevelGraph.LevelLink> linksForPriority = LevelGraph.ReachableLevels(map);
                for (int i = 0; i < linksForPriority.Count; i++)
                {
                    StoragePriority linkedMax = MaxStoragePriorityOn(linksForPriority[i].map);
                    if (linkedMax > StoragePriority.Unstored
                        && (linkedMax > maxLocal || maxLocal > StoragePriority.Unstored))
                    {
                        return true;
                    }
                }
            }

            List<Thing> minis = map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
            if (minis == null || minis.Count == 0)
            {
                return false;
            }
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(map);
            for (int i = 0; i < minis.Count; i++)
            {
                if (minis[i] is not MinifiedThing mini)
                {
                    continue;
                }
                for (int j = 0; j < links.Count; j++)
                {
                    if (WorkGiver_InstallAcrossLevels.FindInstallBlueprintFor(links[j].map, mini) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static StoragePriority MaxStoragePriorityOn(Map map)
        {
            StoragePriority max = StoragePriority.Unstored;
            if (map?.haulDestinationManager == null)
            {
                return max;
            }
            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Settings.Priority > max)
                {
                    max = groups[i].Settings.Priority;
                }
            }
            return max;
        }

        internal static bool ProbeCleaning(Map map)
        {
            return map.listerFilthInHomeArea.FilthInHomeArea.Count > 0;
        }

        internal static bool ProbeFirefighting(Map map)
        {
            List<Thing> fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            return fires != null && fires.Count > 0;
        }

        internal static bool ProbeResearch(Map map)
        {
            if (Find.ResearchManager.GetProject() == null)
            {
                return false;
            }
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                if (b is Building_ResearchBench && !b.IsForbidden(Faction.OfPlayer) && !b.IsBurning())
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool ProbeWarden(Map map)
        {
            List<Pawn> prisoners = map.mapPawns.PrisonersOfColonySpawned;
            if (prisoners == null || prisoners.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < prisoners.Count; i++)
            {
                Pawn p = prisoners[i];
                if (p.Dead || p.Destroyed)
                {
                    continue;
                }
                if (p.needs?.food != null && p.needs.food.CurLevelPercentage < 0.45f)
                {
                    return true;
                }
                if (HealthAIUtility.ShouldBeTendedNowByPlayer(p))
                {
                    return true;
                }
                if (p.guest != null && p.guest.Recruitable)
                {
                    return true;
                }

                Building_Bed owned = p.ownership?.OwnedBed;
                if (owned != null && owned.Spawned && owned.ForPrisoners
                    && owned.Map != map && p.CurrentBed() != owned
                    && LevelGraph.AnyLinkFrom(map))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool ProbeHandling(Map map)
        {
            DesignationManager d = map.designationManager;
            if (d.AnySpawnedDesignationOfDef(DesignationDefOf.Tame)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.Slaughter))
            {
                return true;
            }
            List<Pawn> animals = map.mapPawns.SpawnedColonyAnimals;
            if (animals == null)
            {
                return false;
            }
            for (int i = 0; i < animals.Count; i++)
            {
                Pawn animal = animals[i];
                if (animal.training != null && animal.training.NextTrainableToTrain() != null)
                {
                    return true;
                }
                if (animal.needs?.food != null && animal.needs.food.CurLevelPercentage < 0.35f
                    && animal.RaceProps.EatsFood)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool ProbeFlick(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Flick);
        }

        internal static bool ProbeChildcare(Map map)
        {
            List<Pawn> kids = map.mapPawns.FreeColonistsSpawned;
            if (kids == null)
            {
                return false;
            }
            for (int i = 0; i < kids.Count; i++)
            {
                Pawn p = kids[i];
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Child())
                {
                    if (p.needs?.food != null && p.needs.food.CurLevelPercentage < 0.5f)
                    {
                        return true;
                    }
                    if (HealthAIUtility.ShouldBeTendedNowByPlayer(p))
                    {
                        return true;
                    }
                }

                if (ModsConfig.BiotechActive
                    && p.DevelopmentalStage.Baby()
                    && p.CurrentBed() == null
                    && p.ownership?.OwnedBed != null
                    && p.ownership.OwnedBed.Spawned
                    && p.ownership.OwnedBed.Map != map
                    && LevelGraph.AnyLinkFrom(map))
                {
                    return true;
                }
            }
            return false;
        }

        internal static int CountPlayerThingsPublic(Map map, ThingRequestGroup group)
            => CountPlayerThings(map, group);

        private static bool HasMedicalBedsNeedingPatients(Map map)
        {
            return false;
        }

        private static bool HasBillsFor(Pawn pawn, Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is not IBillGiver billGiver || billGiver.BillStack == null
                    || !billGiver.BillStack.AnyShouldDoNow)
                {
                    continue;
                }
                foreach (Bill bill in billGiver.BillStack)
                {
                    if (!bill.ShouldDoNow() || !BillIngredientUtility.PawnCanDoBill(pawn, bill))
                    {
                        continue;
                    }
                    if (BillIngredientUtility.CanStartOnMap(bill, building, map, pawn))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool AnyPlayerThing(Map map, ThingRequestGroup group)
        {
            return CountPlayerThings(map, group) > 0;
        }

        private static int CountPlayerThings(Map map, ThingRequestGroup group)
        {
            List<Thing> things = map.listerThings.ThingsInGroup(group);
            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].Faction == Faction.OfPlayer)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
