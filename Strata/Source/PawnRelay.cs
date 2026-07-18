using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // The core of Strata's fluidity: instead of building fragile cross-map jobs,
    // we relay the PAWN to the level where something needs doing and let the
    // completely vanilla AI take over once it arrives. Every failure mode
    // degrades to "pawn walks back upstairs and re-thinks", which is safe.
    public static class PawnRelay
    {
        // Don't re-relay the same pawn for a while, so a bad signal (e.g. work
        // it turns out it can't actually do) can't ping-pong it between levels.
        private const int CooldownTicks = 1500;

        // Misc. Robots return-to-base can re-fire Goto every think pass when the
        // recharge room is on another level — throttle pathfinding retries.
        private const int ReturnBaseRetryCooldownTicks = 6000;

        // Idle colonists with null work jobs scan linked levels every think
        // pass; throttle that BFS/work probe separately from relay cooldown.
        private const int RobotWorkScanCooldownTicks = 7500;

        private const int ColonistWorkScanCooldownTicks = 7500;

        private static readonly Dictionary<int, int> lastRelayTick = new Dictionary<int, int>();

        private static readonly Dictionary<int, int> lastReturnBaseAttemptTick = new Dictionary<int, int>();

        private static readonly Dictionary<int, int> lastRobotWorkScanTick = new Dictionary<int, int>();

        private static readonly Dictionary<int, int> lastColonistWorkScanTick = new Dictionary<int, int>();

        // Tick-stamped and keyed by pawn ID, so entries from one save are
        // garbage in another (loading an earlier save leaves future-dated
        // cooldowns that silently suppress relays). Cleared on game load.
        internal static void ResetSession()
        {
            lastRelayTick.Clear();
            lastReturnBaseAttemptTick.Clear();
            lastRobotWorkScanTick.Clear();
            lastColonistWorkScanTick.Clear();
            StrataPawnUtility.ResetMiscRobotCaches();
            LevelGraph.InvalidateCache();
        }

        internal static bool IsOnRelayCooldown(Pawn pawn)
        {
            return pawn != null
                && lastRelayTick.TryGetValue(pawn.thingIDNumber, out int tick)
                && Find.TickManager.TicksGame - tick < CooldownTicks;
        }

        internal static void TouchRelayCooldown(Pawn pawn)
        {
            if (pawn != null)
            {
                lastRelayTick[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }
        }

        internal static bool IsReturnBaseRetryCooldown(Pawn pawn)
        {
            return pawn != null
                && lastReturnBaseAttemptTick.TryGetValue(pawn.thingIDNumber, out int tick)
                && Find.TickManager.TicksGame - tick < ReturnBaseRetryCooldownTicks;
        }

        internal static void TouchReturnBaseRetry(Pawn pawn)
        {
            if (pawn != null)
            {
                lastReturnBaseAttemptTick[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }
        }

        internal static bool IsRobotWorkScanCooldown(Pawn pawn)
        {
            return pawn != null
                && lastRobotWorkScanTick.TryGetValue(pawn.thingIDNumber, out int tick)
                && Find.TickManager.TicksGame - tick < RobotWorkScanCooldownTicks;
        }

        internal static void TouchRobotWorkScan(Pawn pawn)
        {
            if (pawn != null)
            {
                lastRobotWorkScanTick[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }
        }

        internal static bool IsColonistWorkScanCooldown(Pawn pawn)
        {
            return pawn != null
                && lastColonistWorkScanTick.TryGetValue(pawn.thingIDNumber, out int tick)
                && Find.TickManager.TicksGame - tick < ColonistWorkScanCooldownTicks;
        }

        internal static void TouchColonistWorkScan(Pawn pawn)
        {
            if (pawn != null)
            {
                lastColonistWorkScanTick[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }
        }

        public static bool CanRelay(Pawn pawn)
        {
            return CanRelayBasics(pawn) && !IsOnRelayCooldown(pawn);
        }

        // Portal-capable and linked, ignoring the general relay cooldown.
        // Used for owned-bed home commute so a recent food/work trip can't leave
        // someone sleeping on the floor of the wrong level all night.
        public static bool CanRelayBasics(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn)
                || pawn.Drafted || pawn.InMentalState || pawn.IsBurning())
            {
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return false;
            }
            // Mid multi-hop commute (food/work/rest/etc.) — don't pick a new
            // destination until PortalRelayChain finishes the trip.
            if (PortalRelayChain.HasIntent(pawn))
            {
                return false;
            }
            return LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public static Job MakeRelayJob(Pawn pawn, MapPortal firstStep)
        {
            return MakePortalJob(pawn, firstStep, touchRelayCooldown: true);
        }

        // Return-to-base must not consume the general relay cooldown — bots still
        // need to commute for work while heading home on low charge.
        // destMap is the recharge home so multi-hop chains keep going after each stair.
        internal static Job MakeReturnBasePortalJob(Pawn pawn, MapPortal firstStep, Map destMap)
        {
            Job job = MakePortalJob(pawn, firstStep, touchRelayCooldown: false);
            if (job != null && destMap != null)
            {
                PortalRelayChain.Mark(pawn, destMap, RelayPurpose.Work);
            }

            return job;
        }

        // Walk the first portal toward destMap. No stampede claim (each pawn's
        // owned bed is personal). Optional cooldown so home-to-bed can ignore it.
        // Marks PortalRelayChain so multi-hop destinations keep commuting.
        public static Job TryRelayToMap(
            Pawn pawn,
            Map destMap,
            bool touchCooldown,
            RelayPurpose purpose = RelayPurpose.Work,
            Building_Bed preferredBed = null)
        {
            if (pawn == null || destMap == null || destMap == pawn.Map)
            {
                return null;
            }

            MapPortal firstStep = LevelGraph.BestFirstStep(pawn.Map, destMap, pawn.Position);
            Job job = MakePortalJob(pawn, firstStep, touchCooldown);
            if (job != null)
            {
                PortalRelayChain.Mark(pawn, destMap, purpose, preferredBed);
            }

            return job;
        }

        private static Job MakePortalJob(Pawn pawn, MapPortal firstStep, bool touchRelayCooldown)
        {
            if (firstStep == null || !firstStep.Spawned || firstStep.Map != pawn.Map)
            {
                return null;
            }
            if (!firstStep.IsEnterable(out _))
            {
                return null;
            }
            // Deadly: exhausted colonists still need to reach a sealed-off stair.
            if (!pawn.CanReach(firstStep, PathEndMode.Touch, Danger.Deadly))
            {
                return null;
            }
            if (touchRelayCooldown)
            {
                TouchRelayCooldown(pawn);
            }
            return JobMaker.MakeJob(JobDefOf.EnterPortal, firstStep);
        }

        // Relay toward a level only if fewer than 'cap' pawns are already headed
        // there for the same reason - stops a whole colony stampeding to one job
        // or one free bed. Registers the claim on success and chains multi-hop.
        public static Job TryClaimAndRelay(Pawn pawn, LevelGraph.LevelLink link, RelayPurpose purpose, int cap)
        {
            if (!RelayClaims.CanClaim(pawn, link.map, purpose, cap))
            {
                return null;
            }
            // Take the best portal for THIS pawn (nearest, powered elevators
            // preferred), not just the first one the level graph found.
            MapPortal firstStep = LevelGraph.BestFirstStep(pawn.Map, link.map, pawn.Position) ?? link.firstStep;
            Job job = MakeRelayJob(pawn, firstStep);
            if (job != null)
            {
                RelayClaims.Register(pawn, link.map, purpose);
                PortalRelayChain.Mark(pawn, link.map, purpose);
            }
            return job;
        }

        public static int ClaimableBedCount(Pawn pawn, Map map)
        {
            int count = 0;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && IsClaimableColonistBed(pawn, bed))
                {
                    count++;
                }
            }
            return count;
        }

        // Cheap, conservative "is there plausibly work for this pawn over there?"
        // checks. Deliberately approximate: a false positive just costs a walk
        // down the stairs, and the cooldown stops it from repeating.
        // Mods can extend via WorkRelaySignals.RegisterWorkProbe.
        public static bool HasWorkFor(Pawn pawn, Map map) => WorkRelaySignals.HasWorkFor(pawn, map);

        /// <summary>Mods: see <see cref="WorkRelaySignals.RegisterWorkProbe"/>.</summary>
        public static void RegisterWorkProbe(WorkRelaySignals.WorkProbe probe)
            => WorkRelaySignals.RegisterWorkProbe(probe);

        /// <summary>Mods: see <see cref="WorkRelaySignals.UnregisterWorkProbe"/>.</summary>
        public static void UnregisterWorkProbe(WorkRelaySignals.WorkProbe probe)
            => WorkRelaySignals.UnregisterWorkProbe(probe);

        /// <summary>Mods: see <see cref="WorkRelaySignals.RegisterWorkSeekingJobGiverMarker"/>.</summary>
        public static void RegisterWorkSeekingJobGiverMarker(string typeNameContains)
            => WorkRelaySignals.RegisterWorkSeekingJobGiverMarker(typeNameContains);

        public static bool HasFoodFor(Pawn pawn, Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (thing.IsForbidden(pawn) || thing.Position.Fogged(map))
                {
                    continue;
                }
                if (thing.def.IsIngestible && !pawn.WillEat(thing))
                {
                    continue;
                }
                if (thing.def.IsNutritionGivingIngestible || thing is Building_NutrientPasteDispenser)
                {
                    return true;
                }
            }
            return false;
        }

        // A bed the pawn could walk downstairs and claim: colonist-type, not
        // medical, with a free slot. Vanilla claims it once they arrive.
        public static bool HasClaimableBedFor(Pawn pawn, Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && IsClaimableColonistBed(pawn, bed))
                {
                    return true;
                }
            }
            return false;
        }

        // A colonist bed on this map the pawn can actually reach right now.
        public static bool HasUsableBedOnMap(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
            {
                return false;
            }
            Building_Bed ownBed = pawn.ownership?.OwnedBed;
            if (ownBed != null && ownBed.Spawned && ownBed.Map == map && IsUsableColonistBed(pawn, ownBed))
            {
                return true;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && IsUsableColonistBed(pawn, bed))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ShouldCommuteForRest(Pawn pawn, Job vanillaResult)
        {
            if (vanillaResult?.def == JobDefOf.LayDown)
            {
                return true;
            }
            if (pawn?.CurJobDef == JobDefOf.LayDown)
            {
                return true;
            }
            if (pawn?.timetable?.CurrentAssignment == TimeAssignmentDefOf.Sleep)
            {
                return true;
            }
            Need rest = pawn?.needs?.rest;
            return rest != null && rest.CurLevelPercentage < 0.35f;
        }

        // Owned by this pawn, or any free sleeping slot (unowned).
        internal static bool IsClaimableColonistBed(Pawn pawn, Building_Bed bed)
        {
            return IsColonistBedCandidate(pawn, bed)
                && (bed.OwnersForReading.Contains(pawn) || bed.AnyUnownedSleepingSlot);
        }

        private static bool IsUsableColonistBed(Pawn pawn, Building_Bed bed)
        {
            return IsClaimableColonistBed(pawn, bed)
                && bed.Spawned
                && bed.Map == pawn.Map
                && pawn.CanReach(bed, PathEndMode.OnCell, Danger.Some);
        }

        internal static bool IsUsableColonistBedJob(Pawn pawn, Building_Bed bed)
        {
            return IsUsableColonistBed(pawn, bed);
        }

        private static bool IsColonistBedCandidate(Pawn pawn, Building_Bed bed)
        {
            return bed != null
                && bed.Faction == Faction.OfPlayer
                && bed.ForColonists
                && bed.def.building.bed_humanlike
                && !bed.Medical
                && !bed.ForPrisoners
                && !bed.IsForbidden(pawn)
                && !bed.IsBurning();
        }

        public static bool HasMedicalBedFor(Pawn pawn, Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed
                    && bed.Faction == Faction.OfPlayer
                    && bed.def.building.bed_humanlike
                    && bed.Medical
                    && !bed.ForPrisoners
                    && bed.AnyUnoccupiedSleepingSlot
                    && !bed.IsForbidden(pawn)
                    && !bed.IsBurning())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasPatientsNeedingTend(Map map)
        {
            if (map == null)
            {
                return false;
            }
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn patient = pawns[i];
                if (patient.IsColonist && HealthAIUtility.ShouldBeTendedNowByPlayer(patient))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasJoyFor(Pawn pawn, Map map)
        {
            if (map == null || pawn == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (thing.IsForbidden(pawn) || thing.Position.Fogged(map))
                {
                    continue;
                }
                if (thing.def.IsIngestible && thing.def.ingestible?.joy > 0f && pawn.WillEat(thing))
                {
                    return true;
                }
            }
            foreach (JoyGiverDef joyGiver in DefDatabase<JoyGiverDef>.AllDefsListForReading)
            {
                if (joyGiver.thingDefs == null)
                {
                    continue;
                }
                for (int i = 0; i < joyGiver.thingDefs.Count; i++)
                {
                    ThingDef joyThing = joyGiver.thingDefs[i];
                    foreach (Thing thing in map.listerThings.ThingsOfDef(joyThing))
                    {
                        if (thing.Faction == Faction.OfPlayer && !thing.IsForbidden(pawn) && !thing.IsBurning())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
