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
        }

        private static readonly Dictionary<int, Intent> intents = new Dictionary<int, Intent>();
        private static readonly List<int> pending = new List<int>();

        internal static void ResetSession()
        {
            intents.Clear();
            pending.Clear();
        }

        public static void Mark(
            Pawn pawn,
            Map destMap,
            RelayPurpose purpose,
            Building_Bed preferredBed = null,
            Map returnMap = null,
            IntVec3 preferArrivalNear = default,
            Thing preferredThing = null)
        {
            if (pawn == null || destMap == null)
            {
                return;
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

            intents[pawn.thingIDNumber] = new Intent
            {
                destMapId = destMap.uniqueID,
                purpose = purpose,
                preferredBedId = preferredId,
                returnMapId = returnId,
                carriedPawnId = carriedId,
                preferArrivalNear = preferArrivalNear,
            };
        }

        public static bool HasIntent(Pawn pawn)
        {
            return pawn != null && intents.ContainsKey(pawn.thingIDNumber);
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

        internal static void Tick()
        {
            if (pending.Count == 0)
            {
                return;
            }

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                int id = pending[i];
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
                return;
            }

            // Drafted colonists drop automated relays; robots have no draft.
            if (pawn.Drafted && pawn.IsColonist)
            {
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

            int returnMapId = intent.returnMapId;
            int haulConstructibleId = intent.purpose == RelayPurpose.Haul
                ? intent.preferredBedId
                : -1;
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
                FinishHaul(pawn, returnMapId, haulConstructibleId);
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
                if (StrataConstructAcrossLevels.TryFinishFetch(pawn))
                {
                    return;
                }
                CrossLevelOrderedJobs.Finish(pawn);
                return;
            }

            // Food / work / joy: idle so vanilla jobgivers take over.
            // Medical finishes above with LayDown on the pinned bed.
        }

        private static void FinishHaul(Pawn pawn, int returnMapId, int constructibleId)
        {
            bool delivering = StrataPortalUtility.TryStartHaulDelivery(pawn, constructibleId);
            // Soft-compat: unload Pick Up And Haul inventory into dest storage.
            StrataPuahSoftCompat.TryDeliverInventory(pawn);

            Map returnMap = returnMapId > 0 ? FindMap(returnMapId) : null;
            if (returnMap == null || returnMap == pawn.Map)
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
                // Keep carried materials — InterruptForced otherwise drops wood/steel
                // and the force-build looks "forgotten".
                pawn.jobs.StartJob(
                    home,
                    JobCondition.InterruptForced,
                    keepCarryingThingOverride: true);
            }
        }

        private static void FinishRest(Pawn pawn, int preferredBedId)
        {
            Building_Bed bed = ResolveRestArrivalBed(pawn, preferredBedId);
            if (bed == null || !pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
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
    }
}
