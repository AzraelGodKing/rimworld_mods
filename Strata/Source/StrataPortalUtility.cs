using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public static class StrataPortalUtility
    {
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
            return mode != DestroyMode.Vanish && mode != DestroyMode.WillReplace;
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
