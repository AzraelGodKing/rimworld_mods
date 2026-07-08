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

        public static bool CanRelay(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (!pawn.IsFreeColonist || pawn.Drafted || pawn.InMentalState || pawn.IsBurning())
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

        // Cheap, conservative "is there plausibly work for this pawn over there?"
        // checks. Deliberately approximate: a false positive just costs a walk
        // down the stairs, and the cooldown stops it from repeating.
        public static bool HasWorkFor(Pawn pawn, Map map)
        {
            Pawn_WorkSettings work = pawn.workSettings;
            if (work == null || !work.EverWork)
            {
                return false;
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
                if (building is IBillGiver billGiver && billGiver.BillStack != null
                    && billGiver.BillStack.AnyShouldDoNow)
                {
                    foreach (Bill bill in billGiver.BillStack)
                    {
                        if (!bill.ShouldDoNow())
                        {
                            continue;
                        }
                        WorkTypeDef required = bill.recipe?.requiredGiverWorkType;
                        if (required != null ? Active(required)
                            : (Active(StrataDefOf.Cooking) || Active(StrataDefOf.Crafting)))
                        {
                            return true;
                        }
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
