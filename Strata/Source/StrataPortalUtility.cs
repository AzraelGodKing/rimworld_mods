using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Strata
{
    public static class StrataPortalUtility
    {
        private static readonly AccessTools.FieldRef<MapPortal, Map> PocketMapRef =
            AccessTools.FieldRefAccess<MapPortal, Map>("pocketMap");

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

        // Patch_PortalDeSpawnImmunity: colony shafts stay glued down, but
        // Odyssey gravship pack calls Thing.DeSpawn(WillReplace) without
        // BeginPortalMove(). Swallowing that leaves stairs on the launch map
        // (or packed-and-still-spawned), then land cannot restore them.
        public static bool ShouldAllowPortalDeSpawn(Thing thing, DestroyMode mode)
        {
            if (!IsProtectedPortal(thing))
            {
                return true;
            }
            if (PocketMapUtility.currentlyGeneratingPortal != null)
            {
                return true;
            }
            if (PortalMoveInProgress)
            {
                return true;
            }
            if (!StrataGravshipUtility.IsGravshipHostShaft(thing)
                || thing.def == null
                || !thing.def.bringAlongOnGravship)
            {
                return false;
            }
            // Vanilla pack, or any host-shaft despawn while takeoff/land is running.
            return mode == DestroyMode.WillReplace
                || StrataGravshipPortalTravel.TravelInProgress;
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
                    StrataLog.Verbose("[Strata] Restored missing portal landing under " + entrance.LabelCap + ".");
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
                    StrataLog.Verbose("[Strata] Restored missing elevator landing under " + elevator.LabelCap + ".");
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

        [StrataSessionReset]
        internal static void ResetHaulDeliverSession()
        {
            pendingHaulDeliver.Clear();
        }

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
                    if (!TryStartHaulDelivery(pawn)
                        && pawn.carryTracker?.CarriedThing != null
                        && pawn.carryTracker.CarriedThing is not Pawn)
                    {
                        TryForceDropCarriedCargo(pawn);
                    }
                }
            }

            // Stuck carriers: Wait spam with cargo after a failed HaulToContainer.
            TickStuckHaulCarriers();
        }

        private static void TickStuckHaulCarriers()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map == null || !LevelGraph.AnyLinkFrom(map))
                {
                    continue;
                }

                List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
                if (pawns == null)
                {
                    continue;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn?.carryTracker?.CarriedThing == null
                        || pawn.carryTracker.CarriedThing is Pawn
                        || pawn.Downed
                        || pawn.Dead
                        || pawn.Drafted)
                    {
                        continue;
                    }

                    // Only recover idle / Wait — do not interrupt an active haul job.
                    JobDef jobDef = pawn.CurJobDef;
                    if (jobDef != null
                        && jobDef != JobDefOf.Wait_MaintainPosture
                        && jobDef != JobDefOf.Wait_Combat
                        && jobDef != JobDefOf.Wait)
                    {
                        continue;
                    }

                    if (PortalRelayChain.HasIntent(pawn))
                    {
                        continue;
                    }

                    if (!TryStartHaulDelivery(pawn)
                        && pawn.carryTracker.CarriedThing != null
                        && !TryStartStorageDelivery(pawn)
                        && pawn.carryTracker.CarriedThing != null)
                    {
                        TryForceDropCarriedCargo(pawn);
                    }
                }
            }
        }

        // Stockpile first, then blueprint/frame. Returns true if a job started.
        // Prefer carried cargo — vanilla drop at crowded landings often fails and
        // left pawns wandering while still holding meals/steel.
        // preferStoreCell: stockpile cell chosen before the stair trip (same as
        // vanilla HaulToCell once the hauler is on the destination floor).
        public static bool TryStartHaulDelivery(
            Pawn pawn,
            int preferConstructibleId = 0,
            IntVec3 preferStoreCell = default)
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
            // before any stockpile steal. If that fails, reassign to storage so the
            // stack is not stuck in their arms.
            if (preferConstructibleId > 0)
            {
                if (TryStartJobIfReservable(pawn, TryMakeConstructionJobFor(pawn, cargo, preferConstructibleId)))
                {
                    return true;
                }
                if (TryStartStorageDelivery(pawn, cargo, preferStoreCell))
                {
                    return true;
                }
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

            return TryStartStorageDelivery(pawn, cargo, preferStoreCell);
        }

        /// <summary>
        /// Reassign carried cargo into a stockpile/shelf on the current map.
        /// Used when construction delivery fails after a stair trip.
        /// </summary>
        public static bool TryStartStorageDelivery(
            Pawn pawn,
            Thing cargo = null,
            IntVec3 preferStoreCell = default)
        {
            if (pawn?.jobs == null || pawn.Map == null || !pawn.Spawned
                || pawn.Downed || pawn.Dead || pawn.Drafted)
            {
                return false;
            }

            cargo ??= pawn.carryTracker?.CarriedThing;
            if (cargo == null || cargo is Pawn)
            {
                return false;
            }

            Job job = TryMakeStorageJob(pawn, cargo, preferStoreCell);
            if (job == null)
            {
                if (cargo.def != null)
                {
                    StrataStorageSoftCompat.NoteFailedDest(pawn.Map, cargo.def);
                }
                return false;
            }

            if (TryStartJobIfReservable(pawn, job))
            {
                return true;
            }

            if (cargo.def != null)
            {
                StrataStorageSoftCompat.NoteFailedDest(pawn.Map, cargo.def);
            }
            return false;
        }

        /// <summary>
        /// Place carried non-pawn cargo near the pawn. Used when post-stair delivery
        /// finds no frame/stockpile so haulers are not stuck forever.
        /// </summary>
        public static bool TryForceDropCarriedCargo(Pawn pawn)
        {
            if (pawn?.carryTracker == null || pawn.Map == null || !pawn.Spawned)
            {
                return false;
            }

            Thing cargo = pawn.carryTracker.CarriedThing;
            if (cargo == null || cargo is Pawn)
            {
                return false;
            }

            if (pawn.carryTracker.TryDropCarriedThing(
                    pawn.Position,
                    ThingPlaceMode.Near,
                    out Thing _))
            {
                return true;
            }

            // TryDrop can fail if something still blocks it — tear out of the tracker.
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 3);
            if (!cell.IsValid)
            {
                cell = pawn.Position;
            }

            if (pawn.carryTracker.innerContainer.TryDrop(
                    cargo,
                    cell,
                    pawn.Map,
                    ThingPlaceMode.Near,
                    out _,
                    null,
                    null))
            {
                return true;
            }

            // Last resort so corpses never vanish from a stuck haul tracker.
            if (pawn.carryTracker.innerContainer.Contains(cargo))
            {
                pawn.carryTracker.innerContainer.Remove(cargo);
            }
            if (!cargo.Spawned)
            {
                return GenPlace.TryPlaceThing(cargo, cell, pawn.Map, ThingPlaceMode.Near);
            }
            return true;
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

        private static Job TryMakeStorageJob(Pawn pawn, Thing cargo, IntVec3 preferStoreCell = default)
        {
            // Prefer TryFindBestBetterStorageFor + HaulToContainer for buildings
            // (Adaptive Storage / Neat shelves); cell haul for stockpile zones.
            // preferStoreCell: cell chosen before the stair trip (vanilla HaulToCell).
            return StrataStorageSoftCompat.TryMakeStorageJob(pawn, cargo, preferStoreCell);
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

        public static bool IsStrataPortalShaft(Thing thing)
        {
            return thing is MapPortal portal
                && thing is not PocketMapExit
                && CompStrataShaftLinkInjector.IsStrataPortalDef(thing.def)
                && portal.Spawned;
        }

        public static bool IsStrataPortalLanding(Thing thing)
        {
            return thing is PocketMapExit
                && CompStrataShaftLinkInjector.IsStrataPortalDef(thing.def)
                && thing.Spawned;
        }

        public static bool IsProperlyLinked(MapPortal shaft, MapPortal landing)
        {
            if (shaft == null || landing is not PocketMapExit exit)
            {
                return false;
            }
            if (exit.entrance != shaft || shaft.exit != exit)
            {
                return false;
            }
            Map pocket = PocketMapRef(shaft);
            return pocket != null && pocket == landing.Map;
        }

        // Single wiring path for shaft↔landing. Clears stale links, sets
        // entrance/exit/pocketMap, syncs pair IDs, invalidates caches.
        public static bool ConnectPortalPair(MapPortal hostShaft, MapPortal landing, bool log = true)
        {
            if (hostShaft == null || hostShaft is PocketMapExit
                || landing is not PocketMapExit exitLanding)
            {
                return false;
            }
            if (!StrataGravshipUtility.SameShaftFamily(hostShaft, landing))
            {
                if (log)
                {
                    Log.Warning("[Strata] Refused portal pair: colony/gravship family mismatch ("
                        + hostShaft.LabelCap + " <-> " + landing.LabelCap + ").");
                }
                return false;
            }

            if (IsProperlyLinked(hostShaft, exitLanding))
            {
                CompStrataShaftLink.SyncPairIds(hostShaft, exitLanding);
                return true;
            }

            // Drop stale one-way links on the host's previous exit.
            if (hostShaft.exit != null && hostShaft.exit != exitLanding
                && hostShaft.exit.entrance == hostShaft)
            {
                hostShaft.exit.entrance = null;
            }

            // Drop stale entrance on the landing if it pointed elsewhere.
            if (exitLanding.entrance != null && exitLanding.entrance != hostShaft
                && exitLanding.entrance.exit == exitLanding)
            {
                exitLanding.entrance.exit = null;
            }

            exitLanding.entrance = hostShaft;
            hostShaft.exit = exitLanding;

            if (landing.Map != null)
            {
                PocketMapRef(hostShaft) = landing.Map;
                StrataGravshipShaftIdentity.CompOf(hostShaft)?.RememberPocket(landing.Map);
            }

            if (landing.Map?.Parent is PocketMapParent parent && hostShaft.Map != null)
            {
                parent.sourceMap = hostShaft.Map;
            }

            CompStrataShaftLink.SyncPairIds(hostShaft, exitLanding);
            LevelGraph.InvalidateCache();
            StrataGravshipCache.Invalidate();

            if (log)
            {
                StrataLog.Verbose("[Strata] Wired "
                    + landing.LabelCap + " <-> " + hostShaft.LabelCap
                    + " (pair " + (CompStrataShaftLink.CompOf(hostShaft)?.ShortId ?? "?") + ")"
                    + " on " + hostShaft.Map);
            }
            return true;
        }

        // Stamp pair IDs onto every healthy shaft↔landing link.
        public static int StampLinkedPortalPairIds()
        {
            int stamped = 0;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
                for (int j = 0; j < portals.Count; j++)
                {
                    if (!IsStrataPortalShaft(portals[j]))
                    {
                        continue;
                    }
                    MapPortal shaft = (MapPortal)portals[j];
                    PocketMapExit exit = shaft.exit;
                    if (exit == null || !exit.Spawned || !IsProperlyLinked(shaft, exit))
                    {
                        continue;
                    }
                    CompStrataShaftLink.SyncPairIds(shaft, exit);
                    stamped++;
                }
            }
            return stamped;
        }

        // Auto-rewire when exactly one shaft and one landing share a pairGuid
        // but are not properly linked. Call after RepairMissingLandings.
        public static int RelinkPortalsByPairId()
        {
            StampLinkedPortalPairIds();

            var shaftsByGuid = new Dictionary<string, List<MapPortal>>();
            var landingsByGuid = new Dictionary<string, List<MapPortal>>();

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
                for (int j = 0; j < portals.Count; j++)
                {
                    Thing thing = portals[j];
                    CompStrataShaftLink link = CompStrataShaftLink.CompOf(thing);
                    if (link == null)
                    {
                        continue;
                    }
                    link.EnsurePairGuid();
                    string guid = link.pairGuid;
                    if (guid.NullOrEmpty())
                    {
                        continue;
                    }

                    if (IsStrataPortalShaft(thing))
                    {
                        AddToGuidList(shaftsByGuid, guid, (MapPortal)thing);
                    }
                    else if (IsStrataPortalLanding(thing))
                    {
                        AddToGuidList(landingsByGuid, guid, (MapPortal)thing);
                    }
                }
            }

            int relinked = 0;
            foreach (KeyValuePair<string, List<MapPortal>> kv in shaftsByGuid)
            {
                if (kv.Value.Count != 1)
                {
                    continue;
                }
                if (!landingsByGuid.TryGetValue(kv.Key, out List<MapPortal> landings)
                    || landings.Count != 1)
                {
                    continue;
                }

                MapPortal shaft = kv.Value[0];
                MapPortal landing = landings[0];
                if (IsProperlyLinked(shaft, landing))
                {
                    continue;
                }
                if (!StrataGravshipUtility.SameShaftFamily(shaft, landing))
                {
                    continue;
                }
                if (ConnectPortalPair(shaft, landing))
                {
                    relinked++;
                }
            }
            return relinked;
        }

        private static void AddToGuidList(
            Dictionary<string, List<MapPortal>> dict, string guid, MapPortal portal)
        {
            if (!dict.TryGetValue(guid, out List<MapPortal> list))
            {
                list = new List<MapPortal>();
                dict[guid] = list;
            }
            list.Add(portal);
        }

        public static string DebugPortalTopology()
        {
            var sb = new System.Text.StringBuilder("[Strata] Portal topology:\n");
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                sb.AppendLine("  Map " + map + ":");
                List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
                for (int j = 0; j < portals.Count; j++)
                {
                    Thing thing = portals[j];
                    if (!CompStrataShaftLinkInjector.IsStrataPortalDef(thing.def))
                    {
                        continue;
                    }
                    CompStrataShaftLink link = CompStrataShaftLink.CompOf(thing);
                    string pair = link != null ? link.ShortId : "-";
                    if (thing is MapPortal shaft && thing is not PocketMapExit)
                    {
                        PocketMapExit exit = shaft.exit;
                        Map pocket = PocketMapRef(shaft);
                        bool ok = exit != null && IsProperlyLinked(shaft, exit);
                        sb.AppendLine("    SHAFT " + thing.LabelCap + " @" + thing.Position
                            + " pair=" + pair
                            + " exit=" + (exit?.LabelCap ?? "null")
                            + " pocket=" + (pocket?.ToString() ?? "null")
                            + (ok ? " OK" : " BROKEN"));
                    }
                    else if (thing is PocketMapExit landing)
                    {
                        MapPortal entrance = landing.entrance;
                        sb.AppendLine("    LAND " + thing.LabelCap + " @" + thing.Position
                            + " pair=" + pair
                            + " entrance=" + (entrance?.LabelCap ?? "null")
                            + (entrance != null && IsProperlyLinked(entrance, landing) ? " OK" : " BROKEN"));
                    }
                }
            }
            return sb.ToString();
        }

        // Carves a small chamber out of the rock and spawns a portal's bottom
        // landing there. Must run while PocketMapUtility.currentlyGeneratingPortal
        // points at the entrance (during map generation or GeneratePocketMapInt):
        // PocketMapExit.SpawnSetup uses it to wire entrance and exit together.
        public static PocketMapExit SpawnLanding(ThingDef exitDef, IntVec3 cell, Map level, Rot4? rot = null)
        {
            ArrivalZoneUtility.PrepareLandingCell(level, cell);
            Rot4 spawnRot = rot ?? PocketMapUtility.currentlyGeneratingPortal?.Rotation ?? Rot4.North;
            PocketMapExit landing = (PocketMapExit)GenSpawn.Spawn(
                ThingMaker.MakeThing(exitDef), cell, level, spawnRot);
            MapPortal entrance = PocketMapUtility.currentlyGeneratingPortal;
            if (entrance != null && landing != null)
            {
                // Vanilla SpawnSetup usually wires entrance/exit; stamp pair IDs
                // and repair if that wiring was incomplete.
                ConnectPortalPair(entrance, landing, log: false);
            }
            return landing;
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
