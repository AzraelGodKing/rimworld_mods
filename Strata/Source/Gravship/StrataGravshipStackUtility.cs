using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Collects Strata A+/B+ pocket maps whose entrance portals sit on a
    // gravship's substructure so those floors can travel with the ship.
    public static class StrataGravshipStackUtility
    {
        public static bool IsStrataLinkedLevel(Map map)
        {
            return map != null
                && (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map));
        }

        // Pocket maps opened under/over portals that are onboard the ship.
        public static List<Map> CollectTravellingLevels(Building_GravEngine engine)
        {
            var result = new List<Map>();
            if (engine?.Map == null)
            {
                return result;
            }
            Map host = engine.Map;
            HashSet<IntVec3> substructure = engine.ValidSubstructure;
            if (substructure == null || substructure.Count == 0)
            {
                substructure = engine.AllConnectedSubstructure;
            }
            if (substructure == null || substructure.Count == 0)
            {
                return result;
            }

            var seen = new HashSet<Map>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned)
                {
                    continue;
                }
                if (!PortalOnSubstructure(portal, substructure))
                {
                    continue;
                }
                if (portal is Building_StairsDown down && down.PocketMapExists)
                {
                    AddLevelAndStack(down.PocketMap, result, seen);
                }
                else if (portal.PocketMapExists)
                {
                    AddLevelAndStack(portal.PocketMap, result, seen);
                }
            }

            if (host.ChildPocketMaps != null)
            {
                foreach (Map child in host.ChildPocketMaps)
                {
                    // Only bring children whose stairhead is on the ship.
                    if (HasPortalOnSubstructure(child, host, substructure))
                    {
                        AddLevelAndStack(child, result, seen);
                    }
                }
            }
            return result;
        }

        private static bool HasPortalOnSubstructure(Map child, Map host, HashSet<IntVec3> substructure)
        {
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is Building_StairsDown down && down.Spawned
                    && down.PocketMapExists && down.PocketMap == child
                    && PortalOnSubstructure(down, substructure))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool PortalOnSubstructure(MapPortal portal, HashSet<IntVec3> substructure)
        {
            foreach (IntVec3 cell in portal.OccupiedRect())
            {
                if (substructure.Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddLevelAndStack(Map map, List<Map> result, HashSet<Map> seen)
        {
            if (map == null || !seen.Add(map) || !IsStrataLinkedLevel(map))
            {
                return;
            }
            if (IsQuestSiteLevel(map))
            {
                return;
            }
            result.Add(map);
            if (map.ChildPocketMaps != null)
            {
                foreach (Map child in map.ChildPocketMaps)
                {
                    AddLevelAndStack(child, result, seen);
                }
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (IsStrataLinkedLevel(link.map))
                {
                    AddLevelAndStack(link.map, result, seen);
                }
            }
        }

        private static bool IsQuestSiteLevel(Map map)
        {
            string gen = map?.generatorDef?.defName;
            if (gen == null)
            {
                return false;
            }
            return gen.Contains("SunkenRuin")
                || gen.Contains("CollapsedMine")
                || gen.Contains("SealedVault")
                || gen.Contains("GeothermalVent");
        }

        public static void RebindToHost(Map pocket, Map newHost)
        {
            if (pocket?.Parent is not PocketMapParent parent || newHost == null)
            {
                return;
            }
            parent.sourceMap = newHost;
            if (StrataMapUtility.IsWorldGridTile(newHost.Tile))
            {
                parent.Tile = newHost.Tile;
            }
            PocketMapColonyTileUtility.TryAssign(pocket);
            if (StrataMapUtility.IsUpperLevel(pocket))
            {
                UpperDeckUtility.SyncAllFromSource(pocket);
            }
        }

        public static void RebindAll(IEnumerable<Map> pockets, Map newHost)
        {
            if (pockets == null || newHost == null)
            {
                return;
            }
            foreach (Map pocket in pockets)
            {
                if (pocket != null && Find.Maps.Contains(pocket))
                {
                    RebindToHost(pocket, newHost);
                }
            }
        }
    }
}
