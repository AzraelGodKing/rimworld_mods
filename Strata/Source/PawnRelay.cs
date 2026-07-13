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

        private static readonly Dictionary<int, int> lastRelayTick = new Dictionary<int, int>();

        // Tick-stamped and keyed by pawn ID, so entries from one save are
        // garbage in another (loading an earlier save leaves future-dated
        // cooldowns that silently suppress relays). Cleared on game load.
        internal static void ResetSession()
        {
            lastRelayTick.Clear();
        }

        public static bool CanRelay(Pawn pawn)
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
            if (lastRelayTick.TryGetValue(pawn.thingIDNumber, out int tick)
                && Find.TickManager.TicksGame - tick < CooldownTicks)
            {
                return false;
            }
            return LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public static Job MakeRelayJob(Pawn pawn, MapPortal firstStep)
        {
            if (firstStep == null || !firstStep.Spawned || firstStep.Map != pawn.Map)
            {
                return null;
            }
            if (!firstStep.IsEnterable(out _))
            {
                return null;
            }
            if (!pawn.CanReach(firstStep, PathEndMode.Touch, Danger.Some))
            {
                return null;
            }
            lastRelayTick[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            return JobMaker.MakeJob(JobDefOf.EnterPortal, firstStep);
        }

        // Relay toward a level only if fewer than 'cap' pawns are already headed
        // there for the same reason - stops a whole colony stampeding to one job
        // or one free bed. Registers the claim on success.
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
            }
            return job;
        }

        public static int ClaimableBedCount(Pawn pawn, Map map)
        {
            int count = 0;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed
                    && bed.Faction == Faction.OfPlayer
                    && bed.def.building.bed_humanlike
                    && !bed.Medical
                    && !bed.ForPrisoners
                    && bed.AnyUnownedSleepingSlot
                    && !bed.IsForbidden(pawn)
                    && !bed.IsBurning())
                {
                    count++;
                }
            }
            return count;
        }

        // Cheap, conservative "is there plausibly work for this pawn over there?"
        // checks. Deliberately approximate: a false positive just costs a walk
        // down the stairs, and the cooldown stops it from repeating.
        public static bool HasWorkFor(Pawn pawn, Map map)
        {
            Pawn_WorkSettings work = pawn.workSettings;
            if (work == null || !work.EverWork)
            {
                return StrataPawnUtility.IsMiscRobot(pawn)
                    && StrataPawnUtility.MiscRobotHasWorkOn(pawn, map);
            }

            bool Active(WorkTypeDef type) => type != null && work.WorkIsActive(type);

            if (Active(WorkTypeDefOf.Construction))
            {
                if (AnyPlayerThing(map, ThingRequestGroup.Blueprint) || AnyPlayerThing(map, ThingRequestGroup.BuildingFrame))
                {
                    return true;
                }
                if (map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Deconstruct)
                    || map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.SmoothWall)
                    || map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.SmoothFloor))
                {
                    return true;
                }
            }

            if (Active(StrataDefOf.Mining)
                && map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Mine))
            {
                return true;
            }

            if (Active(StrataDefOf.PlantCutting)
                && (map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.CutPlant)
                    || map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.HarvestPlant)))
            {
                return true;
            }

            if (Active(WorkTypeDefOf.Hauling)
                && map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0)
            {
                return true;
            }

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
                if (thing is Building_Bed bed
                    && bed.Faction == Faction.OfPlayer
                    && bed.def.building.bed_humanlike
                    && !bed.Medical
                    && !bed.ForPrisoners
                    && bed.AnyUnownedSleepingSlot
                    && !bed.IsForbidden(pawn)
                    && !bed.IsBurning())
                {
                    return true;
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
