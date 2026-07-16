using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Odyssey helpers: is this map / portal part of a gravship stack?
    public static class StrataGravshipUtility
    {
        public static bool OdysseyActive => ModsConfig.IsActive("Ludeon.RimWorld.Odyssey");

        public static Building_GravEngine FindGravEngine(Map map)
        {
            if (map?.listerBuildings == null)
            {
                return null;
            }
            foreach (Building_GravEngine engine in map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>())
            {
                if (engine != null && engine.Spawned)
                {
                    return engine;
                }
            }
            return null;
        }

        // Host map that currently has a grav engine (landed ship / shipyard).
        public static bool IsGravshipHostMap(Map map)
        {
            return FindGravEngine(map) != null;
        }

        public static bool CellOnGravship(Map map, IntVec3 cell)
        {
            Building_GravEngine engine = FindGravEngine(map);
            if (engine == null || !cell.InBounds(map))
            {
                return false;
            }
            HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub != null && sub.Count > 0 && sub.Contains(cell))
            {
                return true;
            }
            sub = engine.AllConnectedSubstructure;
            if (sub != null && sub.Count > 0 && sub.Contains(cell))
            {
                return true;
            }
            return GravshipUtility.IsOnboardGravship(cell, engine);
        }

        // Colony dig/tower stairs and elevators must stay off the ship footprint —
        // gravship stairwells are the only portals that mark a stack for launch follow.
        public static AcceptanceReport RejectColonyPortalOnGravship(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map)
        {
            if (!OdysseyActive || map == null || FindGravEngine(map) == null)
            {
                return true;
            }
            ThingDef thingDef = def as ThingDef;
            IntVec2 size = thingDef?.size ?? IntVec2.One;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, size))
            {
                if (CellOnGravship(map, cell))
                {
                    return "Cannot build colony stairs on the gravship. Use a gravship stairwell instead.";
                }
            }
            return true;
        }

        public static bool IsGravshipPortal(Thing thing)
        {
            return thing is IStrataGravshipPortal;
        }

        // Pocket level opened by a gravship stair, or currently travelling with a ship.
        public static bool IsGravshipLinkedLevel(Map map)
        {
            if (map == null)
            {
                return false;
            }
            var travelling = WorldComponent_StrataGravshipStacks.Get();
            if (travelling != null && travelling.IsTravelling(map))
            {
                return true;
            }
            if (!StrataMapUtility.IsUnderground(map) && !StrataMapUtility.IsUpperLevel(map))
            {
                return false;
            }
            foreach (Map host in Find.Maps)
            {
                if (host == map)
                {
                    continue;
                }
                foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is not IStrataGravshipPortal)
                    {
                        continue;
                    }
                    if (thing is MapPortal portal && portal.PocketMapExists && portal.PocketMap == map)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // True for the ship host map, or a Strata level tied to gravship stairs.
        public static bool IsGravshipContext(Map map)
        {
            return IsGravshipHostMap(map) || IsGravshipLinkedLevel(map);
        }
    }
}
