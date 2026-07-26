using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    /// <summary>
    /// Cheap "is there plausibly work on this map for this pawn?" probes used by
    /// the work relay. False positives only cost a stair walk + cooldown.
    /// </summary>
    public static class WorkRelaySignals
    {
        public delegate bool WorkProbe(Pawn pawn, Map map);

        private static readonly List<WorkProbe> externalProbes = new List<WorkProbe>();

        private static readonly List<string> workSeekingGiverMarkers = new List<string>();

        internal static int RegisteredWorkSeekingGiverMarkerCount => workSeekingGiverMarkers.Count;

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

        internal static void ResetSession()
        {
            // External registrations are process-lifetime; nothing to clear per save.
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

            Pawn_WorkSettings work = pawn.workSettings;
            if (work == null || !work.EverWork)
            {
                return StrataPawnUtility.IsMiscRobot(pawn)
                    && StrataPawnUtility.MiscRobotHasWorkOn(pawn, map);
            }

            bool Active(WorkTypeDef type) => type != null && work.WorkIsActive(type);

            if (Active(WorkTypeDefOf.Construction) && HasConstruction(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Growing) && HasGrowing(map))
            {
                return true;
            }
            if (Active(StrataDefOf.Mining) && HasMining(map))
            {
                return true;
            }
            if (Active(StrataDefOf.PlantCutting) && HasPlantCutting(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Hunting) && HasHunting(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Hauling) && (HasHauling(map) || HasCrossLevelHaulExports(map)))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Cleaning) && HasCleaning(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Firefighter) && HasFirefighting(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Research) && HasResearch(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Warden) && HasWardenWork(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Handling) && HasHandling(map))
            {
                return true;
            }
            if (Active(WorkTypeDefOf.Doctor) && (PawnRelay.HasPatientsNeedingTend(map)
                || HasMedicalBedsNeedingPatients(map)))
            {
                return true;
            }
            if (HasFlickWork(map) && (Active(WorkTypeDefOf.Hauling) || Active(WorkTypeDefOf.Construction)))
            {
                // Flick is often done by anyone; haul/construct pawns cover it.
                return true;
            }
            if (HasBillsFor(pawn, map))
            {
                return true;
            }
            if (HasChildcare(map) && Active(WorkTypeDefOf.Childcare))
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

        public static bool HasConstruction(Map map)
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

        public static bool HasMining(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Mine);
        }

        public static bool HasPlantCutting(Map map)
        {
            DesignationManager d = map.designationManager;
            return d.AnySpawnedDesignationOfDef(DesignationDefOf.CutPlant)
                || d.AnySpawnedDesignationOfDef(DesignationDefOf.HarvestPlant);
        }

        public static bool HasGrowing(Map map)
        {
            List<Zone> zones = map.zoneManager.AllZones;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is not Zone_Growing grow || grow.cells.Count == 0)
                {
                    continue;
                }
                // Check every cell: sparse sampling (old step = count / 24) missed
                // harvestable plants or empty sowable tiles that fell between
                // sample indices. The relay is cooldown-throttled (7500 ticks
                // per pawn) so the full scan runs rarely and the overhead is fine.
                for (int c = 0; c < grow.cells.Count; c++)
                {
                    IntVec3 cell = grow.cells[c];
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    Plant plant = cell.GetPlant(map);
                    if (plant == null)
                    {
                        if (cell.GetEdifice(map) == null && grow.GetPlantDefToGrow() != null)
                        {
                            return true;
                        }
                    }
                    else if (plant.HarvestableNow)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HasHunting(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Hunt);
        }

        public static bool HasHauling(Map map)
        {
            return map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0;
        }

        // Items on this map that a linked floor is pulling (refuel, higher-priority
        // storage, construction/bills). Lets work-relay send haulers to the source
        // floor even when local listerHaulables is empty.
        public static bool HasCrossLevelHaulExports(Map map)
        {
            if (map == null)
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

            // Minified buildings whose Blueprint_Install sits on a linked floor.
            List<Thing> minis = map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
            if (minis == null || minis.Count == 0 || !LevelGraph.AnyLinkFrom(map))
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

        public static bool HasCleaning(Map map)
        {
            return map.listerFilthInHomeArea.FilthInHomeArea.Count > 0;
        }

        public static bool HasFirefighting(Map map)
        {
            List<Thing> fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            return fires != null && fires.Count > 0;
        }

        public static bool HasResearch(Map map)
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

        public static bool HasWardenWork(Map map)
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
                // Food delivery, tending in prison, or recruit/chat potential.
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

                // Assigned bed on another linked floor — needs a warden commute.
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

        public static bool HasHandling(Map map)
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
                    // Animal feeding is often Handling/Hauling adjacent; count it.
                    return true;
                }
            }
            return false;
        }

        public static bool HasFlickWork(Map map)
        {
            return map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Flick);
        }

        public static bool HasChildcare(Map map)
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

                // Infant on this floor with an assigned crib on a linked floor.
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

        private static bool HasMedicalBedsNeedingPatients(Map map)
        {
            // Doctor work on a hospital floor that has empty medical beds is weak;
            // prefer patients. Kept as a soft signal only via HasPatientsNeedingTend.
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
            List<Thing> things = map.listerThings.ThingsInGroup(group);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].Faction == Faction.OfPlayer)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
