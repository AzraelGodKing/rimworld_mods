using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public static class StrataPortalUtility
    {
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
                if (thing.def.defName.StartsWith("Strata_Stairs") || thing.def.defName.StartsWith("Strata_Elevator"))
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
