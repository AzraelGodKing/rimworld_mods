using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Every relay only issues one EnterPortal. Vanilla clears the job queue after
    // OnEntered, so multi-hop trips (B2 → B1 → surface → A1) must keep commuting
    // until the destination map. Rest finishes with LayDown; other purposes clear
    // the intent and let vanilla AI take over on arrival.
    public static class PortalRelayChain
    {
        private class Intent
        {
            public int destMapId;
            public RelayPurpose purpose;
            public int preferredBedId; // Rest / warden / childcare / platform; -1 = any
            public int returnMapId; // Haul: go back for another load; -1 = none
            public int carriedPawnId; // Warden / childcare / containment captive; -1 = none
            public IntVec3 preferArrivalNear; // pick stair landing that can reach this
            public string intendedWorkDefName; // Work relay: try this work type on arrival
        }

        private static readonly Dictionary<int, Intent> intents = new Dictionary<int, Intent>();
        private static readonly List<int> pending = new List<int>();

        // The trip intent is cleared on arrival, before JobGiver_Work runs.
        // Without this, the work type that caused the climb is already gone
        // and the pawn takes some other job.
        private static readonly Dictionary<int, string> arrivalWork = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> arrivalWorkUntil = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> arrivalWorkMap = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> departureMap = new Dictionary<int, int>();

        // Brief pause so a job stored before the climb can start.
        private const int ArrivalWorkDeferTicks = 180;

        internal static void ResetSession()
        {
            intents.Clear();
            pending.Clear();
            arrivalWork.Clear();
            arrivalWorkUntil.Clear();
            arrivalWorkMap.Clear();
            departureMap.Clear();
        }

        public static void Mark(
            Pawn pawn,
            Map destMap,
            RelayPurpose purpose,
            Building_Bed preferredBed = null,
            Map returnMap = null,
            IntVec3 preferArrivalNear = default,
            Thing preferredThing = null,
            string intendedWorkDefName = null)
        {
            if (pawn == null || destMap == null)
            {
                return;
            }

            if (intendedWorkDefName == null
                && purpose == RelayPurpose.Work
                && arrivalWork.TryGetValue(pawn.thingIDNumber, out string stagedWork))
            {
                intendedWorkDefName = stagedWork;
            }

            arrivalWork.Remove(pawn.thingIDNumber);
            arrivalWorkUntil.Remove(pawn.thingIDNumber);
            arrivalWorkMap.Remove(pawn.thingIDNumber);
            if (pawn.Map != null && pawn.Map != destMap)
            {
                departureMap[pawn.thingIDNumber] = pawn.Map.uniqueID;
            }

            int carriedId = -1;
            if (pawn.carryTracker?.CarriedThing is Pawn carried)
            {
                carriedId = carried.thingIDNumber;
            }

            Thing anchor = (Thing)preferredBed ?? preferredThing;
            if ((!preferArrivalNear.IsValid || !preferArrivalNear.InBounds(destMap))
                && anchor != null && anchor.Spawned && anchor.Map == destMap)
            {
                preferArrivalNear = anchor.Position;
            }

            int returnId = returnMap?.uniqueID ?? -1;
            int preferredId = preferredBed?.thingIDNumber
                ?? preferredThing?.thingIDNumber
                ?? -1;

            // Mid-hop TryRelayToMap re-Marks without returnMap / constructible.
            // Preserve haul home + force-build site across intermediate landings.
            if (purpose == RelayPurpose.Haul
                && intents.TryGetValue(pawn.thingIDNumber, out Intent prior)
                && prior.purpose == RelayPurpose.Haul)
            {
                if (returnId < 0 && prior.returnMapId > 0)
                {
                    returnId = prior.returnMapId;
                }
                if (preferredId < 0 && prior.preferredBedId > 0)
                {
                    preferredId = prior.preferredBedId;
                }
                if ((!preferArrivalNear.IsValid || !preferArrivalNear.InBounds(destMap))
                    && prior.preferArrivalNear.IsValid)
                {
                    preferArrivalNear = prior.preferArrivalNear;
                }
            }

            if (intendedWorkDefName == null
                && purpose == RelayPurpose.Work
                && intents.TryGetValue(pawn.thingIDNumber, out Intent priorWork)
                && priorWork.purpose == RelayPurpose.Work)
            {
                intendedWorkDefName = priorWork.intendedWorkDefName;
            }

            intents[pawn.thingIDNumber] = new Intent
            {
                destMapId = destMap.uniqueID,
                purpose = purpose,
                preferredBedId = preferredId,
                returnMapId = returnId,
                carriedPawnId = carriedId,
                preferArrivalNear = preferArrivalNear,
                intendedWorkDefName = intendedWorkDefName,
            };
        }

        public static bool HasIntent(Pawn pawn)
        {
            return pawn != null && intents.ContainsKey(pawn.thingIDNumber);
        }

        public static string IntendedWorkDefName(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            if (arrivalWork.TryGetValue(pawn.thingIDNumber, out string staged)
                && !string.IsNullOrEmpty(staged))
            {
                return staged;
            }
            if (!intents.TryGetValue(pawn.thingIDNumber, out Intent intent))
            {
                return null;
            }
            return intent.purpose == RelayPurpose.Work ? intent.intendedWorkDefName : null;
        }

        public static int DepartureMapId(Pawn pawn)
        {
            if (pawn != null && departureMap.TryGetValue(pawn.thingIDNumber, out int mapId))
            {
                return mapId;
            }
            return -1;
        }

        // True only for a moment after arrival, so a player order stored before
        // the climb can still start. The work assignment itself lasts longer.
        public static bool DeferringOrderedJobs(Pawn pawn)
        {
            if (pawn == null || !arrivalWork.ContainsKey(pawn.thingIDNumber))
            {
                return false;
            }
            int now = Find.TickManager?.TicksGame ?? 0;
            return arrivalWorkUntil.TryGetValue(pawn.thingIDNumber, out int until) && until > now;
        }

        internal static void RememberArrivalWork(int pawnId, string workDefName, int untilTick, int mapId)
        {
            if (pawnId <= 0 || string.IsNullOrEmpty(workDefName))
            {
                return;
            }
            arrivalWork[pawnId] = workDefName;
            arrivalWorkUntil[pawnId] = untilTick;
            if (mapId > 0)
            {
                arrivalWorkMap[pawnId] = mapId;
            }
        }

        private static void StageArrivalWork(int pawnId, string workDefName, int mapId)
        {
            if (pawnId <= 0 || string.IsNullOrEmpty(workDefName))
            {
                return;
            }
            int now = Find.TickManager?.TicksGame ?? 0;
            arrivalWork[pawnId] = workDefName;
            arrivalWorkMap[pawnId] = mapId;
            arrivalWorkUntil[pawnId] = now + ArrivalWorkDeferTicks;
        }

        internal static List<CrossFloorArrivalWorkRecord> CaptureArrivalWork()
        {
            var saved = new List<CrossFloorArrivalWorkRecord>(arrivalWork.Count);
            foreach (KeyValuePair<int, string> pair in arrivalWork)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }
                arrivalWorkUntil.TryGetValue(pair.Key, out int until);
                arrivalWorkMap.TryGetValue(pair.Key, out int mapId);
                saved.Add(new CrossFloorArrivalWorkRecord
                {
                    pawnId = pair.Key,
                    workDefName = pair.Value,
                    untilTick = until,
                    mapId = mapId,
                });
            }
            return saved;
        }

        public static bool IsBreakableWorkCommute(Pawn pawn)
        {
            if (pawn == null || !intents.TryGetValue(pawn.thingIDNumber, out Intent intent))
            {
                return false;
            }

            return intent.purpose == RelayPurpose.Work
                || intent.purpose == RelayPurpose.Haul
                || intent.purpose == RelayPurpose.ForcedOrder;
        }

        // Sleep, food, joy, or a drug need. Drop the stair chain. The floor
        // assignment stays, so they come back after the need is met.
        public static void ReleaseWorkCommute(Pawn pawn)
        {
            if (!IsBreakableWorkCommute(pawn))
            {
                return;
            }

            intents.Remove(pawn.thingIDNumber);
            StrataConstructAcrossLevels.ClearPending(pawn);
            BillIngredientUtility.ClearPending(pawn);
        }

        public static bool HasIntent(Pawn pawn, RelayPurpose purpose)
        {
            return pawn != null
                && intents.TryGetValue(pawn.thingIDNumber, out Intent intent)
                && intent.purpose == purpose;
        }

        public static bool HasIntent(int pawnId, RelayPurpose purpose)
        {
            return intents.TryGetValue(pawnId, out Intent intent)
                && intent.purpose == purpose;
        }

        public static void ClearIntent(Pawn pawn)
        {
            if (pawn != null)
            {
                intents.Remove(pawn.thingIDNumber);
            }
        }

        public static void NotifyPortalArrival(Pawn pawn)
        {
            if (pawn == null || !intents.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            if (!pending.Contains(pawn.thingIDNumber))
            {
                pending.Add(pawn.thingIDNumber);
            }
        }

        /// <summary>
        /// Finish stair arrivals on the pawn's map tick. World DoSingleTick is
        /// async-world in Multiplayer; MakeJob there desyncs UniqueIDs.
        /// </summary>
        internal static void TickMap(Map map)
        {
            if (map == null || pending.Count == 0)
            {
                return;
            }

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                int id = pending[i];
                Pawn pawn = FindPawn(id);
                // Mid-portal pawns are unspawned; wait for the dest map tick.
                if (pawn == null || pawn.Map != map)
                {
                    continue;
                }

                pending.RemoveAt(i);
                Continue(id);
            }
        }

        private static void Continue(int pawnId)
        {
            if (!intents.TryGetValue(pawnId, out Intent intent))
            {
                return;
            }

            Pawn pawn = FindPawn(pawnId);
            if (pawn?.jobs == null || !pawn.Spawned || pawn.Downed || pawn.Dead)
            {
                intents.Remove(pawnId);
                if (pawn == null || pawn.Dead)
                {
                    CrossLevelOrderedJobs.Release(pawnId);
                }
                else
                {
                    CrossLevelOrderedJobs.ArmRetry(pawnId);
                }
                return;
            }

            if ((intent.purpose == RelayPurpose.Work
                    || intent.purpose == RelayPurpose.Haul
                    || intent.purpose == RelayPurpose.ForcedOrder)
                && NeedsBreak.ShouldBreak(pawn))
            {
                intents.Remove(pawnId);
                StrataConstructAcrossLevels.ClearPending(pawn);
                BillIngredientUtility.ClearPending(pawn);
                return;
            }

            // Drafted colonists drop automated relays. A player order still
            // finishes: ruin stairs draft on the way in, and that was cancelling it.
            if (pawn.Drafted && pawn.IsColonist
                && intent.purpose != RelayPurpose.ForcedOrder
                && intent.purpose != RelayPurpose.Guard)
            {
                // Already on the floor they were sent to. Keep the work so
                // undrafting does not send them back up the stairs.
                if (intent.purpose == RelayPurpose.Work
                    && pawn.Map != null
                    && pawn.Map.uniqueID == intent.destMapId)
                {
                    StageArrivalWork(pawnId, intent.intendedWorkDefName, intent.destMapId);
                }
                intents.Remove(pawnId);
                return;
            }

            Map destMap = FindMap(intent.destMapId);
            if (destMap == null)
            {
                intents.Remove(pawnId);
                return;
            }

            if (pawn.Map != destMap)
            {
                // Childcare / warden hops need the captive — re-grab if vanilla
                // EnterPortal dropped them on a mid landing.
                if (intent.purpose == RelayPurpose.Childcare
                    || intent.purpose == RelayPurpose.Warden
                    || intent.purpose == RelayPurpose.Containment
                    || intent.purpose == RelayPurpose.Rescue)
                {
                    if (pawn.carryTracker?.CarriedThing is not Pawn)
                    {
                        Pawn captive = ResolveCarriedOrNearbyPawn(pawn, intent.carriedPawnId);
                        if (captive == null || !captive.Spawned || captive.Map != pawn.Map
                            || !pawn.carryTracker.TryStartCarry(captive))
                        {
                            intents.Remove(pawnId);
                            return;
                        }
                    }
                }

                Building_Bed bed = intent.purpose != RelayPurpose.Containment && intent.preferredBedId > 0
                    ? FindBedById(destMap, intent.preferredBedId)
                    : null;
                if (intent.purpose == RelayPurpose.Containment && intent.preferredBedId > 0)
                {
                    Thing platform = FindThingById(destMap, intent.preferredBedId);
                    if (platform != null)
                    {
                        intent.preferArrivalNear = platform.Position;
                    }
                }
                Job hop = PawnRelay.TryRelayToMap(
                    pawn,
                    destMap,
                    touchCooldown: false,
                    intent.purpose,
                    bed,
                    intent.preferArrivalNear);
                if (hop != null)
                {
                    pawn.jobs.StartJob(
                        hop,
                        JobCondition.InterruptForced,
                        keepCarryingThingOverride: true);
                    return;
                }

                intents.Remove(pawnId);
                return;
            }

            if (intent.purpose == RelayPurpose.Work
                && !string.IsNullOrEmpty(intent.intendedWorkDefName))
            {
                StageArrivalWork(pawn.thingIDNumber, intent.intendedWorkDefName, pawn.Map.uniqueID);
            }

            int returnMapId = intent.returnMapId;
            int haulConstructibleId = intent.purpose == RelayPurpose.Haul
                ? intent.preferredBedId
                : -1;
            IntVec3 haulPreferCell = intent.preferArrivalNear;
            intents.Remove(pawnId);

            if (intent.purpose == RelayPurpose.Rest)
            {
                FinishRest(pawn, intent.preferredBedId);
                return;
            }

            if (intent.purpose == RelayPurpose.Medical)
            {
                FinishMedical(pawn, intent.preferredBedId);
                return;
            }

            if (intent.purpose == RelayPurpose.Haul)
            {
                FinishHaul(pawn, returnMapId, haulConstructibleId, haulPreferCell);
                return;
            }

            if (intent.purpose == RelayPurpose.Childcare)
            {
                FinishChildcare(pawn, intent.preferredBedId, intent.carriedPawnId);
                return;
            }

            if (intent.purpose == RelayPurpose.Warden)
            {
                FinishWarden(pawn, intent.preferredBedId, intent.carriedPawnId);
                return;
            }

            if (intent.purpose == RelayPurpose.Rescue)
            {
                FinishRescue(pawn, intent.preferredBedId, intent.carriedPawnId);
                return;
            }

            if (intent.purpose == RelayPurpose.Containment)
            {
                FinishContainment(pawn, intent.preferredBedId, intent.carriedPawnId);
                return;
            }

            if (intent.purpose == RelayPurpose.ForcedOrder)
            {
                if (StrataConstructAcrossLevels.TryFinishFetch(pawn)
                    || BillIngredientUtility.TryFinishFetch(pawn))
                {
                    return;
                }
                CrossLevelOrderedJobs.Finish(pawn);
                return;
            }

            if (intent.purpose == RelayPurpose.Work)
            {
                // Free stampede slot.
                RelayClaims.Release(pawn);
            }

            // Food / work / joy: idle so vanilla jobgivers take over.
            // Medical finishes above with LayDown on the pinned bed.
        }

        private static void FinishHaul(
            Pawn pawn,
            int returnMapId,
            int constructibleId,
            IntVec3 preferStoreCell)
        {
            bool delivering = StrataPortalUtility.TryStartHaulDelivery(
                pawn, constructibleId, preferStoreCell);
            // Soft-compat: unload Pick Up And Haul inventory into dest storage.
            StrataPuahSoftCompat.TryDeliverInventory(pawn);

            // Construction/frame delivery failed — reassign to storage on this floor
            // before dropping at feet or stair-ping-ponging home with cargo.
            if (!delivering
                && pawn.carryTracker?.CarriedThing != null
                && pawn.carryTracker.CarriedThing is not Pawn)
            {
                delivering = StrataPortalUtility.TryStartStorageDelivery(pawn, preferStoreCell: preferStoreCell);
            }

            if (!delivering
                && pawn.carryTracker?.CarriedThing != null
                && pawn.carryTracker.CarriedThing is not Pawn)
            {
                StrataPortalUtility.TryForceDropCarriedCargo(pawn);
            }

            Map returnMap = returnMapId > 0 ? FindMap(returnMapId) : null;
            if (returnMap == null || returnMap == pawn.Map)
            {
                return;
            }

            // This floor is their work. Stay and build or cook. A return trip
            // for "another load" sent them up and down a few times first.
            if (JobRelay.AssignedTo(pawn, pawn.Map))
            {
                return;
            }

            // Still carrying after failed storage + drop — do not stair-ping-pong.
            if (pawn.carryTracker?.CarriedThing != null
                && pawn.carryTracker.CarriedThing is not Pawn)
            {
                return;
            }

            Job home = PawnRelay.TryRelayToMap(
                pawn,
                returnMap,
                touchCooldown: false,
                RelayPurpose.Haul);
            if (home == null)
            {
                // Stair congested / temporarily unreachable — retry next ticks
                // instead of wandering forever on the delivery floor.
                Mark(pawn, returnMap, RelayPurpose.Haul);
                NotifyPortalArrival(pawn);
                return;
            }

            if (delivering)
            {
                pawn.jobs.jobQueue.EnqueueLast(home, JobTag.Misc);
            }
            else
            {
                // Empty-handed return for another load.
                pawn.jobs.StartJob(
                    home,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            }
        }

        private static void FinishRest(Pawn pawn, int preferredBedId)
        {
            Building_Bed bed = ResolveRestArrivalBed(pawn, preferredBedId);
            if (bed == null)
            {
                return;
            }
            if (!pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                Job detour = DraftedPortalPathing.TryMakeRestDetourJob(pawn, bed);
                if (detour != null)
                {
                    pawn.jobs.StartJob(detour, JobCondition.InterruptForced);
                }
                return;
            }

            // Never claim a different bed on arrival — ownership was decided
            // before the commute (owned bed or homeless claim).
            pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        }

        private static void FinishMedical(Pawn pawn, int preferredBedId)
        {
            Building_Bed bed = preferredBedId > 0 ? FindBedById(pawn.Map, preferredBedId) : null;
            if (bed == null || !bed.Medical || !bed.AnyUnoccupiedSleepingSlot
                || bed.IsForbidden(pawn) || bed.IsBurning())
            {
                bed = PawnRelay.FindMedicalBedFor(pawn, pawn.Map);
            }
            if (bed == null || !pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return;
            }

            pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        }

        private static void FinishChildcare(Pawn hauler, int preferredBedId, int carriedPawnId)
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            Pawn baby = ResolveCarriedOrNearbyPawn(hauler, carriedPawnId);
            if (baby == null || !ChildcareUtility.CanSuckle(baby, out _))
            {
                return;
            }

            Building_Bed crib = preferredBedId > 0
                ? FindBedById(hauler.Map, preferredBedId)
                : null;
            if (crib == null)
            {
                crib = baby.ownership?.OwnedBed;
            }

            if (crib == null || !crib.Spawned || crib.Map != hauler.Map
                || !hauler.CanReach(crib, PathEndMode.OnCell, Danger.Deadly))
            {
                // Crib missing/unreachable — let vanilla childcare reassess.
                Job fallback = JobMaker.MakeJob(JobDefOf.BringBabyToSafetyUnforced, baby);
                fallback.count = 1;
                hauler.jobs.StartJob(
                    fallback,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
                return;
            }

            // Same-map tuck: Rescue/TakeToBed keeps the carried infant and
            // places them in the assigned crib (BringBabyToSafety would also
            // work, but Rescue targets the crib directly).
            Job job = JobMaker.MakeJob(JobDefOf.Rescue, baby, crib);
            job.count = 1;
            hauler.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                keepCarryingThingOverride: true);
        }

        private static void FinishWarden(Pawn warden, int preferredBedId, int carriedPawnId)
        {
            Pawn prisoner = ResolveCarriedOrNearbyPawn(warden, carriedPawnId);
            if (prisoner == null)
            {
                return;
            }

            Building_Bed bed = preferredBedId > 0
                ? FindBedById(warden.Map, preferredBedId)
                : null;
            if (bed == null)
            {
                bed = prisoner.ownership?.OwnedBed;
            }

            if (bed == null || !bed.Spawned || bed.Map != warden.Map
                || !bed.ForPrisoners
                || !warden.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return;
            }

            JobDef def;
            if (!prisoner.IsPrisonerOfColony)
            {
                // Capture / arrest finish — makeTargetPrisoner tucks them in.
                def = prisoner.CanBeCaptured() || prisoner.Downed
                    ? JobDefOf.Capture
                    : JobDefOf.Arrest;
            }
            else if (prisoner.Downed && HealthAIUtility.ShouldSeekMedicalRest(prisoner))
            {
                def = JobDefOf.TakeWoundedPrisonerToBed;
            }
            else
            {
                def = JobDefOf.EscortPrisonerToBed;
            }

            Job job = JobMaker.MakeJob(def, prisoner, bed);
            job.count = 1;
            warden.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                keepCarryingThingOverride: true);
        }

        private static void FinishRescue(Pawn rescuer, int preferredBedId, int carriedPawnId)
        {
            Pawn patient = ResolveCarriedOrNearbyPawn(rescuer, carriedPawnId);
            if (patient == null)
            {
                return;
            }

            Building_Bed bed = preferredBedId > 0
                ? FindBedById(rescuer.Map, preferredBedId)
                : null;
            if (bed == null || !bed.Spawned || bed.Map != rescuer.Map || bed.ForPrisoners
                || (!bed.AnyUnoccupiedSleepingSlot && !bed.IsOwner(patient, out _))
                || !rescuer.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                bed = RestUtility.FindBedFor(
                        patient,
                        rescuer,
                        checkSocialProperness: false,
                        ignoreOtherReservations: true)
                    ?? RestUtility.FindPatientBedFor(patient);
            }

            if (bed == null || bed.Map != rescuer.Map
                || !rescuer.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Rescue, patient, bed);
            job.count = 1;
            rescuer.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                keepCarryingThingOverride: true);
        }

        private static void FinishContainment(Pawn hauler, int preferredPlatformId, int carriedPawnId)
        {
            if (!ModsConfig.AnomalyActive)
            {
                return;
            }
            Pawn entity = ResolveCarriedOrNearbyPawn(hauler, carriedPawnId);
            if (entity == null)
            {
                return;
            }
            Thing platform = preferredPlatformId > 0
                ? FindThingById(hauler.Map, preferredPlatformId)
                : null;
            if (platform == null || !platform.Spawned || platform.Map != hauler.Map
                || !hauler.CanReach(platform, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return;
            }
            CompEntityHolder holder = platform.TryGetComp<CompEntityHolder>();
            if (holder == null || !holder.Available)
            {
                return;
            }

            // Vanilla CompHoldingPlatformTarget clears targetHolder when maps differ;
            // only assign once we are on the platform's floor.
            CompHoldingPlatformTarget target = entity.TryGetComp<CompHoldingPlatformTarget>();
            if (target != null)
            {
                target.targetHolder = platform;
            }

            // TargetA = platform, TargetB = entity (same as vanilla CaptureEntity).
            JobDef def = hauler.carryTracker?.CarriedThing == entity
                ? JobDefOf.CarryToEntityHolderAlreadyHolding
                : JobDefOf.CarryToEntityHolder;
            Job job = JobMaker.MakeJob(def, platform, entity);
            job.count = 1;
            hauler.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                keepCarryingThingOverride: true);
        }

        private static Pawn ResolveCarriedOrNearbyPawn(Pawn carrier, int carriedPawnId)
        {
            if (carrier.carryTracker?.CarriedThing is Pawn carried)
            {
                return carried;
            }

            if (carriedPawnId <= 0 || carrier.Map == null)
            {
                return null;
            }

            // Vanilla EnterPortal may have dropped them on the landing.
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
                         carrier.Position,
                         carrier.Map,
                         2.9f,
                         true))
            {
                if (thing is Pawn p && p.thingIDNumber == carriedPawnId)
                {
                    return p;
                }
            }

            return FindPawn(carriedPawnId);
        }

        private static Building_Bed ResolveRestArrivalBed(Pawn pawn, int preferredBedId)
        {
            // Owned bed on this map first — never land in a random double.
            Building_Bed owned = pawn.ownership?.OwnedBed;
            if (owned != null && owned.Spawned && owned.Map == pawn.Map)
            {
                return owned;
            }

            Building_Bed preferred = FindBedById(pawn.Map, preferredBedId);
            if (preferred != null)
            {
                return preferred;
            }

            return null;
        }

        private static Building_Bed FindBedById(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && bed.thingIDNumber == thingId)
                {
                    return bed;
                }
            }

            return null;
        }

        private static Thing FindThingById(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }
            List<Thing> all = map.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].thingIDNumber == thingId)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Map FindMap(int uniqueId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == uniqueId)
                {
                    return maps[i];
                }
            }

            return null;
        }

        private static Pawn FindPawn(int thingId)
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

        internal static List<CrossFloorIntentRecord> CaptureIntents()
        {
            var saved = new List<CrossFloorIntentRecord>(intents.Count);
            foreach (KeyValuePair<int, Intent> pair in intents)
            {
                Intent intent = pair.Value;
                saved.Add(new CrossFloorIntentRecord
                {
                    pawnId = pair.Key,
                    destMapId = intent.destMapId,
                    purpose = intent.purpose,
                    preferredBedId = intent.preferredBedId,
                    returnMapId = intent.returnMapId,
                    carriedPawnId = intent.carriedPawnId,
                    preferArrivalNear = intent.preferArrivalNear,
                    intendedWorkDefName = intent.intendedWorkDefName,
                });
            }
            return saved;
        }

        // Called from LoadedGame, after session reset has cleared this dictionary.
        internal static void RestoreIntents(List<CrossFloorIntentRecord> saved)
        {
            if (saved == null || saved.Count == 0)
            {
                return;
            }

            for (int i = 0; i < saved.Count; i++)
            {
                CrossFloorIntentRecord record = saved[i];
                if (record == null)
                {
                    continue;
                }
                Pawn pawn = FindPawn(record.pawnId);
                Map dest = FindMap(record.destMapId);
                if (pawn == null || pawn.Dead || dest == null)
                {
                    continue;
                }
                if (record.carriedPawnId > 0 && !PawnStillExists(record.carriedPawnId))
                {
                    continue;
                }
                if (PurposePinsBed(record.purpose)
                    && record.preferredBedId > 0
                    && FindBedById(dest, record.preferredBedId) == null)
                {
                    continue;
                }
                if (record.purpose == RelayPurpose.Containment
                    && record.preferredBedId > 0
                    && FindThingById(dest, record.preferredBedId) == null)
                {
                    continue;
                }
                if (record.purpose == RelayPurpose.ForcedOrder)
                {
                    CrossLevelOrderedJobs.HoldForArrival(pawn);
                }

                int returnMapId = record.returnMapId;
                if (returnMapId > 0 && FindMap(returnMapId) == null)
                {
                    returnMapId = -1;
                }

                intents[pawn.thingIDNumber] = new Intent
                {
                    destMapId = dest.uniqueID,
                    purpose = record.purpose,
                    preferredBedId = record.preferredBedId,
                    returnMapId = returnMapId,
                    carriedPawnId = record.carriedPawnId,
                    preferArrivalNear = record.preferArrivalNear,
                    intendedWorkDefName = record.intendedWorkDefName,
                };
                NotifyPortalArrival(pawn);
            }
        }

        private static bool PurposePinsBed(RelayPurpose purpose)
        {
            return purpose == RelayPurpose.Rest
                || purpose == RelayPurpose.Medical
                || purpose == RelayPurpose.Childcare
                || purpose == RelayPurpose.Warden
                || purpose == RelayPurpose.Rescue;
        }

        private static bool PawnStillExists(int thingId)
        {
            if (FindPawn(thingId) != null)
            {
                return true;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> pawns = maps[i].mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    if (pawns[j].thingIDNumber == thingId)
                    {
                        return true;
                    }
                    if (pawns[j].carryTracker?.CarriedThing is Pawn carried
                        && carried.thingIDNumber == thingId)
                    {
                        return true;
                    }
                }
                foreach (Pawn mapPawn in maps[i].mapPawns.AllPawns)
                {
                    if (mapPawn.thingIDNumber == thingId)
                    {
                        return true;
                    }
                }
            }
            if (Find.WorldPawns == null)
            {
                return false;
            }
            foreach (Pawn worldPawn in Find.WorldPawns.AllPawnsAlive)
            {
                if (worldPawn.thingIDNumber == thingId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
