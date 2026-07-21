using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Odyssey helpers: is this map / portal part of a gravship stack?
    public static class StrataGravshipUtility
    {
        public static bool OdysseyActive => ModsConfig.IsActive("Ludeon.RimWorld.Odyssey");

        // Grav engine physically on this map (ship deck / host surface).
        // With Vanilla Gravship Expanded, prefer the engine that actually owns substructure
        // (jumper / hulk / vanilla are all Building_GravEngine).
        public static Building_GravEngine FindGravEngineOnMap(Map map)
        {
            if (map?.listerBuildings == null)
            {
                return null;
            }
            if (StrataVgeCompat.Active)
            {
                return StrataVgeCompat.PreferBestEngine(
                    map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>());
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

        /// <summary>G1: exact thingID lookup — never PreferBestEngine.</summary>
        public static Building_GravEngine FindGravEngineByThingId(Map map, int thingId)
        {
            if (map?.listerBuildings == null || thingId < 0)
            {
                return null;
            }
            foreach (Building_GravEngine engine in map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>())
            {
                if (engine != null && engine.Spawned && engine.thingIDNumber == thingId)
                {
                    return engine;
                }
            }
            return null;
        }

        public static Building_GravEngine FindGravEngineByThingIdAnywhere(int thingId)
        {
            if (thingId < 0 || Find.Maps == null)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Building_GravEngine engine = FindGravEngineByThingId(maps[i], thingId);
                if (engine != null)
                {
                    return engine;
                }
            }
            return null;
        }

        /// <summary>
        /// G1 land rebind: when <paramref name="pinnedThingId"/> is set, resolve only that engine.
        /// No PreferBestEngine / first-engine fallback while a pin is active.
        /// </summary>
        public static Building_GravEngine FindGravEngineForLandRebind(Map host, int pinnedThingId)
        {
            if (pinnedThingId >= 0)
            {
                Building_GravEngine pinned = FindGravEngineByThingId(host, pinnedThingId);
                if (pinned != null)
                {
                    return pinned;
                }
                Log.Warning("[Strata] G1: pinned takeoff engine thingID " + pinnedThingId
                    + " not found on host map — skipping PreferBestEngine fallback.");
                return null;
            }
            return FindGravEngineOnMap(host);
        }

        // Host engine, or the engine on the gravship stack root when map is a linked B+/A+ floor.
        public static Building_GravEngine FindGravEngine(Map map)
        {
            Building_GravEngine local = FindGravEngineOnMap(map);
            if (local != null)
            {
                return local;
            }
            if (!OdysseyActive || map == null || !IsGravshipLinkedLevel(map))
            {
                return null;
            }
            Map root = FindGravshipStackRoot(map);
            return root != null && root != map ? FindGravEngineOnMap(root) : null;
        }

        // Host map that currently has a grav engine (landed ship / shipyard).
        public static bool IsGravshipHostMap(Map map)
        {
            return FindGravEngineOnMap(map) != null;
        }

        public static bool CellOnGravship(Map map, IntVec3 cell)
        {
            Building_GravEngine engine = FindGravEngineOnMap(map);
            if (engine == null || engine.Destroyed || !engine.Spawned
                || engine.Map != map || !cell.InBounds(map))
            {
                return false;
            }
            HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub != null && sub.Count > 0)
            {
                return sub.Contains(cell);
            }
            sub = engine.AllConnectedSubstructure;
            if (sub != null && sub.Count > 0)
            {
                return sub.Contains(cell);
            }
            // VGE: scaffold / damaged substructure terrains still count as ship deck
            // while engine cell sets are rebuilding after land.
            if (StrataVgeCompat.Active && StrataVgeCompat.CellHasShipFoundation(map, cell))
            {
                return true;
            }
            // Substructure is empty while Odyssey wires the ship up after
            // ArriveNewMap; IsOnboardGravship NREs in that window.
            return false;
        }

        // Colony dig/tower stairs and elevators must stay off the ship footprint —
        // gravship stairwells are the only portals that mark a stack for launch follow.
        public static AcceptanceReport RejectColonyPortalOnGravship(
            BuildableDef def,
            IntVec3 center,
            Rot4 rot,
            Map map)
        {
            if (!OdysseyActive || map == null || FindGravEngineOnMap(map) == null)
            {
                return true;
            }
            ThingDef thingDef = def as ThingDef;
            IntVec2 size = thingDef?.size ?? IntVec2.One;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, size))
            {
                if (CellOnGravship(map, cell))
                {
                    return "Strata_Place_NoColonyStairsOnGravship".Translate();
                }
            }
            return true;
        }

        public static bool IsGravshipPortal(Thing thing)
        {
            return thing is IStrataGravshipPortal;
        }

        // Host shaft on the ship deck (not a pocket landing).
        public static bool IsGravshipHostShaft(Thing thing)
        {
            return thing is IStrataGravshipPortal && thing is MapPortal && thing is not PocketMapExit;
        }

        public static bool IsGravshipTowerShaft(Thing thing)
        {
            return thing is Building_StairsBuildUp;
        }

        public static bool IsGravshipLanding(Thing thing)
        {
            return thing is IStrataGravshipPortal && thing is PocketMapExit;
        }

        // Shared underdeck / upper deck for any gravship shaft on this host.
        public static Map ExistingGravshipLevelBelow(Map host, Thing self)
        {
            if (host == null)
            {
                return null;
            }
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == self || !IsGravshipHostShaft(thing) || IsGravshipTowerShaft(thing))
                {
                    continue;
                }
                if (thing is MapPortal portal && portal.Spawned && portal.PocketMapExists)
                {
                    return portal.PocketMap;
                }
            }
            // Land race: travelling underdeck still exists but no shaft holds
            // PocketMapExists — adopt it instead of spawning a second empty floor.
            return StrataGravshipOrphanLevels.FindAdoptableUnderdeck(host);
        }

        public static Map ExistingGravshipLevelAbove(Map host, Thing self)
        {
            if (host == null)
            {
                return null;
            }
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == self || !IsGravshipHostShaft(thing) || !IsGravshipTowerShaft(thing))
                {
                    continue;
                }
                if (thing is MapPortal portal && portal.Spawned && portal.PocketMapExists)
                {
                    return portal.PocketMap;
                }
            }
            return StrataGravshipOrphanLevels.FindAdoptableUpperDeck(host);
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
            return FindDirectGravshipPortalHost(map) != null;
        }

        // Surface map that owns this gravship-linked floor (engine map), or null.
        public static Map FindGravshipStackRoot(Map map)
        {
            if (!OdysseyActive || map == null)
            {
                return null;
            }
            if (IsGravshipHostMap(map))
            {
                return map;
            }
            Map portalHost = FindDirectGravshipPortalHost(map);
            if (portalHost != null)
            {
                return ResolveGravshipHost(portalHost);
            }
            Map current = map;
            int guard = 0;
            while (current?.Parent is PocketMapParent pocket && pocket.sourceMap != null && guard++ < 64)
            {
                current = pocket.sourceMap;
                portalHost = FindDirectGravshipPortalHost(current);
                if (portalHost != null)
                {
                    return ResolveGravshipHost(portalHost);
                }
            }
            return null;
        }

        public static bool IsInGravshipStack(Map map)
        {
            return FindGravshipStackRoot(map) != null;
        }

        // Map that holds the gravship shaft opening this pocket, if any.
        public static Map FindDirectGravshipPortalHost(Map level)
        {
            if (!OdysseyActive || level == null
                || (!StrataMapUtility.IsUnderground(level) && !StrataMapUtility.IsUpperLevel(level)))
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map host = maps[i];
                if (host == level)
                {
                    continue;
                }
                foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                        || !portal.Spawned || !portal.PocketMapExists || portal.PocketMap != level)
                    {
                        continue;
                    }
                    return host;
                }
            }
            return null;
        }

        private static Map ResolveGravshipHost(Map portalHostMap)
        {
            Map current = portalHostMap;
            int guard = 0;
            while (current != null && guard++ < 64)
            {
                if (IsGravshipHostMap(current))
                {
                    return current;
                }
                if (current.Parent is PocketMapParent pocket && pocket.sourceMap != null)
                {
                    current = pocket.sourceMap;
                    continue;
                }
                break;
            }
            return IsGravshipHostMap(portalHostMap) ? portalHostMap : null;
        }

        // True for the ship host map, or a Strata level tied to gravship stairs.
        public static bool IsGravshipContext(Map map)
        {
            return IsGravshipHostMap(map) || IsInGravshipStack(map);
        }

        public static bool EngineHasSubstructure(Building_GravEngine engine)
        {
            if (engine == null)
            {
                return false;
            }
            HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub != null && sub.Count > 0)
            {
                return true;
            }
            sub = engine.AllConnectedSubstructure;
            return sub != null && sub.Count > 0;
        }

        // Always replace vanilla IsOnboardGravship*: it NREs on
        // cell.GetRoom(engine.Map).PsychologicallyOutdoors when Room is null
        // (underdeck cells, mid-land maps) and on ValidSubstructure.Contains when
        // that set is null but AllConnectedSubstructure is not.
        public static bool AllowVanillaOnboardCheck(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            return AllowVanillaOnboardCheck(
                cell, engine, pawn: null, desperate: false, respectAllowedAreas: true, ref __result);
        }

        public static bool AllowVanillaOnboardCheck(
            IntVec3 cell,
            Building_GravEngine engine,
            Pawn pawn,
            bool desperate,
            bool respectAllowedAreas,
            ref bool __result)
        {
            if (!OdysseyActive)
            {
                return true;
            }
            __result = SafeIsOnboardGravship(cell, engine, pawn, desperate, respectAllowedAreas);
            return false;
        }

        public static bool SafeIsOnboardGravship(
            IntVec3 cell,
            Building_GravEngine engine,
            Pawn pawn = null,
            bool desperate = false,
            bool respectAllowedAreas = true)
        {
            if (engine == null || engine.Destroyed || !engine.Spawned || engine.Map == null)
            {
                return false;
            }
            if (respectAllowedAreas && pawn != null)
            {
                Map pawnMap = pawn.MapHeld;
                if (pawnMap != null && cell.InBounds(pawnMap) && !cell.InAllowedArea(pawn))
                {
                    return false;
                }
            }

            // Linked underdeck / tower deck cells (launch ritual reach from below).
            if (TryStrataOnboardCell(cell, engine))
            {
                return true;
            }

            Map host = engine.Map;
            if (!cell.InBounds(host))
            {
                return false;
            }
            if (!desperate)
            {
                Room room = cell.GetRoom(host);
                if (room == null || room.PsychologicallyOutdoors)
                {
                    return false;
                }
            }

            HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub != null && sub.Count > 0)
            {
                return sub.Contains(cell);
            }
            sub = engine.AllConnectedSubstructure;
            if (sub != null && sub.Count > 0)
            {
                return sub.Contains(cell);
            }
            // VGE scaffold / damaged foundation while ValidSubstructure is rebuilding.
            return StrataVgeCompat.Active && StrataVgeCompat.CellHasShipFoundation(host, cell);
        }

        public static void ApplyOnboardPostfix(IntVec3 cell, Building_GravEngine engine, ref bool __result)
        {
            // Prefix fully replaces vanilla — Postfix is a no-op keep for Harmony wiring.
        }

        private static bool TryStrataOnboardCell(IntVec3 cell, Building_GravEngine engine)
        {
            Map host = engine.Map;
            if (host != null && cell.InBounds(host) && IsLinkedDeckCell(host, cell, engine))
            {
                return true;
            }
            foreach (Map map in EnumerateStackDeckMaps(engine))
            {
                if (map == host || !cell.InBounds(map))
                {
                    continue;
                }
                if (IsLinkedDeckCell(map, cell, engine))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLinkedDeckCell(Map map, IntVec3 cell, Building_GravEngine engine)
        {
            if (map == null || engine == null || engine.Map == null || !cell.InBounds(map))
            {
                return false;
            }
            if (map == engine.Map)
            {
                return CellOnGravship(map, cell);
            }
            if (!IsGravshipLinkedLevel(map) || FindGravshipStackRoot(map) != engine.Map)
            {
                return false;
            }
            if (StrataGravshipSubstructureSync.HasSubstructure(map, cell))
            {
                return true;
            }
            var tracker = map.GetComponent<MapComponent_StrataProjectedSubstructure>();
            if (tracker != null && tracker.IsProjected(cell))
            {
                return true;
            }
            // G6: wired host shaft projects vertical ownership onto the pocket cell
            // at the same absolute coords (shaft as grav-extender).
            if (CellOwnedByWiredHostShaft(map, cell, engine.Map))
            {
                return true;
            }
            if (StrataMapUtility.IsUpperLevel(map))
            {
                return cell.GetTerrain(map)?.defName == UpperDeckUtility.RoofDeckDefName;
            }
            return GravshipDeckUtility.IsWalkableDeckCell(map, cell);
        }

        private static bool CellOwnedByWiredHostShaft(Map pocket, IntVec3 cell, Map host)
        {
            if (host == null || pocket.Size != host.Size || !cell.InBounds(host))
            {
                return false;
            }
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal shaft || !shaft.Spawned
                    || !IsGravshipHostShaft(shaft)
                    || !shaft.PocketMapExists || shaft.PocketMap != pocket)
                {
                    continue;
                }
                // Shaft footprint + 1-cell rim owns the vertical column on the pocket.
                CellRect rect = shaft.OccupiedRect().ExpandedBy(1);
                if (rect.Contains(cell))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<Map> EnumerateStackDeckMaps(Building_GravEngine engine)
        {
            if (engine?.Map == null)
            {
                yield break;
            }
            var seen = new HashSet<Map>();
            if (seen.Add(engine.Map))
            {
                yield return engine.Map;
            }
            foreach (Map level in CollectGravshipLinkedLevels(engine.Map, seen))
            {
                yield return level;
            }
        }

        // Portal-linked gravship decks only — no level-graph walk (safe during UI / region BFS).
        private static IEnumerable<Map> CollectGravshipLinkedLevels(Map host, HashSet<Map> seen)
        {
            if (host?.listerThings == null)
            {
                yield break;
            }
            var pending = new List<Map>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                Map pocket = portal.PocketMap;
                if (pocket != null && seen.Add(pocket))
                {
                    pending.Add(pocket);
                    yield return pocket;
                }
            }
            for (int i = 0; i < pending.Count; i++)
            {
                foreach (Map nested in CollectGravshipChildLevels(pending[i], seen))
                {
                    yield return nested;
                }
            }
        }

        private static IEnumerable<Map> CollectGravshipChildLevels(Map map, HashSet<Map> seen)
        {
            if (map?.ChildPocketMaps == null)
            {
                yield break;
            }
            foreach (Map child in map.ChildPocketMaps)
            {
                if (child == null || !IsGravshipLinkedLevel(child) || !seen.Add(child))
                {
                    continue;
                }
                yield return child;
                foreach (Map nested in CollectGravshipChildLevels(child, seen))
                {
                    yield return nested;
                }
            }
        }
    }
}
