using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    public static class StrataPortalUtility
    {
        // Escape hatch for Patch_PortalDeSpawnImmunity: intentional portal
        // despawn/respawn cycles (moves) raise this around the whole cycle.
        private static int portalMoveDepth;

        public static bool PortalMoveInProgress => portalMoveDepth > 0;

        public static void BeginPortalMove() => portalMoveDepth++;

        public static void EndPortalMove()
        {
            if (portalMoveDepth > 0)
            {
                portalMoveDepth--;
            }
        }

        // Shafts, stairs, elevators, and dig extensions — never valid for
        // infestation hives, roof collapse, or event damage.
        public static bool IsProtectedPortal(Thing thing)
        {
            if (thing == null || !thing.Spawned)
            {
                return false;
            }
            if (thing is MapPortal)
            {
                string name = thing.def?.defName;
                return !name.NullOrEmpty() && name.StartsWith("Strata_");
            }
            return false;
        }

        public static bool CellBlockedByProtectedPortal(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (IsProtectedPortal(things[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool RectBlockedByProtectedPortal(Map map, IntVec3 center, Rot4 rot, IntVec2 size)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, size))
            {
                if (CellBlockedByProtectedPortal(map, cell))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ShouldBlockPortalDestroy(Thing thing, DestroyMode mode)
        {
            if (!IsProtectedPortal(thing))
            {
                return false;
            }
            // Vanish / WillReplace: map-gen and pack/unpack moves.
            // Deconstruct: player tore down an empty stairwell/elevator after
            // DeconstructibleBy allowed it — must not be swallowed here or the
            // designation finishes while the shaft stays forever.
            return mode != DestroyMode.Vanish
                && mode != DestroyMode.WillReplace
                && mode != DestroyMode.Deconstruct;
        }

        // Colony pawns (incl. downed), prisoners, mechs, and player animals on a
        // linked level — broader than vanilla AnyPawnBlockingMapRemoval, which
        // ignores downed colonists and non-colonist colony pawns.
        public static bool LinkedLevelHasColonyPresence(Map level)
        {
            if (level?.mapPawns == null)
            {
                return false;
            }
            if (level.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return true;
            }
            Faction player = Faction.OfPlayer;
            IReadOnlyList<Pawn> pawns = level.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }
                if (pawn.Faction == player || pawn.HostFaction == player)
                {
                    return true;
                }
            }
            return false;
        }

        // Stairs/elevators are def.destroyable=false; vanilla Thing.Destroy then
        // Log.Errors and leaves the duplicate spawned (ghost shaft + debug log pop).
        public static void ForceDestroyPortal(Thing portal, DestroyMode mode = DestroyMode.Vanish)
        {
            if (portal == null || portal.Destroyed)
            {
                return;
            }
            BeginPortalMove();
            bool prev = Thing.allowDestroyNonDestroyable;
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                portal.Destroy(mode);
            }
            finally
            {
                Thing.allowDestroyNonDestroyable = prev;
                EndPortalMove();
            }
        }

        // Prefer this over Thing.Destroy when the thing may be a Strata shaft/landing
        // (or any destroyable=false building) — plain Destroy leaves ghosts and
        // double-registers CompPower transmitters after gravship land.
        public static void SafeDestroyThing(Thing thing, DestroyMode mode = DestroyMode.Vanish)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }
            if (thing.def != null && !thing.def.destroyable)
            {
                ForceDestroyPortal(thing, mode);
                return;
            }
            thing.Destroy(mode);
        }

        // WipeMode cannot remove destroyable=false portals; clear them first so
        // GenSpawn / PlaceGravship do not stack two transmitters on one cell.
        public static void PrefireWipeStrataPortals(
            Map map,
            IntVec3 loc,
            Rot4 rot,
            IntVec2 size,
            Thing except = null)
        {
            if (map == null)
            {
                return;
            }
            CellRect rect = GenAdj.OccupiedRect(loc, rot, size);
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> at = cell.GetThingList(map);
                for (int i = at.Count - 1; i >= 0; i--)
                {
                    Thing blocker = at[i];
                    if (blocker == null || blocker == except || blocker.Destroyed)
                    {
                        continue;
                    }
                    if (blocker is MapPortal
                        && blocker.def?.defName != null
                        && blocker.def.defName.StartsWith("Strata_"))
                    {
                        ForceDestroyPortal(blocker);
                    }
                }
            }
        }

        public static void ClearBuildingsAndItemsInRect(
            Map map,
            CellRect rect,
            Thing except = null)
        {
            if (map == null)
            {
                return;
            }
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> at = cell.GetThingList(map);
                for (int i = at.Count - 1; i >= 0; i--)
                {
                    Thing blocker = at[i];
                    if (blocker == null || blocker == except || blocker.Destroyed)
                    {
                        continue;
                    }
                    if (blocker.def.category == ThingCategory.Building
                        || blocker.def.category == ThingCategory.Item)
                    {
                        SafeDestroyThing(blocker);
                    }
                }
            }
        }

        // Entrance has a pocket map but the landing is missing — restore it.
        public static void RepairMissingLandings()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                var entrances = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal));
                for (int j = 0; j < entrances.Count; j++)
                {
                    if (entrances[j] is not Building_StairsDown entrance || !entrance.Spawned || !entrance.PocketMapExists)
                    {
                        continue;
                    }
                    Map level = entrance.PocketMap;
                    if (level == null)
                    {
                        continue;
                    }
                    PocketMapExit exit = entrance.exit;
                    if (exit != null && !exit.Destroyed && exit.Spawned)
                    {
                        continue;
                    }
                    ThingDef exitDef = entrance.def.portal?.exitDef;
                    if (exitDef == null)
                    {
                        continue;
                    }
                    IntVec3 spot = entrance.FindLandingCell(level);
                    if (!spot.IsValid)
                    {
                        spot = StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, level);
                    }
                    if (!spot.IsValid)
                    {
                        continue;
                    }
                    PocketMapUtility.currentlyGeneratingPortal = entrance;
                    try
                    {
                        StrataPortalUtility.SpawnLanding(exitDef, spot, level);
                    }
                    finally
                    {
                        PocketMapUtility.currentlyGeneratingPortal = null;
                    }
                    Log.Message("[Strata] Restored missing portal landing under " + entrance.LabelCap + ".");
                }

                // Elevator pairs use Building_ElevatorDown as entrance.
                for (int j = 0; j < entrances.Count; j++)
                {
                    if (entrances[j] is not Building_ElevatorDown elevator || !elevator.Spawned || !elevator.PocketMapExists)
                    {
                        continue;
                    }
                    PocketMapExit exit = elevator.exit;
                    if (exit != null && !exit.Destroyed && exit.Spawned)
                    {
                        continue;
                    }
                    ThingDef exitDef = elevator.def.portal?.exitDef;
                    if (exitDef == null)
                    {
                        continue;
                    }
                    Map level = elevator.PocketMap;
                    IntVec3 spot = elevator.FindLandingCell(level);
                    if (!spot.IsValid)
                    {
                        spot = StrataMapUtility.VerticalAlign(elevator.Position, elevator.Map, level);
                    }
                    if (!spot.IsValid)
                    {
                        continue;
                    }
                    PocketMapUtility.currentlyGeneratingPortal = elevator;
                    try
                    {
                        SpawnLanding(exitDef, spot, level);
                    }
                    finally
                    {
                        PocketMapUtility.currentlyGeneratingPortal = null;
                    }
                    Log.Message("[Strata] Restored missing elevator landing under " + elevator.LabelCap + ".");
                }
            }
        }

        // Haul designations live in each map's DesignationManager, so a thing
        // that needs one to be haulable (stone chunks, mostly) arrives on
        // another level undesignated and haulers there ignore it. Runs from
        // OnEntered - after the pawn spawns on the destination map, before
        // vanilla drops its cargo there. Things picked straight out of storage
        // never had a designation, so this adds one rather than only moving an
        // existing one.
        public static void TransferHaulDesignation(MapPortal portal, Pawn pawn)
        {
            Thing carried = pawn?.carryTracker?.CarriedThing;
            if (carried == null || !carried.def.designateHaulable || carried.def.alwaysHaulable)
            {
                return;
            }
            Map source = portal?.Map;
            Map dest = pawn.Map;
            source?.designationManager.TryRemoveDesignationOn(carried, DesignationDefOf.Haul);
            if (dest != null && dest != source
                && dest.designationManager.DesignationOn(carried, DesignationDefOf.Haul) == null)
            {
                dest.designationManager.AddDesignation(new Designation(carried, DesignationDefOf.Haul));
            }
        }

        // Vanilla EnterPortal clears the job queue AFTER OnEntered, so any
        // EnqueueFirst here is wiped. Defer storage/construction finish to the
        // next tick (same pattern as PortalRelayChain / DraftedPortalPathing).
        private static readonly List<int> pendingHaulDeliver = new List<int>();

        internal static void ResetHaulDeliverSession()
        {
            pendingHaulDeliver.Clear();
        }

        internal static bool HasPendingHaulDeliveries => pendingHaulDeliver.Count > 0;

        public static void NotifyHaulArrival(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (!pendingHaulDeliver.Contains(pawn.thingIDNumber))
            {
                pendingHaulDeliver.Add(pawn.thingIDNumber);
            }
        }

        internal static void TickHaulDeliveries()
        {
            if (pendingHaulDeliver.Count == 0)
            {
                return;
            }

            for (int i = pendingHaulDeliver.Count - 1; i >= 0; i--)
            {
                int id = pendingHaulDeliver[i];
                pendingHaulDeliver.RemoveAt(i);
                // Haul chain FinishHaul already delivered on dest; skip mid-hop.
                if (PortalRelayChain.HasIntent(id, RelayPurpose.Haul))
                {
                    continue;
                }

                Pawn pawn = FindPawnById(id);
                if (pawn != null)
                {
                    TryStartHaulDelivery(pawn);
                }
            }
        }

        // Stockpile first, then blueprint/frame. Returns true if a job started.
        // Prefer carried cargo — vanilla drop at crowded landings often fails and
        // left pawns wandering while still holding meals/steel.
        public static bool TryStartHaulDelivery(Pawn pawn, int preferConstructibleId = 0)
        {
            if (pawn?.jobs == null || pawn.Map == null || !pawn.Spawned
                || pawn.Downed || pawn.Dead || pawn.Drafted)
            {
                return false;
            }

            Thing cargo = pawn.carryTracker?.CarriedThing;
            if (cargo == null)
            {
                cargo = FindNearbyHaulCargo(pawn);
            }

            if (cargo == null)
            {
                return false;
            }

            // Force-build / cross-level deliver: hit the remembered blueprint/frame
            // before any stockpile steal.
            if (preferConstructibleId > 0
                && TryStartJobIfReservable(pawn, TryMakeConstructionJobFor(pawn, cargo, preferConstructibleId)))
            {
                return true;
            }

            // Probe reservations before StartJob — several haulers can land on the
            // same tick and all pick one frame (HaulToContainer reserves maxPawns=1).
            // Starting without a probe spam-logs and EndCurrentJob(Errored).
            // Billgivers first (cellar food → surface stove), then refuel
            // (uranium → reactors), then cross-level reinstall blueprints,
            // then frames (prefer construction over general stockpile), then storage.
            if (TryStartJobIfReservable(pawn, TryMakeBillJob(pawn, cargo)))
            {
                return true;
            }

            if (TryStartJobIfReservable(pawn, TryMakeRefuelJob(pawn, cargo)))
            {
                return true;
            }

            if (TryStartJobIfReservable(pawn, TryMakeInstallJob(pawn, cargo)))
            {
                return true;
            }

            if (TryStartJobIfReservable(pawn, TryMakeConstructionJob(pawn, cargo)))
            {
                return true;
            }

            return TryStartJobIfReservable(pawn, TryMakeStorageJob(pawn, cargo));
        }

        private static Job TryMakeInstallJob(Pawn pawn, Thing cargo)
        {
            if (cargo is not MinifiedThing mini)
            {
                return null;
            }

            Blueprint_Install bp = WorkGiver_InstallAcrossLevels.FindInstallBlueprintFor(pawn.Map, mini);
            if (bp == null)
            {
                return null;
            }

            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }

            if (!pawn.CanReserveAndReach(bp, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, cargo, bp);
            job.count = 1;
            job.haulMode = HaulMode.ToContainer;
            return job;
        }

        // CompRefuelable is not a HaulToContainer destination — use the vanilla
        // Refuel / RefuelAtomic jobs (targetA = building, targetB = fuel).
        private static Job TryMakeRefuelJob(Pawn pawn, Thing cargo)
        {
            ThingWithComps target = FindRefuelableNeeding(pawn, cargo);
            if (target == null)
            {
                return null;
            }

            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }

            CompRefuelable refuel = target.GetComp<CompRefuelable>();
            if (refuel?.Props == null)
            {
                return null;
            }

            if (refuel.Props.atomicFueling)
            {
                Job atomic = JobMaker.MakeJob(JobDefOf.RefuelAtomic, target);
                atomic.targetQueueB = new List<LocalTargetInfo> { cargo };
                atomic.count = cargo.stackCount;
                return atomic;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Refuel, target, cargo);
            job.count = System.Math.Min(cargo.stackCount, refuel.GetFuelCountToFullyRefuel());
            return job;
        }

        private static ThingWithComps FindRefuelableNeeding(Pawn pawn, Thing cargo)
        {
            Map map = pawn.Map;
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            ThingWithComps best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] is not ThingWithComps twc)
                {
                    continue;
                }

                CompRefuelable refuel = twc.GetComp<CompRefuelable>();
                if (refuel == null || !refuel.ShouldAutoRefuelNow)
                {
                    continue;
                }

                if (refuel.Props?.fuelFilter == null || !refuel.Props.fuelFilter.Allows(cargo))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(twc, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                float dist = twc.Position.DistanceToSquared(pawn.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = twc;
                }
            }

            return best;
        }

        private static Job TryMakeBillJob(Pawn pawn, Thing cargo)
        {
            Thing billGiver = BillIngredientUtility.FindBillGiverNeeding(pawn, cargo.def);
            if (billGiver == null)
            {
                return null;
            }
            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, cargo, billGiver);
            job.count = cargo.stackCount;
            job.haulMode = HaulMode.ToContainer;
            return job;
        }

        private static bool TryStartJobIfReservable(Pawn pawn, Job job)
        {
            if (job == null)
            {
                return false;
            }

            if (!job.TryMakePreToilReservations(pawn, errorOnFailed: false))
            {
                pawn.ClearReservationsForJob(job);
                return false;
            }

            // Keep the probe reservations — clearing them races other haulers onto
            // the same cell and StartJob then Warning-spams (opens the debug log).
            // Same-pawn re-reserve in the fresh driver is allowed.
            // Keep carried cargo: InterruptForced otherwise drops wood/steel mid
            // force-build / cross-level haul finish.
            pawn.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                keepCarryingThingOverride: true);
            return true;
        }

        private static Job TryMakeStorageJob(Pawn pawn, Thing cargo)
        {
            if (!StoreUtility.TryFindBestBetterStoreCellFor(
                    cargo,
                    pawn,
                    pawn.Map,
                    StoragePriority.Unstored,
                    pawn.Faction,
                    out IntVec3 cell))
            {
                return null;
            }

            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }

            return HaulAIUtility.HaulToCellStorageJob(pawn, cargo, cell, fitInStoreCell: true);
        }

        private static Job TryMakeConstructionJob(Pawn pawn, Thing cargo)
        {
            Thing site = FindConstructibleNeeding(pawn, cargo.def);
            if (site == null)
            {
                return null;
            }

            return MakeHaulToConstructible(pawn, cargo, site);
        }

        private static Job TryMakeConstructionJobFor(Pawn pawn, Thing cargo, int constructibleId)
        {
            Thing site = FindThingByIdOnMap(pawn.Map, constructibleId);
            if (site == null || site is not IConstructible constructible)
            {
                return null;
            }
            if (LevelDemand.IsInstallBlueprint(site))
            {
                return null;
            }
            if (constructible.ThingCountNeeded(cargo.def) <= 0)
            {
                return null;
            }
            if (!pawn.CanReserveAndReach(site, PathEndMode.Touch, Danger.Deadly, 1, 1))
            {
                return null;
            }
            return MakeHaulToConstructible(pawn, cargo, site);
        }

        private static Job MakeHaulToConstructible(Pawn pawn, Thing cargo, Thing site)
        {
            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, cargo, site);
            job.count = cargo.stackCount;
            job.haulMode = HaulMode.ToContainer;
            return job;
        }

        private static Thing FindThingByIdOnMap(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].thingIDNumber == thingId)
                {
                    return things[i];
                }
            }
            return null;
        }

        public static Thing FindThingByIdAcrossMaps(int thingId)
        {
            if (thingId <= 0)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Thing t = FindThingByIdOnMap(maps[i], thingId);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        private static Thing FindNearbyHaulCargo(Pawn pawn)
        {
            Map map = pawn.Map;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pawn.Position, map, 2.9f, true))
            {
                if (thing.def.category != ThingCategory.Item || thing.IsForbidden(pawn))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                if (StoreUtility.TryFindBestBetterStoreCellFor(
                        thing,
                        pawn,
                        map,
                        StoragePriority.Unstored,
                        pawn.Faction,
                        out _)
                    || FindConstructibleNeeding(pawn, thing.def) != null
                    || FindRefuelableNeeding(pawn, thing) != null)
                {
                    return thing;
                }
            }

            return null;
        }

        private static Pawn FindPawnById(int thingId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> pawns = maps[i].mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    if (pawns[j].thingIDNumber == thingId)
                    {
                        return pawns[j];
                    }
                }
            }

            return null;
        }

        private static Thing FindConstructibleNeeding(Pawn pawn, ThingDef material)
        {
            Map map = pawn.Map;
            Thing best = null;
            float bestDist = float.MaxValue;
            AppendNearestConstructible(pawn, map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint), material, ref best, ref bestDist);
            AppendNearestConstructible(pawn, map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame), material, ref best, ref bestDist);
            return best;
        }

        private static void AppendNearestConstructible(
            Pawn pawn,
            List<Thing> things,
            ThingDef material,
            ref Thing best,
            ref float bestDist)
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Faction != Faction.OfPlayer
                    || LevelDemand.IsInstallBlueprint(thing)
                    || thing is not IConstructible constructible)
                {
                    continue;
                }

                if (constructible.ThingCountNeeded(material) <= 0)
                {
                    continue;
                }

                // Match JobDriver_HaulToContainer (non-enroute): maxPawns=1, stackCount=1.
                if (!pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly, 1, 1))
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(thing.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = thing;
                }
            }
        }

        // Carves a small chamber out of the rock and spawns a portal's bottom
        // landing there. Must run while PocketMapUtility.currentlyGeneratingPortal
        // points at the entrance (during map generation or GeneratePocketMapInt):
        // PocketMapExit.SpawnSetup uses it to wire entrance and exit together.
        public static PocketMapExit SpawnLanding(ThingDef exitDef, IntVec3 cell, Map level, Rot4? rot = null)
        {
            ArrivalZoneUtility.PrepareLandingCell(level, cell);
            Rot4 spawnRot = rot ?? PocketMapUtility.currentlyGeneratingPortal?.Rotation ?? Rot4.North;
            return (PocketMapExit)GenSpawn.Spawn(ThingMaker.MakeThing(exitDef), cell, level, spawnRot);
        }

        public static bool IsSealedPortal(Thing thing)
        {
            if (thing is Building_StairsDown stairsDown)
            {
                return stairsDown.Sealed;
            }
            if (thing is Building_ElevatorDown elevatorDown)
            {
                return elevatorDown.Sealed;
            }
            if (thing is Building_StairsUp && thing is PocketMapExit exit && exit.entrance is Building_StairsDown entrance)
            {
                return entrance.Sealed;
            }
            if (thing is Building_ElevatorUp elevatorUp && elevatorUp.entrance is Building_ElevatorDown elevEntrance)
            {
                return elevEntrance.Sealed;
            }
            if (thing is Building_ElevatorBuildUpLanding towerLanding
                && towerLanding.entrance is Building_StairsBuildUp towerEntrance)
            {
                return towerEntrance.Sealed;
            }
            return false;
        }

        public static bool CellBlockedBySealedPortal(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.def.building == null)
                {
                    continue;
                }
                if (thing.def.defName.StartsWith("Strata_Stairs")
                    || thing.def.defName.StartsWith("Strata_Elevator")
                    || thing.def.defName.StartsWith("Strata_Gravship"))
                {
                    if (IsSealedPortal(thing))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool GasBlockedBetween(Map map, IntVec3 from, IntVec3 to)
        {
            return CellBlockedBySealedPortal(map, from) || CellBlockedBySealedPortal(map, to);
        }
    }
}
