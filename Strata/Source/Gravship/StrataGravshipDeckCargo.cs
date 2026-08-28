using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // G8: pack linked A+/B+ deck contents at takeoff (engine-relative, unrotated
    // frame) and place them at the landed engine root using the exact vanilla
    // transform: dest = root + PrefabUtility.GetAdjustedLocalPosition(local, rot),
    // rot = gravship.Rotation applied like Gravship.GetPlacementValues. This is
    // what vanilla does for the host deck, so pockets stay 1:1 with the ship even
    // when the player rotates the landing.
    //
    // Also captured: pocket landings/exit portals + pawns (kept spawned during
    // travel, moved at land), player-visible deck terrain + roofs, zones and
    // cell designations. Capture origin is drift-corrected per pocket (landing
    // re-anchored under its host shaft) so an old misaligned land self-heals on
    // the next flight.
    public static class StrataGravshipDeckCargo
    {
        public class CargoThing : IExposable
        {
            public Thing thing;
            public IntVec3 local;
            public Rot4 rotation = Rot4.North;
            // Portals/pawns stay spawned during travel; moved (not respawned) at land.
            public bool moveOnly;

            public void ExposeData()
            {
                Scribe_Values.Look(ref moveOnly, "moveOnly", false);
                if (moveOnly)
                {
                    Scribe_References.Look(ref thing, "thingRef");
                }
                else
                {
                    Scribe_Deep.Look(ref thing, "thing");
                }
                Scribe_Values.Look(ref local, "local", IntVec3.Invalid);
                Scribe_Values.Look(ref rotation, "rot", Rot4.North);
            }
        }

        public class CargoTerrain : IExposable
        {
            public IntVec3 local;
            public TerrainDef terrain;
            public RoofDef roof;

            public void ExposeData()
            {
                Scribe_Values.Look(ref local, "local", IntVec3.Invalid);
                Scribe_Defs.Look(ref terrain, "terrain");
                Scribe_Defs.Look(ref roof, "roof");
            }
        }

        public class DeckCargo : IExposable
        {
            public int pocketMapId = -1;
            public bool isTower;
            // Capture origin on the shared 1:1 grid (drift-corrected per pocket).
            public IntVec3 origin = IntVec3.Invalid;
            public List<CargoThing> things = new List<CargoThing>();
            public List<CargoTerrain> terrain = new List<CargoTerrain>();

            public void ExposeData()
            {
                Scribe_Values.Look(ref pocketMapId, "pocketMapId", -1);
                Scribe_Values.Look(ref isTower, "isTower", false);
                Scribe_Values.Look(ref origin, "origin", IntVec3.Invalid);
                Scribe_Collections.Look(ref things, "things", LookMode.Deep);
                Scribe_Collections.Look(ref terrain, "terrain", LookMode.Deep);
                things ??= new List<CargoThing>();
                terrain ??= new List<CargoTerrain>();
            }
        }

        private static readonly List<DeckCargo> pending = new List<DeckCargo>();
        private static bool placedThisLand;
        // Engine facing at takeoff — fallback rotation delta when no Gravship object.
        private static Rot4 takeoffEngineRot = Rot4.Invalid;

        public static bool HasPending => pending.Count > 0;

        public static bool PlacedThisLand => placedThisLand;

        [StrataSessionReset]
        public static void ResetSession()
        {
            pending.Clear();
            placedThisLand = false;
            takeoffEngineRot = Rot4.Invalid;
        }

        public static List<DeckCargo> TakeForSave() => new List<DeckCargo>(pending);

        public static Rot4 TakeRotForSave() => takeoffEngineRot;

        public static void RestoreFromSave(List<DeckCargo> saved, Rot4 savedRot)
        {
            pending.Clear();
            if (saved != null)
            {
                pending.AddRange(saved);
            }
            takeoffEngineRot = savedRot;
        }

        // engineRotAtTakeoff: pass Rot4.Invalid when unknown — default(Rot4) is
        // North, which would silently claim a known facing.
        public static void CaptureAll(
            Building_GravEngine engine,
            List<Map> levels,
            IntVec3 origin,
            Rot4 engineRotAtTakeoff)
        {
            pending.Clear();
            placedThisLand = false;
            takeoffEngineRot = engineRotAtTakeoff.IsValid
                ? engineRotAtTakeoff
                : (engine != null && engine.Spawned ? engine.Rotation : Rot4.Invalid);
            if (engine == null || levels == null)
            {
                return;
            }
            if (!origin.IsValid)
            {
                origin = engine.Position;
            }
            Map host = engine.MapHeld ?? engine.Map;
            for (int i = 0; i < levels.Count; i++)
            {
                Map pocket = levels[i];
                if (pocket == null || !StrataGravshipStackUtility.IsStrataLinkedLevel(pocket))
                {
                    continue;
                }
                DeckCargo cargo = CaptureOne(pocket, host, origin);
                if (cargo.things.Count > 0 || cargo.terrain.Count > 0)
                {
                    pending.Add(cargo);
                }
            }
            if (pending.Count > 0)
            {
                StrataLog.Verbose("[Strata] G8 deck cargo: packed " + pending.Count
                    + " linked level(s) for land place (rot-aware).");
            }
        }

        // origin = takeoffEnginePos + (landing - hostShaft): re-anchors a drifted
        // room under its shaft so old misalignment does not survive the flight.
        // Uses the InitiateTakeoff snapshots — the live host shafts are already
        // despawned (packed) by the time RegisterTakeoff captures cargo.
        private static IntVec3 ResolveOrigin(Map pocket, Map host, IntVec3 engineOrigin)
        {
            if (host != null && pocket.Size != host.Size)
            {
                return engineOrigin;
            }
            foreach (StrataGravshipPortalTravel.PortalSnapshot snap
                in StrataGravshipPortalTravel.PeekSnapshots())
            {
                if (snap == null || snap.pocketMapId != pocket.uniqueID
                    || !snap.offsetFromEngine.IsValid)
                {
                    continue;
                }
                MapPortal landing = FindLandingForSnapshot(pocket, snap);
                if (landing == null)
                {
                    continue;
                }
                IntVec3 shaftPosAtTakeoff = engineOrigin + snap.offsetFromEngine;
                IntVec3 drift = landing.Position - shaftPosAtTakeoff;
                if (drift != IntVec3.Zero)
                {
                    StrataLog.Verbose("[Strata] G8 deck cargo: pocket " + pocket.uniqueID
                        + " drift " + drift + " corrected at capture (re-anchored under shaft "
                        + (snap.shaftId ?? snap.defName) + ").");
                }
                return engineOrigin + drift;
            }
            return engineOrigin;
        }

        private static MapPortal FindLandingForSnapshot(
            Map pocket,
            StrataGravshipPortalTravel.PortalSnapshot snap)
        {
            MapPortal first = null;
            foreach (Thing thing in pocket.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not PocketMapExit landing || !landing.Spawned
                    || !StrataGravshipUtility.IsGravshipLanding(landing))
                {
                    continue;
                }
                first ??= landing;
                // Two shafts can share one pocket (two landings) — match by the
                // entrance's stable shaftId when possible.
                if (!snap.shaftId.NullOrEmpty()
                    && landing.entrance != null
                    && StrataGravshipShaftIdentity.CompOf(landing.entrance)?.shaftId == snap.shaftId)
                {
                    return landing;
                }
            }
            return first;
        }

        private static DeckCargo CaptureOne(Map pocket, Map host, IntVec3 engineOrigin)
        {
            IntVec3 origin = ResolveOrigin(pocket, host, engineOrigin);
            var cargo = new DeckCargo
            {
                pocketMapId = pocket.uniqueID,
                isTower = StrataMapUtility.IsUpperLevel(pocket),
                origin = origin,
            };
            CaptureTerrain(pocket, cargo, origin);
            var seen = new HashSet<Thing>();
            var snapshot = new List<Thing>(pocket.listerThings.AllThings);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing thing = snapshot[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || !seen.Add(thing))
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                // Landings/exits and pawns ride the pocket spawned; record their
                // spot so land can move them with the same transform.
                if (thing is MapPortal || thing is Pawn)
                {
                    cargo.things.Add(new CargoThing
                    {
                        thing = thing,
                        local = thing.Position - origin,
                        rotation = thing.Rotation,
                        moveOnly = true,
                    });
                    continue;
                }
                if (thing.def.category != ThingCategory.Building
                    && thing.def.category != ThingCategory.Item)
                {
                    continue;
                }
                if (thing.def.category == ThingCategory.Item && !thing.def.bringAlongOnGravship)
                {
                    continue;
                }
                IntVec3 local = thing.Position - origin;
                Rot4 rot = thing.Rotation;
                thing.DeSpawn(DestroyMode.WillReplace);
                cargo.things.Add(new CargoThing
                {
                    thing = thing,
                    local = local,
                    rotation = rot,
                });
            }
            return cargo;
        }

        // Player-visible deck floor travels with the room: managed deck terrain
        // plus player-built floors laid on it (carpet on the underdeck).
        private static void CaptureTerrain(Map pocket, DeckCargo cargo, IntVec3 origin)
        {
            bool upper = cargo.isTower;
            foreach (IntVec3 cell in pocket.AllCells)
            {
                TerrainDef terrain = cell.GetTerrain(pocket);
                if (terrain == null)
                {
                    continue;
                }
                bool managedDeck = upper
                    ? terrain.defName == UpperDeckUtility.RoofDeckDefName
                    : terrain.defName == GravshipDeckUtility.DeckDefName;
                bool playerFloor = terrain.BuildableByPlayer
                    && terrain.defName != UpperDeckUtility.OpenSkyDefName
                    && terrain.defName != GravshipDeckUtility.HullDefName;
                if (!managedDeck && !playerFloor)
                {
                    continue;
                }
                cargo.terrain.Add(new CargoTerrain
                {
                    local = cell - origin,
                    terrain = terrain,
                    roof = pocket.roofGrid.RoofAt(cell),
                });
            }
        }

        // Vanilla Gravship.GetPlacementValues, applied to one thing.
        private static void GetPlacement(
            ThingDef def,
            IntVec3 local,
            Rot4 origRot,
            Rot4 shipRot,
            out IntVec3 adjustedLocal,
            out Rot4 rot)
        {
            adjustedLocal = PrefabUtility.GetAdjustedLocalPosition(local, shipRot);
            IntVec2 size = def.size;
            bool rotate = true;
            if (!def.rotatable && size.x == size.z)
            {
                GenAdj.AdjustForRotation(ref adjustedLocal, ref size, def.defaultPlacingRot, shipRot);
                rotate = false;
            }
            else if (!def.rotatable && def.category != ThingCategory.Building)
            {
                rotate = false;
            }
            rot = rotate
                ? new Rot4((shipRot.AsInt + origRot.AsInt) % 4)
                : def.defaultPlacingRot;
        }

        public static void PlaceAll(
            Gravship ship,
            Map host,
            Building_GravEngine engine)
        {
            placedThisLand = false;
            if (pending.Count == 0 || host == null || engine == null)
            {
                return;
            }
            Rot4 shipRot = ResolveLandRotation(ship, engine);
            IntVec3 root = engine.Position;
            int placedThings = 0;
            for (int i = 0; i < pending.Count; i++)
            {
                placedThings += PlaceOne(pending[i], root, shipRot);
            }
            pending.Clear();
            placedThisLand = placedThings > 0;
            if (placedThings > 0)
            {
                StrataLog.Verbose("[Strata] G8 deck cargo: placed " + placedThings
                    + " thing(s) at landed engine root " + root
                    + " (rotation " + shipRot.ToStringHuman() + ").");
            }
        }

        private static Rot4 ResolveLandRotation(Gravship ship, Building_GravEngine engine)
        {
            if (ship != null)
            {
                return ship.Rotation;
            }
            // Fallback: the packed engine itself was placed with the ship rotation,
            // so the delta between its takeoff and landed facing is the ship delta.
            if (takeoffEngineRot.IsValid && engine != null && engine.Spawned)
            {
                return new Rot4((engine.Rotation.AsInt - takeoffEngineRot.AsInt + 4) % 4);
            }
            return Rot4.North;
        }

        private static int PlaceOne(DeckCargo cargo, IntVec3 root, Rot4 shipRot)
        {
            Map pocket = StrataGravshipOrphanLevels.FindMapById(cargo.pocketMapId);
            if (pocket == null || !Find.Maps.Contains(pocket))
            {
                return 0;
            }

            // 1) Terrain + roofs first so things land on walkable deck (vanilla order).
            var newTerrainCells = new HashSet<IntVec3>();
            for (int t = 0; t < cargo.terrain.Count; t++)
            {
                CargoTerrain ct = cargo.terrain[t];
                if (ct?.terrain == null)
                {
                    continue;
                }
                IntVec3 dest = root + PrefabUtility.GetAdjustedLocalPosition(ct.local, shipRot);
                if (!dest.InBounds(pocket))
                {
                    continue;
                }
                dest.GetFirstMineable(pocket)?.Destroy(DestroyMode.Vanish);
                pocket.terrainGrid.SetTerrain(dest, ct.terrain);
                pocket.roofGrid.SetRoof(dest, ct.roof);
                newTerrainCells.Add(dest);
            }

            // 2) Zones and cell designations follow the same transform.
            if (cargo.origin.IsValid)
            {
                TransformZones(pocket, cargo.origin, root, shipRot);
                TransformDesignations(pocket, cargo.origin, root, shipRot);
            }

            // 3) Things: portals first (exact cell), then vanilla spawn priority.
            var ordered = new List<CargoThing>(cargo.things);
            ordered.Sort((a, b) =>
            {
                int pa = a.thing is MapPortal ? 0 : (a.thing is Pawn ? 2 : 1);
                int pb = b.thing is MapPortal ? 0 : (b.thing is Pawn ? 2 : 1);
                if (pa != pb)
                {
                    return pa.CompareTo(pb);
                }
                return GravshipUtility.ThingSpawnPriority(a.thing)
                    .CompareTo(GravshipUtility.ThingSpawnPriority(b.thing));
            });

            int placed = 0;
            for (int t = 0; t < ordered.Count; t++)
            {
                CargoThing ct = ordered[t];
                if (ct?.thing == null || ct.thing.Destroyed)
                {
                    continue;
                }
                GetPlacement(ct.thing.def, ct.local, ct.rotation, shipRot,
                    out IntVec3 adjusted, out Rot4 rot);
                IntVec3 dest = (root + adjusted).ClampInsideMap(pocket);
                if (ct.moveOnly)
                {
                    bool movedOk = MoveSpawnedThing(ct.thing, dest, pocket, rot);
                    if (movedOk)
                    {
                        placed++;
                    }
                    else if (ct.thing is MapPortal
                        && (!ct.thing.Spawned || ct.thing.Map != pocket || ct.thing.Position != dest))
                    {
                        StrataLog.Warning("[Strata] G8 deck cargo: landing " + ct.thing.LabelCap
                            + " (" + ct.thing.ThingID + ") not moved to " + dest
                            + " (spawned=" + ct.thing.Spawned
                            + " map=" + (ct.thing.Map?.uniqueID.ToString() ?? "null")
                            + " pos=" + ct.thing.Position + ").");
                    }
                    continue;
                }
                if (ct.thing.Spawned)
                {
                    continue;
                }
                if (GenSpawn.Spawn(ct.thing, dest, pocket, rot, WipeMode.VanishOrMoveAside) != null)
                {
                    placed++;
                }
            }

            // 4) Clear the abandoned floor at the old spot (pawns moved above).
            ClearOldTerrain(cargo, pocket, newTerrainCells);
            return placed;
        }

        private static bool MoveSpawnedThing(Thing thing, IntVec3 dest, Map pocket, Rot4 rot)
        {
            if (!dest.InBounds(pocket))
            {
                return false;
            }
            if (thing.Spawned && thing.Map == pocket && thing.Position == dest)
            {
                return false;
            }
            if (thing is Pawn pawn)
            {
                if (!pawn.Spawned)
                {
                    return false;
                }
                IntVec3 cell = dest.Standable(pocket)
                    ? dest
                    : CellFinder.RandomClosewalkCellNear(dest.ClampInsideMap(pocket), pocket, 4);
                pawn.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(pawn, cell, pocket, WipeMode.Vanish);
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
                return true;
            }
            if (!thing.Spawned)
            {
                return false;
            }
            // Portals: exact cell, clear blockers (same contract as PocketAlign).
            CellRect rect = GenAdj.OccupiedRect(dest, rot, thing.def.Size);
            if (!rect.InBounds(pocket))
            {
                return false;
            }
            IntVec3 originalPos = thing.Position;
            Rot4 originalRot = thing.Rotation;
            // Move scope lifts the portal despawn-immunity patch;
            // currentlyGeneratingPortal keeps PocketMapExit.SpawnSetup wiring.
            MapPortal entrance = (thing as PocketMapExit)?.entrance;
            StrataPortalUtility.BeginPortalMove();
            PocketMapUtility.currentlyGeneratingPortal = entrance;
            try
            {
                thing.DeSpawn(DestroyMode.WillReplace);
                StrataPortalUtility.ClearBuildingsAndItemsInRect(pocket, rect, thing);
                if (!thing.Spawned
                    && GenSpawn.Spawn(thing, dest, pocket, rot, WipeMode.VanishOrMoveAside) == null
                    && !thing.Spawned && !thing.Destroyed)
                {
                    // Never leave a landing unspawned — fall back to its old cell.
                    GenSpawn.Spawn(thing, originalPos, pocket, originalRot, WipeMode.VanishOrMoveAside);
                    StrataLog.Warning("[Strata] G8 deck cargo: could not move " + thing.LabelCap
                        + " to " + dest + " — restored at " + originalPos + ".");
                }
            }
            finally
            {
                PocketMapUtility.currentlyGeneratingPortal = null;
                StrataPortalUtility.EndPortalMove();
            }
            return thing.Spawned && thing.Map == pocket && thing.Position == dest;
        }

        private static void ClearOldTerrain(
            DeckCargo cargo,
            Map pocket,
            HashSet<IntVec3> newTerrainCells)
        {
            if (!cargo.origin.IsValid)
            {
                return;
            }
            TerrainDef voidT = cargo.isTower
                ? UpperDeckUtility.OpenSky
                : GravshipDeckUtility.VoidTerrain;
            int cleared = 0;
            for (int t = 0; t < cargo.terrain.Count; t++)
            {
                CargoTerrain ct = cargo.terrain[t];
                if (ct == null)
                {
                    continue;
                }
                IntVec3 old = cargo.origin + ct.local;
                if (!old.InBounds(pocket) || newTerrainCells.Contains(old))
                {
                    continue;
                }
                if (CellHasSpawnedContent(pocket, old))
                {
                    continue;
                }
                Thing sub = StrataGravshipSubstructureSync.SubstructureAt(pocket, old);
                if (sub != null && !sub.Destroyed)
                {
                    sub.Destroy(DestroyMode.Vanish);
                }
                pocket.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(old);
                pocket.terrainGrid.SetTerrain(old, voidT);
                pocket.roofGrid.SetRoof(old, null);
                cleared++;
            }
            // Hull rim is not packed (only deck/floors) — void the ring around
            // the old pad so leftover circles don't linger until the deferred sweep.
            if (!cargo.isTower && cargo.terrain.Count > 0)
            {
                var rimCandidates = new HashSet<IntVec3>();
                for (int t = 0; t < cargo.terrain.Count; t++)
                {
                    CargoTerrain ct = cargo.terrain[t];
                    if (ct == null)
                    {
                        continue;
                    }
                    IntVec3 old = cargo.origin + ct.local;
                    if (!old.InBounds(pocket))
                    {
                        continue;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        IntVec3 adj = old + GenAdj.CardinalDirections[i];
                        if (adj.InBounds(pocket)
                            && !newTerrainCells.Contains(adj)
                            && adj.GetTerrain(pocket)?.defName == GravshipDeckUtility.HullDefName)
                        {
                            rimCandidates.Add(adj);
                        }
                    }
                }
                foreach (IntVec3 adj in rimCandidates)
                {
                    if (CellHasSpawnedContent(pocket, adj))
                    {
                        continue;
                    }
                    Thing sub = StrataGravshipSubstructureSync.SubstructureAt(pocket, adj);
                    if (sub != null && !sub.Destroyed)
                    {
                        sub.Destroy(DestroyMode.Vanish);
                    }
                    pocket.GetComponent<MapComponent_StrataProjectedSubstructure>()?.UnmarkProjected(adj);
                    pocket.terrainGrid.SetTerrain(adj, voidT);
                    pocket.roofGrid.SetRoof(adj, null);
                    cleared++;
                }
            }
            if (cleared > 0)
            {
                StrataLog.Verbose("[Strata] G8 deck cargo: cleared " + cleared
                    + " old-footprint cell(s) on pocket " + pocket.uniqueID + ".");
            }
        }

        private static bool CellHasSpawnedContent(Map map, IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                if (thing.def.category == ThingCategory.Building
                    || thing.def.category == ThingCategory.Item
                    || thing is Pawn)
                {
                    return true;
                }
            }
            return false;
        }

        private static void TransformZones(Map map, IntVec3 origin, IntVec3 root, Rot4 shipRot)
        {
            StrataGravshipPocketAlign.RemapZones(
                map,
                cell => root + PrefabUtility.GetAdjustedLocalPosition(cell - origin, shipRot));
        }

        private static void TransformDesignations(Map map, IntVec3 origin, IntVec3 root, Rot4 shipRot)
        {
            StrataGravshipPocketAlign.RemapDesignations(
                map,
                cell => root + PrefabUtility.GetAdjustedLocalPosition(cell - origin, shipRot));
        }
    }
}
