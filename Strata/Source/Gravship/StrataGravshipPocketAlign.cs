using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Strata
{
    // Keeps gravship-linked pocket maps 1:1 with the ship when it moves.
    // Primary: snap each pocket's landing under its host shaft (self-heals drift).
    // Fallback: shift by grav-engine takeoff→land delta.
    public static class StrataGravshipPocketAlign
    {
        // Force the pocket landing (or content centroid) under the host shaft /
        // footprint so footprint rebuild does not leave an empty silhouette island
        // next to the furnished travelling room.
        public static int SnapContentsToHostFootprint(Map pocket, Map host, HashSet<IntVec3> hostDeckCells)
        {
            if (pocket == null || host == null)
            {
                return 0;
            }
            var hostShafts = CollectHostShafts(host);
            MapPortal landing = FindPocketLanding(pocket);
            MapPortal shaft = FindMatchingHostShaft(hostShafts, pocket, landing);
            IntVec3 contentAnchor = landing != null && landing.Spawned
                ? landing.Position
                : ContentCentroid(pocket);
            if (!contentAnchor.IsValid)
            {
                return 0;
            }

            IntVec3 target = IntVec3.Invalid;
            if (shaft != null && shaft.Spawned)
            {
                target = shaft.Position;
            }
            else if (hostDeckCells != null && hostDeckCells.Count > 0)
            {
                target = NearestCell(contentAnchor, hostDeckCells);
            }
            if (!target.IsValid)
            {
                return 0;
            }

            IntVec3 delta = target - contentAnchor;
            if (delta == IntVec3.Zero)
            {
                return 0;
            }
            int moved = TranslateMapContents(pocket, delta);
            if (moved > 0)
            {
                Log.Message("[Strata] Gravship land: snapped " + moved
                    + " thing(s) on " + pocket.uniqueID + " by " + delta
                    + " so contents sit on the ship footprint.");
            }
            return moved;
        }

        public static void AlignPocketsToLandedShip(
            List<Map> pockets,
            Map newHost,
            IntVec3 takeoffEnginePos)
        {
            if (pockets == null || newHost == null)
            {
                return;
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(newHost);
            if (engine == null || !engine.Spawned)
            {
                return;
            }

            var hostShafts = CollectHostShafts(newHost);
            IntVec3 engineDelta = takeoffEnginePos.IsValid
                ? engine.Position - takeoffEnginePos
                : IntVec3.Zero;

            int shifted = 0;
            for (int i = 0; i < pockets.Count; i++)
            {
                Map pocket = pockets[i];
                if (pocket == null || !Find.Maps.Contains(pocket))
                {
                    continue;
                }

                IntVec3 delta = ComputeSnapDelta(pocket, hostShafts, engine, engineDelta);
                if (delta == IntVec3.Zero)
                {
                    continue;
                }

                shifted += TranslateMapContents(pocket, delta);
            }

            RepaintAll(pockets, newHost, engine);
            StrataGravshipPortalTravel.RewireHostShafts(newHost, pockets);

            Log.Message("[Strata] Gravship land align: engineDelta " + engineDelta
                + ", moved " + shifted + " thing(s) on "
                + pockets.Count + " linked level(s).");
        }

        private static IntVec3 ComputeSnapDelta(
            Map pocket,
            List<MapPortal> hostShafts,
            Building_GravEngine engine,
            IntVec3 engineDelta)
        {
            MapPortal landing = FindPocketLanding(pocket);
            MapPortal shaft = FindMatchingHostShaft(hostShafts, pocket, landing);
            if (landing != null && shaft != null && landing.Spawned && shaft.Spawned)
            {
                // Exact 1:1: move pocket so the landing sits under the ship shaft.
                IntVec3 snap = shaft.Position - landing.Position;
                if (snap != IntVec3.Zero)
                {
                    Log.Message("[Strata] Gravship land align: shaft-snap "
                        + landing.LabelCap + " " + landing.Position
                        + " -> " + shaft.Position + " (delta " + snap + ")");
                }
                return snap;
            }

            // No shaft pair yet — fall back to whole-ship engine movement.
            return engineDelta;
        }

        private static List<MapPortal> CollectHostShafts(Map host)
        {
            var list = new List<MapPortal>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && portal.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    list.Add(portal);
                }
            }
            return list;
        }

        private static MapPortal FindPocketLanding(Map pocket)
        {
            foreach (Thing thing in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (StrataGravshipUtility.IsGravshipLanding(thing) && thing is MapPortal portal)
                {
                    return portal;
                }
            }
            foreach (Thing thing in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is PocketMapExit exit && exit.Spawned)
                {
                    return exit;
                }
            }
            return null;
        }

        private static MapPortal FindMatchingHostShaft(
            List<MapPortal> hostShafts,
            Map pocket,
            MapPortal landing)
        {
            if (hostShafts == null || hostShafts.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < hostShafts.Count; i++)
            {
                MapPortal shaft = hostShafts[i];
                if (shaft.PocketMapExists && shaft.PocketMap == pocket)
                {
                    return shaft;
                }
                if (landing is PocketMapExit exit && exit.entrance == shaft)
                {
                    return shaft;
                }
            }

            bool wantTower = landing is Building_BuildUpLanding
                || StrataMapUtility.IsUpperLevel(pocket);
            for (int i = 0; i < hostShafts.Count; i++)
            {
                if (StrataGravshipUtility.IsGravshipTowerShaft(hostShafts[i]) == wantTower)
                {
                    return hostShafts[i];
                }
            }

            return hostShafts[0];
        }

        private static void RepaintAll(List<Map> pockets, Map host, Building_GravEngine engine)
        {
            if (!StrataGravshipUtility.EngineHasSubstructure(engine))
            {
                Log.Message("[Strata] Gravship land: host substructure not ready yet — "
                    + "keeping linked deck terrain/projected tiles; sync will retry shortly.");
                MapComponent_StrataGravshipUpperDeckSync.RequestSync(host);
                ScheduleDeferredRepaint(pockets, host);
                return;
            }

            for (int i = 0; i < pockets.Count; i++)
            {
                Map pocket = pockets[i];
                if (pocket == null || !Find.Maps.Contains(pocket))
                {
                    continue;
                }
                if (StrataMapUtility.IsUpperLevel(pocket))
                {
                    UpperDeckUtility.SyncAllFromSource(pocket);
                }
                else if (StrataMapUtility.IsUnderground(pocket))
                {
                    GravshipDeckUtility.PaintSubstructureFootprint(
                        pocket,
                        host,
                        GravshipDeckUtility.DeckTerrain,
                        GravshipDeckUtility.HullTerrain,
                        ceilingOnDeck: true,
                        preserveExistingDeck: true);
                    GravshipDeckUtility.RestoreDeckUnderBuildings(pocket);
                }
                StrataGravshipSubstructureSync.SyncMap(pocket, host, engine);
            }
        }

        private static void ScheduleDeferredRepaint(List<Map> pockets, Map host)
        {
            if (pockets == null || host == null)
            {
                return;
            }
            var pocketIds = new List<int>(pockets.Count);
            for (int i = 0; i < pockets.Count; i++)
            {
                if (pockets[i] != null)
                {
                    pocketIds.Add(pockets[i].uniqueID);
                }
            }
            int hostId = host.uniqueID;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Map landedHost = Find.Maps.Find(m => m != null && m.uniqueID == hostId);
                if (landedHost == null)
                {
                    return;
                }
                Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(landedHost);
                if (!StrataGravshipUtility.EngineHasSubstructure(engine))
                {
                    MapComponent_StrataGravshipUpperDeckSync.RequestSync(landedHost);
                    return;
                }
                var ready = new List<Map>();
                for (int i = 0; i < pocketIds.Count; i++)
                {
                    Map pocket = Find.Maps.Find(m => m != null && m.uniqueID == pocketIds[i]);
                    if (pocket != null)
                    {
                        ready.Add(pocket);
                    }
                }
                // Re-snap under shafts now that the host footprint exists, then
                // grow-only repaint (Align → RepaintAll).
                AlignPocketsToLandedShip(ready, landedHost, IntVec3.Invalid);
            });
        }

        private static IntVec3 ContentCentroid(Map map)
        {
            long sx = 0;
            long sz = 0;
            int n = 0;
            List<Thing> things = map.listerThings?.AllThings;
            if (things == null)
            {
                return IntVec3.Invalid;
            }
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                if (thing.def.category != ThingCategory.Building
                    && thing.def.category != ThingCategory.Item
                    && thing is not Pawn)
                {
                    continue;
                }
                sx += thing.Position.x;
                sz += thing.Position.z;
                n++;
            }
            if (n == 0)
            {
                return IntVec3.Invalid;
            }
            return new IntVec3((int)(sx / n), 0, (int)(sz / n));
        }

        private static IntVec3 NearestCell(IntVec3 from, HashSet<IntVec3> cells)
        {
            IntVec3 best = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            foreach (IntVec3 cell in cells)
            {
                int dist = (cell - from).LengthHorizontalSquared;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = cell;
                }
            }
            return best;
        }

        private static int TranslateMapContents(Map map, IntVec3 delta)
        {
            if (map == null || delta == IntVec3.Zero)
            {
                return 0;
            }

            TranslateZones(map, delta);
            TranslateDesignations(map, delta);

            List<Thing> all = map.listerThings.AllThings.ToList();
            var moves = new List<(Thing thing, IntVec3 dest, Rot4 rot)>();
            var seen = new HashSet<Thing>();

            for (int i = 0; i < all.Count; i++)
            {
                Thing thing = all[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || !seen.Add(thing))
                {
                    continue;
                }
                if (thing.def.category == ThingCategory.Ethereal && thing is not Blueprint && thing is not Frame)
                {
                    continue;
                }

                moves.Add((thing, thing.Position + delta, thing.Rotation));
            }

            moves.Sort((a, b) =>
                (b.thing.def.size.x * b.thing.def.size.z).CompareTo(a.thing.def.size.x * a.thing.def.size.z));

            for (int i = 0; i < moves.Count; i++)
            {
                Thing thing = moves[i].thing;
                if (thing.Spawned)
                {
                    thing.DeSpawn(DestroyMode.WillReplace);
                }
            }

            int placed = 0;
            moves.Sort((a, b) => SpawnOrder(a.thing).CompareTo(SpawnOrder(b.thing)));

            for (int i = 0; i < moves.Count; i++)
            {
                (Thing thing, IntVec3 dest, Rot4 rot) = moves[i];
                if (thing.Destroyed || thing.Spawned)
                {
                    continue;
                }

                if (!TrySpawnAt(thing, dest, map, rot)
                    && !TrySpawnNear(thing, dest, map, rot)
                    && !TrySpawnNear(thing, map.Center, map, rot))
                {
                    Log.Warning("[Strata] Gravship align: could not place "
                        + thing.LabelCap + " after shift " + delta);
                    continue;
                }
                placed++;
            }

            return placed;
        }

        private static int SpawnOrder(Thing thing)
        {
            if (thing is Pawn)
            {
                return 3;
            }
            if (thing.def?.defName == StrataGravshipSubstructureSync.SubstructureDefName)
            {
                return 0;
            }
            if (thing.def?.category == ThingCategory.Item)
            {
                return 2;
            }
            return 1;
        }

        private static bool TrySpawnAt(Thing thing, IntVec3 dest, Map map, Rot4 rot)
        {
            if (!dest.InBounds(map) || !CanAccept(thing, dest, map, rot))
            {
                return false;
            }
            return GenSpawn.Spawn(thing, dest, map, rot) != null;
        }

        private static bool TrySpawnNear(Thing thing, IntVec3 dest, Map map, Rot4 rot)
        {
            if (CellFinder.TryFindRandomCellNear(
                    dest.ClampInsideMap(map),
                    map,
                    12,
                    c => CanAccept(thing, c, map, rot),
                    out IntVec3 near))
            {
                return GenSpawn.Spawn(thing, near, map, rot) != null;
            }
            return false;
        }

        private static bool CanAccept(Thing thing, IntVec3 cell, Map map, Rot4 rot)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            if (thing is Pawn)
            {
                return cell.Standable(map);
            }
            return GenAdj.OccupiedRect(cell, rot, thing.def.Size).InBounds(map);
        }

        private static void TranslateZones(Map map, IntVec3 delta)
        {
            if (map.zoneManager == null)
            {
                return;
            }
            List<Zone> zones = map.zoneManager.AllZones.ToList();
            for (int i = 0; i < zones.Count; i++)
            {
                Zone zone = zones[i];
                if (zone == null)
                {
                    continue;
                }
                List<IntVec3> cells = zone.Cells.ToList();
                for (int c = 0; c < cells.Count; c++)
                {
                    zone.RemoveCell(cells[c]);
                }
                for (int c = 0; c < cells.Count; c++)
                {
                    IntVec3 next = cells[c] + delta;
                    if (next.InBounds(map))
                    {
                        zone.AddCell(next);
                    }
                }
            }
        }

        private static void TranslateDesignations(Map map, IntVec3 delta)
        {
            if (map.designationManager == null)
            {
                return;
            }
            List<Designation> all = map.designationManager.AllDesignations.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                Designation des = all[i];
                if (des == null || des.target.HasThing)
                {
                    continue;
                }
                IntVec3 cell = des.target.Cell;
                if (!cell.IsValid)
                {
                    continue;
                }
                DesignationDef def = des.def;
                map.designationManager.RemoveDesignation(des);
                IntVec3 next = cell + delta;
                if (next.InBounds(map))
                {
                    map.designationManager.AddDesignation(new Designation(next, def));
                }
            }
        }
    }
}
