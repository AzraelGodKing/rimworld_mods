using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Odyssey packing scans ValidSubstructure cell-by-cell; 2x2 shafts can miss
    // a cell check. Force dedicated gravship portals onto the packed ship.
    [HarmonyPatch(typeof(Gravship), "ShouldBringOnGravship")]
    public static class Patch_Gravship_ShouldBringPortal
    {
        public static void Postfix(Thing thing, IntVec3 cell, ref bool __result)
        {
            if (__result || thing == null)
            {
                return;
            }

            if (thing is IStrataGravshipPortal
                && thing.def.bringAlongOnGravship
                && thing.Spawned
                && StrataGravshipUtility.IsGravshipPortal(thing))
            {
                __result = true;
            }
        }
    }

    // AddThing also requires OnValidSubstructure for every occupied cell — a 2x2
    // shaft with one fringe cell off Valid (or excess substructure) is skipped
    // and left on a GravAnchor-kept map. Force host shafts through.
    [HarmonyPatch(typeof(Gravship), "AddThing")]
    public static class Patch_Gravship_AddThingPortal
    {
        public static bool Prefix(Gravship __instance, Thing thing, IntVec3 offset)
        {
            if (thing == null
                || !StrataGravshipUtility.IsGravshipHostShaft(thing)
                || !thing.def.bringAlongOnGravship)
            {
                return true;
            }

            return !StrataGravshipPortalTravel.TryForcePackHostShaft(__instance, thing, offset);
        }
    }

    // Snapshots of host shafts taken at InitiateTakeoff so land can reconnect
    // or respawn them if packing still drops a portal.
    public static class StrataGravshipPortalTravel
    {
        public class PortalSnapshot : IExposable
        {
            public string defName;
            public IntVec3 offsetFromEngine;
            public Rot4 rotation;
            public int pocketMapId;
            public bool isTower;
            // Takeoff engine facing; Invalid = legacy save (skip rotation delta).
            public Rot4 engineRotationAtTakeoff = Rot4.Invalid;

            public void ExposeData()
            {
                Scribe_Values.Look(ref defName, "defName");
                Scribe_Values.Look(ref offsetFromEngine, "offsetFromEngine", IntVec3.Invalid);
                Scribe_Values.Look(ref rotation, "rotation", Rot4.North);
                Scribe_Values.Look(ref pocketMapId, "pocketMapId", -1);
                Scribe_Values.Look(ref isTower, "isTower", false);
                Scribe_Values.Look(ref engineRotationAtTakeoff, "engineRotationAtTakeoff", Rot4.Invalid);
            }
        }

        private static readonly List<PortalSnapshot> snapshots = new List<PortalSnapshot>();

        public static void ResetSession()
        {
            snapshots.Clear();
        }

        public static IReadOnlyList<PortalSnapshot> PeekSnapshots() => snapshots;

        // Pack a host shaft into the Gravship.Things dictionary even when
        // OnValidSubstructure would reject it (GravAnchor leftovers).
        public static bool TryForcePackHostShaft(Gravship ship, Thing thing, IntVec3 offset)
        {
            if (ship == null || thing == null || !StrataGravshipUtility.IsGravshipHostShaft(thing))
            {
                return false;
            }

            var things = AccessTools.Field(typeof(Gravship), "things")
                .GetValue(ship) as Dictionary<Thing, PositionData>;
            if (things == null)
            {
                return false;
            }

            if (things.ContainsKey(thing))
            {
                return true;
            }

            things.Add(thing, new PositionData(offset, thing.Rotation));

            if (thing.TryGetComp(out CompPowerTrader power))
            {
                var powerOn = AccessTools.Field(typeof(Gravship), "powerOn")
                    .GetValue(ship) as Dictionary<Thing, bool>;
                if (powerOn != null && !powerOn.ContainsKey(thing))
                {
                    powerOn.Add(thing, power.PowerOn);
                }
            }

            return true;
        }

        // After Odyssey GenerateGravship: any host shaft still sitting on the
        // launch map (visible under GravAnchor) is forced onto the ship and
        // despawned like vanilla packed buildings.
        public static void SweepLeftBehindHostShafts(Gravship ship, Building_GravEngine engine)
        {
            if (ship == null || engine?.Map == null)
            {
                return;
            }

            Map map = engine.Map;
            var remaining = new List<Thing>();
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing != null && thing.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(thing)
                    && thing.def.bringAlongOnGravship)
                {
                    remaining.Add(thing);
                }
            }

            if (remaining.Count == 0)
            {
                return;
            }

            int swept = 0;
            for (int i = 0; i < remaining.Count; i++)
            {
                Thing shaft = remaining[i];
                if (shaft == null || shaft.Destroyed)
                {
                    continue;
                }

                IntVec3 offset = shaft.Position - engine.Position;
                TryForcePackHostShaft(ship, shaft, offset);

                if (shaft.Spawned)
                {
                    shaft.PreSwapMap();
                    shaft.DeSpawn(DestroyMode.WillReplace);
                    swept++;
                }
            }

            if (swept > 0)
            {
                Log.Message("[Strata] Gravship takeoff: swept " + swept
                    + " left-behind host shaft(s) off launch map (GravAnchor-safe).");
            }
        }

        public static void SnapshotHostPortals(Building_GravEngine engine)
        {
            snapshots.Clear();
            if (engine?.Map == null)
            {
                return;
            }

            Map host = engine.Map;
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned
                    || portal is not IStrataGravshipPortal)
                {
                    continue;
                }

                // Host shafts only — landings live on pocket maps.
                if (!StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    continue;
                }

                int pocketId = portal.PocketMapExists ? portal.PocketMap.uniqueID : -1;
                snapshots.Add(new PortalSnapshot
                {
                    defName = portal.def.defName,
                    offsetFromEngine = portal.Position - engine.Position,
                    rotation = portal.Rotation,
                    pocketMapId = pocketId,
                    isTower = StrataGravshipUtility.IsGravshipTowerShaft(portal),
                    engineRotationAtTakeoff = engine.Rotation,
                });
            }

            if (snapshots.Count > 0)
            {
                Log.Message("[Strata] Gravship takeoff: snapshot "
                    + snapshots.Count + " host shaft(s) for land reconnect.");
            }
        }

        public static List<PortalSnapshot> TakeSnapshotsForSave()
        {
            return new List<PortalSnapshot>(snapshots);
        }

        public static void RestoreSnapshotsFromSave(List<PortalSnapshot> saved)
        {
            snapshots.Clear();
            if (saved == null)
            {
                return;
            }

            snapshots.AddRange(saved);
        }

        // After pockets rebind to the landed host: reconnect exits, or respawn
        // missing shafts from takeoff snapshots.
        // restoreMissingShafts: false while the packed ship is still spawning
        // (PostSwapMap) so we do not place temporary shafts that get replaced
        // and drop the pocketMap link.
        public static void ReconnectOrRestore(
            Map hostMap,
            List<Map> pockets,
            bool restoreMissingShafts = true)
        {
            if (hostMap == null)
            {
                return;
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(hostMap);
            if (engine == null)
            {
                return;
            }

            if (restoreMissingShafts)
            {
                EnsureHostShafts(hostMap, engine);
            }
            WirePocketsToHostShafts(hostMap, pockets ?? CollectPocketsOnHost(hostMap));
            if (restoreMissingShafts)
            {
                snapshots.Clear();
            }
        }

        // Re-wire after pocket contents have been shifted under the host shafts.
        public static void RewireHostShafts(Map hostMap, List<Map> pockets)
        {
            if (hostMap == null)
            {
                return;
            }
            WirePocketsToHostShafts(hostMap, pockets ?? CollectPocketsOnHost(hostMap));
        }

        private static List<Map> CollectPocketsOnHost(Map host)
        {
            var list = new List<Map>();
            if (host.ChildPocketMaps == null)
            {
                return list;
            }

            foreach (Map child in host.ChildPocketMaps)
            {
                if (child != null && StrataGravshipStackUtility.IsStrataLinkedLevel(child))
                {
                    list.Add(child);
                }
            }

            return list;
        }

        private static void EnsureHostShafts(Map host, Building_GravEngine engine)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                PortalSnapshot snap = snapshots[i];
                if (FindHostShaftMatching(host, engine, snap) != null)
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(snap.defName);
                if (def == null)
                {
                    Log.Warning("[Strata] Gravship land: missing shaft def " + snap.defName);
                    continue;
                }

                IntVec3 cell = engine.Position + snap.offsetFromEngine;
                if (!cell.InBounds(host))
                {
                    cell = engine.Position;
                }

                if (!CanPlaceShaft(host, def, cell, snap.rotation))
                {
                    if (!CellFinder.TryFindRandomCellNear(
                            engine.Position,
                            host,
                            8,
                            c => CanPlaceShaft(host, def, c, snap.rotation),
                            out cell))
                    {
                        Log.Warning("[Strata] Gravship land: could not place restored shaft "
                            + snap.defName);
                        continue;
                    }
                }

                Thing shaft = ThingMaker.MakeThing(def);
                GenSpawn.Spawn(shaft, cell, host, snap.rotation);
                Log.Message("[Strata] Gravship land: restored missing shaft "
                    + def.defName + " at " + cell);
            }
        }

        private static bool CanPlaceShaft(Map map, ThingDef def, IntVec3 cell, Rot4 rot)
        {
            return cell.InBounds(map)
                && GenConstruct.CanPlaceBlueprintAt(def, cell, rot, map).Accepted;
        }

        private static IntVec3 OffsetForLandedEngine(
            Building_GravEngine engine,
            PortalSnapshot snap)
        {
            IntVec3 offset = snap.offsetFromEngine;
            // Legacy snapshots lack takeoff engine facing — keep un-rotated match.
            if (!snap.engineRotationAtTakeoff.IsValid || engine == null)
            {
                return offset;
            }

            int delta = (engine.Rotation.AsInt - snap.engineRotationAtTakeoff.AsInt + 4) % 4;
            if (delta != 0)
            {
                offset = offset.RotatedBy(new Rot4(delta));
            }

            return offset;
        }

        private static MapPortal FindHostShaftMatching(
            Map host,
            Building_GravEngine engine,
            PortalSnapshot snap,
            HashSet<MapPortal> claimedShafts = null)
        {
            IntVec3 expected = engine.Position + OffsetForLandedEngine(engine, snap);
            MapPortal byDef = null;
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned
                    || portal.def.defName != snap.defName
                    || portal is not IStrataGravshipPortal)
                {
                    continue;
                }

                if (claimedShafts != null && claimedShafts.Contains(portal))
                {
                    continue;
                }

                if ((portal.Position - expected).LengthManhattan <= 2)
                {
                    return portal;
                }

                byDef ??= portal;
            }

            return byDef;
        }

        private static void WirePocketsToHostShafts(Map host, List<Map> pockets)
        {
            if (pockets == null)
            {
                return;
            }

            var hostShafts = new List<MapPortal>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && portal.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    hostShafts.Add(portal);
                }
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
            var wiredPockets = new HashSet<Map>();
            var claimedShafts = new HashSet<MapPortal>();

            // Prefer takeoff pocketMapId so the furnished travelling floor wins
            // over a freshly generated empty underdeck.
            if (engine != null)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    PortalSnapshot snap = snapshots[i];
                    Map pocket = StrataGravshipOrphanLevels.FindMapById(snap.pocketMapId);
                    if (pocket == null || !Find.Maps.Contains(pocket))
                    {
                        continue;
                    }
                    MapPortal shaft = FindHostShaftMatching(host, engine, snap, claimedShafts);
                    MapPortal landing = FindLandingOn(pocket);
                    if (shaft == null || landing == null)
                    {
                        continue;
                    }
                    ConnectPortalPair(shaft, landing);
                    claimedShafts.Add(shaft);
                    wiredPockets.Add(pocket);
                }
            }

            for (int i = 0; i < pockets.Count; i++)
            {
                Map pocket = pockets[i];
                if (pocket == null || !Find.Maps.Contains(pocket) || wiredPockets.Contains(pocket))
                {
                    continue;
                }

                MapPortal landing = FindLandingOn(pocket);
                if (landing == null)
                {
                    continue;
                }

                MapPortal shaft = FindBestShaftForPocket(hostShafts, pocket, landing, claimedShafts);
                if (shaft == null)
                {
                    Log.Message("[Strata] Gravship land: left pocket unwired (no unclaimed matching shaft) "
                        + pocket);
                    continue;
                }

                ConnectPortalPair(shaft, landing);
                claimedShafts.Add(shaft);
            }
        }

        private static MapPortal FindLandingOn(Map pocket)
        {
            foreach (Thing thing in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (StrataGravshipUtility.IsGravshipLanding(thing))
                {
                    return (MapPortal)thing;
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

        private static MapPortal FindBestShaftForPocket(
            List<MapPortal> hostShafts,
            Map pocket,
            MapPortal landing,
            HashSet<MapPortal> claimedShafts)
        {
            // Prefer shaft already linked to this pocket, then matching direction.
            for (int i = 0; i < hostShafts.Count; i++)
            {
                MapPortal shaft = hostShafts[i];
                if (claimedShafts != null && claimedShafts.Contains(shaft))
                {
                    continue;
                }

                if (shaft.PocketMapExists && shaft.PocketMap == pocket)
                {
                    return shaft;
                }
            }

            bool wantTower = StrataGravshipUtility.IsGravshipTowerShaft(landing)
                || landing is Building_BuildUpLanding
                || StrataMapUtility.IsUpperLevel(pocket);
            for (int i = 0; i < hostShafts.Count; i++)
            {
                MapPortal shaft = hostShafts[i];
                if (claimedShafts != null && claimedShafts.Contains(shaft))
                {
                    continue;
                }

                if (StrataGravshipUtility.IsGravshipTowerShaft(shaft) == wantTower)
                {
                    return shaft;
                }
            }

            // No cross-type last resort — leave unwired for EnsureHostShafts.
            return null;
        }

        private static void ConnectPortalPair(MapPortal hostShaft, MapPortal landing)
        {
            if (hostShaft == null || landing == null)
            {
                return;
            }

            // MapPortal.exit / PocketMapExit.entrance are the bidirectional link.
            if (landing is PocketMapExit exitLanding)
            {
                if (exitLanding.entrance == hostShaft && hostShaft.exit == exitLanding)
                {
                    return;
                }

                // Drop the old landing's entrance so rewiring leaves no one-way link.
                if (hostShaft.exit != null && hostShaft.exit != exitLanding
                    && hostShaft.exit.entrance == hostShaft)
                {
                    hostShaft.exit.entrance = null;
                }

                exitLanding.entrance = hostShaft;
                hostShaft.exit = exitLanding;
            }

            if (landing.Map != null)
            {
                AccessTools.Field(typeof(MapPortal), "pocketMap").SetValue(hostShaft, landing.Map);
            }

            // Ensure pocket parenting points at the shaft's host map.
            if (landing.Map?.Parent is PocketMapParent parent && hostShaft.Map != null)
            {
                parent.sourceMap = hostShaft.Map;
            }

            Log.Message("[Strata] Gravship land: wired "
                + landing.LabelCap + " <-> " + hostShaft.LabelCap
                + " on " + hostShaft.Map);
        }

        // True when a landing's entrance is usable on the current gravship stack.
        public static bool EntranceOnCurrentStack(MapPortal entrance, Map landingMap)
        {
            if (entrance == null || entrance.Destroyed || !entrance.Spawned)
            {
                return false;
            }

            Map root = StrataGravshipUtility.FindGravshipStackRoot(landingMap);
            if (root == null)
            {
                // Not a gravship stack — any valid entrance is fine.
                return true;
            }

            return entrance.Map == root
                || StrataGravshipUtility.FindGravshipStackRoot(entrance.Map) == root;
        }

        public static Map ResolveGravshipHostForLanding(Map landingMap)
        {
            Map source = (landingMap?.Parent as PocketMapParent)?.sourceMap;
            if (source != null && StrataGravshipUtility.FindGravEngineOnMap(source) != null)
            {
                return source;
            }

            Map root = StrataGravshipUtility.FindGravshipStackRoot(landingMap);
            if (root != null && StrataGravshipUtility.FindGravEngineOnMap(root) != null)
            {
                return root;
            }

            return source ?? root;
        }

        public static IntVec3 ResolveExitCell(Map host, MapPortal preferredEntrance)
        {
            if (host == null)
            {
                return IntVec3.Invalid;
            }

            if (preferredEntrance != null && preferredEntrance.Spawned && preferredEntrance.Map == host)
            {
                IntVec3 near = preferredEntrance.Position;
                if (CellFinder.TryFindRandomCellNear(
                        near,
                        host,
                        4,
                        c => c.Standable(host) && !c.Fogged(host),
                        out IntVec3 cell))
                {
                    return cell;
                }

                return near;
            }

            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is IStrataGravshipPortal && thing.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(thing))
                {
                    if (CellFinder.TryFindRandomCellNear(
                            thing.Position,
                            host,
                            4,
                            c => c.Standable(host) && !c.Fogged(host),
                            out IntVec3 atShaft))
                    {
                        return atShaft;
                    }
                }
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
            IntVec3 anchor = engine != null ? engine.Position : host.Center;
            if (CellFinder.TryFindRandomCellNear(
                    anchor,
                    host,
                    8,
                    c => c.Standable(host) && !c.Fogged(host),
                    out IntVec3 nearEngine))
            {
                return nearEngine;
            }

            return CellFinder.RandomCell(host);
        }
    }
}
