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
            // G2 stable identity
            public string shaftId;
            public string stackGuid;

            public void ExposeData()
            {
                Scribe_Values.Look(ref defName, "defName");
                Scribe_Values.Look(ref offsetFromEngine, "offsetFromEngine", IntVec3.Invalid);
                Scribe_Values.Look(ref rotation, "rotation", Rot4.North);
                Scribe_Values.Look(ref pocketMapId, "pocketMapId", -1);
                Scribe_Values.Look(ref isTower, "isTower", false);
                Scribe_Values.Look(ref engineRotationAtTakeoff, "engineRotationAtTakeoff", Rot4.Invalid);
                Scribe_Values.Look(ref shaftId, "shaftId");
                Scribe_Values.Look(ref stackGuid, "stackGuid");
            }
        }

        private static readonly List<PortalSnapshot> snapshots = new List<PortalSnapshot>();

        // Launch map while engine may already be despawned during GenerateGravship.
        private static Map launchMapAtTakeoff;

        public static void ResetSession()
        {
            snapshots.Clear();
            launchMapAtTakeoff = null;
        }

        public static IReadOnlyList<PortalSnapshot> PeekSnapshots() => snapshots;

        // Pack a host shaft into the Gravship.Things dictionary even when
        // OnValidSubstructure would reject it (GravAnchor leftovers).
        // Must despawn here: skipping vanilla AddThing also skips its DeSpawn,
        // which otherwise leaves stairs on GravAnchor-kept maps.
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

            if (!things.ContainsKey(thing))
            {
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
            }

            DespawnPackedHostShaft(thing);
            return true;
        }

        private static void DespawnPackedHostShaft(Thing shaft)
        {
            if (shaft == null || shaft.Destroyed || !shaft.Spawned)
            {
                return;
            }

            StrataPortalUtility.BeginPortalMove();
            try
            {
                shaft.PreSwapMap();
                shaft.DeSpawn(DestroyMode.WillReplace);
            }
            finally
            {
                StrataPortalUtility.EndPortalMove();
            }
        }

        // After Odyssey GenerateGravship: any host shaft still sitting on the
        // launch map (visible under GravAnchor) is forced onto the ship and
        // despawned like vanilla packed buildings.
        public static void SweepLeftBehindHostShafts(Gravship ship, Building_GravEngine engine)
        {
            if (ship == null)
            {
                return;
            }

            int swept = 0;

            // Packed but still Spawned (force-pack skipped vanilla DeSpawn, or
            // engine.Map was already null when an earlier sweep ran).
            var packed = AccessTools.Field(typeof(Gravship), "things")
                .GetValue(ship) as Dictionary<Thing, PositionData>;
            if (packed != null)
            {
                var packedList = new List<Thing>(packed.Keys);
                for (int i = 0; i < packedList.Count; i++)
                {
                    Thing thing = packedList[i];
                    if (thing == null || !thing.Spawned
                        || !StrataGravshipUtility.IsGravshipHostShaft(thing))
                    {
                        continue;
                    }

                    DespawnPackedHostShaft(thing);
                    swept++;
                }
            }

            Map map = engine?.Map ?? launchMapAtTakeoff;
            if (map != null && Find.Maps.Contains(map))
            {
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

                for (int i = 0; i < remaining.Count; i++)
                {
                    Thing shaft = remaining[i];
                    if (shaft == null || shaft.Destroyed || !shaft.Spawned)
                    {
                        continue;
                    }

                    IntVec3 offset = ResolvePackOffset(shaft, engine);
                    TryForcePackHostShaft(ship, shaft, offset);
                    if (!shaft.Spawned)
                    {
                        swept++;
                    }
                }
            }

            if (swept > 0)
            {
                Log.Message("[Strata] Gravship takeoff: swept " + swept
                    + " left-behind host shaft(s) off launch map (GravAnchor-safe).");
            }
        }

        private static IntVec3 ResolvePackOffset(Thing shaft, Building_GravEngine engine)
        {
            if (engine != null && engine.Spawned)
            {
                return shaft.Position - engine.Position;
            }

            for (int s = 0; s < snapshots.Count; s++)
            {
                if (snapshots[s].defName == shaft.def.defName)
                {
                    return snapshots[s].offsetFromEngine;
                }
            }

            return IntVec3.Zero;
        }

        public static void SnapshotHostPortals(Building_GravEngine engine)
        {
            snapshots.Clear();
            launchMapAtTakeoff = engine?.Map;
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
                CompStrataGravshipShaft identity = StrataGravshipShaftIdentity.CompOf(portal);
                string shaftId = StrataGravshipShaftIdentity.GetOrMintShaftId(portal);
                string stackGuid = WorldComponent_StrataGravshipStacks.Get()?.PeekOrMintStackGuid();
                if (identity != null)
                {
                    identity.BindStack(stackGuid);
                    if (pocketId >= 0)
                    {
                        identity.RememberPocket(portal.PocketMap);
                    }
                }
                snapshots.Add(new PortalSnapshot
                {
                    defName = portal.def.defName,
                    offsetFromEngine = portal.Position - engine.Position,
                    rotation = portal.Rotation,
                    pocketMapId = pocketId >= 0
                        ? pocketId
                        : (identity?.lastPocketMapId ?? -1),
                    isTower = StrataGravshipUtility.IsGravshipTowerShaft(portal),
                    engineRotationAtTakeoff = engine.Rotation,
                    shaftId = shaftId,
                    stackGuid = stackGuid,
                });
            }

            if (snapshots.Count > 0)
            {
                Log.Message("[Strata] Gravship takeoff: snapshot "
                    + snapshots.Count + " host shaft(s) for land reconnect (G2 shaft IDs).");
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
            bool restoreMissingShafts = true,
            Building_GravEngine landEngine = null,
            Gravship landShip = null)
        {
            if (hostMap == null)
            {
                return;
            }

            Building_GravEngine engine = landEngine ?? StrataGravshipUtility.FindGravEngineOnMap(hostMap);
            if (engine == null)
            {
                return;
            }

            if (restoreMissingShafts)
            {
                EnsureHostShafts(hostMap, engine, landShip);
            }
            WirePocketsToHostShafts(hostMap, pockets ?? CollectPocketsOnHost(hostMap), engine);
            if (restoreMissingShafts)
            {
                snapshots.Clear();
            }
        }

        // Call before Odyssey places cargo: packed stairs can keep Spawned=true
        // with no map (or still on the GravAnchor site) → "already spawned".
        public static void EnsurePackedHostShaftsUnspawned(Gravship ship)
        {
            if (ship == null)
            {
                return;
            }

            var packed = AccessTools.Field(typeof(Gravship), "things")
                .GetValue(ship) as Dictionary<Thing, PositionData>;
            if (packed == null)
            {
                return;
            }

            int cleared = 0;
            int stripped = 0;
            var keys = new List<Thing>(packed.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                Thing thing = keys[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                // Pocket-side things must never ride the vanilla ship — spawning
                // them on the host errors ("already spawned") or teleports the
                // landing off its level.
                bool pocketSide = thing is PocketMapExit
                    || (thing.Spawned && thing.Map != null
                        && StrataGravshipStackUtility.IsStrataLinkedLevel(thing.Map));
                if (pocketSide)
                {
                    packed.Remove(thing);
                    stripped++;
                    StrataLog.Warning("[Strata] Gravship land: stripped pocket-side "
                        + thing.LabelCap + " (" + thing.ThingID
                        + ") from packed ship — it stays on its linked level.");
                    continue;
                }

                if (!StrataGravshipUtility.IsGravshipHostShaft(thing))
                {
                    continue;
                }

                if (ForceUnspawn(thing))
                {
                    cleared++;
                }
            }

            if (cleared > 0 || stripped > 0)
            {
                Log.Message("[Strata] Gravship land: unspawned " + cleared
                    + " packed host shaft(s), stripped " + stripped
                    + " pocket-side thing(s) before PlaceGravship.");
            }
        }

        private static bool ForceUnspawn(Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned)
            {
                return false;
            }

            StrataPortalUtility.BeginPortalMove();
            try
            {
                if (thing.Map != null)
                {
                    thing.PreSwapMap();
                    thing.DeSpawn(DestroyMode.WillReplace);
                }
            }
            finally
            {
                StrataPortalUtility.EndPortalMove();
            }

            if (thing.Spawned)
            {
                thing.ForceSetStateToUnspawned();
            }

            return !thing.Spawned;
        }

        // Deterministic land invariant: a gravship pocket landing sits at EXACTLY
        // its host shaft's cell (raw 1:1 — never proportional; a new host map of
        // a different size must not scale the pocket). Self-heals whatever else
        // moved or failed to move the landing during the land.
        public static int SnapAllLandingsUnderShafts(Map host)
        {
            if (host == null)
            {
                return 0;
            }
            int moved = 0;
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal shaft && shaft.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(shaft)
                    && shaft.PocketMapExists
                    && SnapLandingUnderShaft(shaft))
                {
                    moved++;
                }
            }
            return moved;
        }

        public static bool SnapLandingUnderShaft(MapPortal shaft)
        {
            if (shaft == null || !shaft.Spawned || !shaft.PocketMapExists)
            {
                return false;
            }
            Map pocket = shaft.PocketMap;
            MapPortal landing = shaft.exit != null && shaft.exit.Spawned && shaft.exit.Map == pocket
                ? shaft.exit
                : FindLandingOn(pocket);
            if (landing == null || !landing.Spawned || landing.Map != pocket)
            {
                return false;
            }
            IntVec3 dest = shaft.Position;
            if (!dest.InBounds(pocket) || landing.Position == dest)
            {
                return false;
            }
            CellRect rect = GenAdj.OccupiedRect(dest, shaft.Rotation, landing.def.Size);
            if (!rect.InBounds(pocket))
            {
                return false;
            }

            IntVec3 oldPos = landing.Position;
            Rot4 oldRot = landing.Rotation;
            // currentlyGeneratingPortal + move scope: the despawn-immunity patch
            // otherwise swallows the DeSpawn and the respawn no-ops with an
            // "already spawned" error.
            bool ok;
            StrataPortalUtility.BeginPortalMove();
            PocketMapUtility.currentlyGeneratingPortal = shaft;
            try
            {
                landing.DeSpawn(DestroyMode.WillReplace);
                StrataPortalUtility.ClearBuildingsAndItemsInRect(pocket, rect, landing);
                ArrivalZoneUtility.PrepareLandingCell(pocket, dest);
                ok = landing.Spawned
                    || GenSpawn.Spawn(landing, dest, pocket, shaft.Rotation, WipeMode.VanishOrMoveAside) != null;
                if (!ok && !landing.Spawned && !landing.Destroyed)
                {
                    GenSpawn.Spawn(landing, oldPos, pocket, oldRot, WipeMode.VanishOrMoveAside);
                    StrataLog.Warning("[Strata] Landing snap: could not place " + landing.LabelCap
                        + " at " + dest + " — restored at " + oldPos + ".");
                    return false;
                }
            }
            finally
            {
                PocketMapUtility.currentlyGeneratingPortal = null;
                StrataPortalUtility.EndPortalMove();
            }
            // Verify the move actually landed where we asked (despawn immunity
            // used to fake success here).
            if (landing.Spawned && landing.Position == dest)
            {
                Log.Message("[Strata] Landing snap: " + landing.LabelCap + " " + oldPos
                    + " -> " + dest + " (under " + shaft.LabelCap + ").");
                return true;
            }
            StrataLog.Warning("[Strata] Landing snap: " + landing.LabelCap
                + " did not move (" + oldPos + " -> wanted " + dest
                + ", actual " + (landing.Spawned ? landing.Position.ToString() : "unspawned") + ").");
            return false;
        }

        // Cull orphaned duplicate landings on gravship pockets (left by old
        // versions that spawned fresh landings instead of moving them) and queue
        // the ghost deck around them for the deferred clear. A landing is live
        // iff its entrance is a spawned host shaft that points back at it.
        public static int CleanupPocketLeftovers(Map host)
        {
            if (host == null)
            {
                return 0;
            }
            int culled = 0;
            var pockets = new HashSet<Map>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal shaft && shaft.Spawned
                    && StrataGravshipUtility.IsGravshipHostShaft(shaft)
                    && shaft.PocketMapExists)
                {
                    pockets.Add(shaft.PocketMap);
                }
            }
            foreach (Map pocket in pockets)
            {
                var landings = new List<PocketMapExit>();
                foreach (Thing thing in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is PocketMapExit landing && landing.Spawned)
                    {
                        landings.Add(landing);
                    }
                }
                bool anyLive = false;
                for (int i = 0; i < landings.Count; i++)
                {
                    if (IsLiveLanding(landings[i], pocket))
                    {
                        anyLive = true;
                        break;
                    }
                }
                // Never cull the last way out of a level.
                if (!anyLive)
                {
                    continue;
                }
                for (int i = 0; i < landings.Count; i++)
                {
                    PocketMapExit landing = landings[i];
                    if (IsLiveLanding(landing, pocket))
                    {
                        continue;
                    }
                    Log.Message("[Strata] Gravship pocket cleanup: removed orphaned landing "
                        + landing.LabelCap + " at " + landing.Position
                        + " on " + pocket.uniqueID + ".");
                    StrataPortalUtility.ForceDestroyPortal(landing, DestroyMode.Vanish);
                    culled++;
                }
                // Ghost deck around removed landings drains via the deferred clear.
                GravshipDeckUtility.CleanupEmptySilhouetteIslands(pocket, host);
                if (StrataMapUtility.IsUpperLevel(pocket))
                {
                    UpperDeckUtility.CleanupEmptySilhouetteIslands(pocket);
                }
            }
            return culled;
        }

        private static bool IsLiveLanding(PocketMapExit landing, Map pocket)
        {
            MapPortal entrance = landing.entrance;
            return entrance != null && !entrance.Destroyed && entrance.Spawned
                && entrance.exit == landing
                && entrance.PocketMapExists && entrance.PocketMap == pocket;
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

        private static void EnsureHostShafts(Map host, Building_GravEngine engine, Gravship landShip)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                PortalSnapshot snap = snapshots[i];
                if (!snap.shaftId.NullOrEmpty()
                    && StrataGravshipShaftIdentity.FindHostShaftById(host, snap.shaftId) != null)
                {
                    continue;
                }
                if (FindHostShaftMatching(host, engine, snap) != null)
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(snap.defName);
                if (def == null)
                {
                    StrataLog.Warning("[Strata] Gravship land: missing shaft def " + snap.defName);
                    continue;
                }

                // Restore into the landed engine's frame — a rotated landing
                // rotates both the offset and the shaft's own facing.
                IntVec3 cell = engine.Position + OffsetForLandedEngine(engine, snap);
                Rot4 spawnRot = snap.rotation;
                if (snap.engineRotationAtTakeoff.IsValid)
                {
                    int rotDelta = (engine.Rotation.AsInt - snap.engineRotationAtTakeoff.AsInt + 4) % 4;
                    spawnRot = new Rot4((snap.rotation.AsInt + rotDelta) % 4);
                }
                if (!cell.InBounds(host))
                {
                    cell = engine.Position;
                }

                // Prefer a cell on the live ship footprint when offset lands off-substructure.
                if (!StrataGravshipUtility.CellOnGravship(host, cell)
                    && !TryFindShipCellNear(host, engine, def, spawnRot, out cell))
                {
                    // Keep offset cell; CanPlaceShaft may still accept it.
                }

                if (!CanPlaceShaft(host, def, cell, spawnRot))
                {
                    if (!TryFindShipCellNear(host, engine, def, spawnRot, out cell)
                        && !CellFinder.TryFindRandomCellNear(
                            engine.Position,
                            host,
                            8,
                            c => CanPlaceShaft(host, def, c, spawnRot),
                            out cell))
                    {
                        StrataLog.Warning("[Strata] Gravship land: could not place restored shaft "
                            + snap.defName);
                        continue;
                    }
                }

                // G2: reclaim by shaftId from cargo first — never MakeThing a twin.
                MapPortal existing = StrataGravshipShaftIdentity.FindPackedShaftById(
                        landShip, snap.shaftId)
                    ?? FindPackedShaft(landShip, snap.defName)
                    ?? FindShaftThingAnywhere(snap.defName);
                if (existing != null)
                {
                    if (existing.Destroyed)
                    {
                        StrataLog.Warning("[Strata] Gravship land: packed shaft " + snap.defName
                            + " is destroyed — skipping reclaim (PlaceGravship may already own it).");
                        continue;
                    }
                    if (existing.Spawned && existing.Map == host)
                    {
                        continue;
                    }

                    ForceUnspawn(existing);
                    if (existing.Destroyed || existing.Spawned)
                    {
                        continue;
                    }
                    StrataPortalUtility.PrefireWipeStrataPortals(
                        host, cell, spawnRot, existing.def.Size, existing);
                    GenSpawn.Spawn(existing, cell, host, spawnRot, WipeMode.VanishOrMoveAside);
                    CompStrataGravshipShaft id = StrataGravshipShaftIdentity.CompOf(existing);
                    if (id != null)
                    {
                        if (!snap.shaftId.NullOrEmpty())
                        {
                            id.shaftId = snap.shaftId;
                        }
                        id.BindStack(snap.stackGuid);
                        if (snap.pocketMapId >= 0)
                        {
                            Map pocket = StrataGravshipOrphanLevels.FindMapById(snap.pocketMapId);
                            id.RememberPocket(pocket);
                        }
                    }
                    Log.Message("[Strata] Gravship land: reclaimed packed shaft "
                        + def.defName + " at " + cell
                        + (snap.shaftId.NullOrEmpty() ? "" : " (shaftId " + snap.shaftId + ")"));
                    continue;
                }

                StrataLog.Warning("[Strata] Gravship land: no packed shaft for "
                    + snap.defName + " — skipping MakeThing restore (avoids duplicate off-pad).");
            }

            CullOffShipDuplicateShafts(host, engine);
        }

        private static MapPortal FindPackedShaft(Gravship ship, string defName)
        {
            if (ship == null || string.IsNullOrEmpty(defName))
            {
                return null;
            }

            var packed = AccessTools.Field(typeof(Gravship), "things")
                .GetValue(ship) as Dictionary<Thing, PositionData>;
            if (packed == null)
            {
                return null;
            }

            foreach (Thing thing in packed.Keys)
            {
                if (thing is MapPortal portal
                    && portal.def.defName == defName
                    && portal is IStrataGravshipPortal
                    && StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    return portal;
                }
            }

            return null;
        }

        private static bool TryFindShipCellNear(
            Map host,
            Building_GravEngine engine,
            ThingDef def,
            Rot4 rot,
            out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (host == null || engine == null || def == null)
            {
                return false;
            }

            if (CellFinder.TryFindRandomCellNear(
                    engine.Position,
                    host,
                    12,
                    c => CanPlaceShaft(host, def, c, rot)
                        && StrataGravshipUtility.CellOnGravship(host, c),
                    out cell))
            {
                return true;
            }

            return false;
        }

        private static MapPortal FindShaftThingAnywhere(string defName)
        {
            if (string.IsNullOrEmpty(defName) || Find.Maps == null)
            {
                return null;
            }

            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map map = Find.Maps[m];
                if (map == null)
                {
                    continue;
                }

                foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is MapPortal portal && portal.Spawned
                        && portal.def.defName == defName
                        && portal is IStrataGravshipPortal
                        && StrataGravshipUtility.IsGravshipHostShaft(portal))
                    {
                        return portal;
                    }
                }
            }

            return null;
        }

        // Drop extra host shafts of the same def that sit off the ship after a bad restore.
        private static void CullOffShipDuplicateShafts(Map host, Building_GravEngine engine)
        {
            if (host == null)
            {
                return;
            }

            var byDef = new Dictionary<string, List<MapPortal>>();
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned
                    || portal is not IStrataGravshipPortal
                    || !StrataGravshipUtility.IsGravshipHostShaft(portal))
                {
                    continue;
                }

                string key = portal.def.defName;
                if (!byDef.TryGetValue(key, out List<MapPortal> list))
                {
                    list = new List<MapPortal>();
                    byDef[key] = list;
                }

                list.Add(portal);
            }

            foreach (KeyValuePair<string, List<MapPortal>> pair in byDef)
            {
                List<MapPortal> list = pair.Value;
                if (list.Count < 2)
                {
                    continue;
                }

                MapPortal keep = null;
                int bestScore = int.MinValue;
                for (int i = 0; i < list.Count; i++)
                {
                    MapPortal shaft = list[i];
                    int score = 0;
                    if (StrataGravshipUtility.CellOnGravship(host, shaft.Position))
                    {
                        score += 1000;
                    }

                    if (engine != null)
                    {
                        score -= (shaft.Position - engine.Position).LengthManhattan;
                    }

                    if (shaft.PocketMapExists)
                    {
                        score += 50;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        keep = shaft;
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    MapPortal shaft = list[i];
                    if (shaft == keep || shaft.Destroyed)
                    {
                        continue;
                    }

                    Log.Message("[Strata] Gravship land: culled duplicate shaft "
                        + shaft.LabelCap + " at " + shaft.Position
                        + " (kept " + keep.Position + ")");
                    StrataPortalUtility.ForceDestroyPortal(shaft, DestroyMode.Vanish);
                }
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
            MapPortal onShip = null;
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

                if (onShip == null
                    && StrataGravshipUtility.CellOnGravship(host, portal.Position))
                {
                    onShip = portal;
                }

                byDef ??= portal;
            }

            return onShip ?? byDef;
        }

        private static void WirePocketsToHostShafts(Map host, List<Map> pockets, Building_GravEngine engine = null)
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

            engine ??= StrataGravshipUtility.FindGravEngineOnMap(host);
            var wiredPockets = new HashSet<Map>();
            var claimedShafts = new HashSet<MapPortal>();

            // G2/G5: wire by shaftId → pocketMapId table first (furnished travelling floor).
            var tableOnlyPockets = new HashSet<Map>();
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
                    MapPortal landing = FindLandingOn(pocket);
                    if (landing == null)
                    {
                        continue;
                    }

                    // G5: when shaftId is present, never fall back to defName / best-match.
                    if (!snap.shaftId.NullOrEmpty())
                    {
                        tableOnlyPockets.Add(pocket);
                        MapPortal byId = StrataGravshipShaftIdentity.FindHostShaftById(
                            host, snap.shaftId);
                        if (byId == null || claimedShafts.Contains(byId))
                        {
                            Log.Message("[Strata] G5 wiring table: no host shaft for shaftId "
                                + snap.shaftId + " → " + pocket);
                            continue;
                        }
                        ConnectPortalPair(byId, landing);
                        claimedShafts.Add(byId);
                        wiredPockets.Add(pocket);
                        continue;
                    }

                    MapPortal shaft = FindHostShaftMatching(host, engine, snap, claimedShafts);
                    if (shaft == null)
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
                // G5: pockets listed with a shaftId stay table-only (no best-match salvage).
                if (tableOnlyPockets.Contains(pocket))
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

        internal static MapPortal FindLandingOn(Map pocket)
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
            MapPortal best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < hostShafts.Count; i++)
            {
                MapPortal shaft = hostShafts[i];
                if (claimedShafts != null && claimedShafts.Contains(shaft))
                {
                    continue;
                }

                if (StrataGravshipUtility.IsGravshipTowerShaft(shaft) != wantTower)
                {
                    continue;
                }

                int score = 0;
                if (StrataGravshipUtility.CellOnGravship(shaft.Map, shaft.Position))
                {
                    score += 1000;
                }

                if (landing != null && landing.Spawned)
                {
                    score -= (shaft.Position - landing.Position).LengthManhattan;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = shaft;
                }
            }

            // No cross-type last resort — leave unwired for EnsureHostShafts.
            return best;
        }

        private static void ConnectPortalPair(MapPortal hostShaft, MapPortal landing)
        {
            StrataPortalUtility.ConnectPortalPair(hostShaft, landing);
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
