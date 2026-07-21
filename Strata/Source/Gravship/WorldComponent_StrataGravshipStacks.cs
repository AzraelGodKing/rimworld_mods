using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Tracks Strata pocket maps riding with a Gravship WorldObject between
    // takeoff and landing so AbandonMap cannot destroy them.
    public class WorldComponent_StrataGravshipStacks : WorldComponent
    {
        private List<TravellingStack> stacks = new List<TravellingStack>();

        // Maps currently protected from destroyOnParentMapAbandoned cascades.
        private HashSet<int> travellingMapIds = new HashSet<int>();

        // Engine cell + thingID at InitiateTakeoff — shift pockets on land; G1 engine of record.
        private IntVec3 pendingTakeoffEnginePos = IntVec3.Invalid;
        private int pendingTakeoffEngineThingId = -1;

        public WorldComponent_StrataGravshipStacks(World world) : base(world)
        {
        }

        public static WorldComponent_StrataGravshipStacks Get()
        {
            return Find.World?.GetComponent<WorldComponent_StrataGravshipStacks>();
        }

        public bool IsTravelling(Map map)
        {
            return map != null && travellingMapIds.Contains(map.uniqueID);
        }

        public void RememberTakeoffEngine(Building_GravEngine engine)
        {
            if (engine == null)
            {
                return;
            }
            if (engine.Position.IsValid)
            {
                pendingTakeoffEnginePos = engine.Position;
            }
            pendingTakeoffEngineThingId = engine.thingIDNumber;
            StrataGravshipPocketAlign.ClearLandAlignState();
        }

        /// <summary>G1: pinned takeoff engine thingID for the active travelling stack (or pending).</summary>
        public int PeekTakeoffEngineThingId(Gravship ship = null)
        {
            if (ship != null)
            {
                TravellingStack stack = FindStack(ship);
                if (stack != null && stack.takeoffEngineThingId >= 0)
                {
                    return stack.takeoffEngineThingId;
                }
            }
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].takeoffEngineThingId >= 0)
                {
                    return stacks[i].takeoffEngineThingId;
                }
            }
            return pendingTakeoffEngineThingId;
        }

        // Early mark + detach before the Gravship WorldObject exists.
        public void MarkTravelling(List<Map> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return;
            }
            for (int i = 0; i < levels.Count; i++)
            {
                Map map = levels[i];
                if (map == null)
                {
                    continue;
                }
                travellingMapIds.Add(map.uniqueID);
                if (map.Parent is PocketMapParent pocket)
                {
                    pocket.sourceMap = null;
                }
            }
        }

        public void RegisterTakeoff(Gravship ship, Building_GravEngine engine)
        {
            if (ship == null || engine == null)
            {
                return;
            }
            List<Map> levels = StrataGravshipStackUtility.CollectTravellingLevels(engine);
            // Prefer already-marked maps if the host was already abandoned.
            if (levels.Count == 0 && travellingMapIds.Count > 0)
            {
                levels = new List<Map>();
                foreach (int id in travellingMapIds)
                {
                    Map map = FindMapById(id);
                    if (map != null && StrataGravshipStackUtility.IsStrataLinkedLevel(map))
                    {
                        levels.Add(map);
                    }
                }
            }
            if (levels.Count == 0)
            {
                return;
            }
            UnregisterShip(ship);
            MarkTravelling(levels);
            IntVec3 takeoffPos = pendingTakeoffEnginePos.IsValid
                ? pendingTakeoffEnginePos
                : engine.Position;
            int takeoffThingId = pendingTakeoffEngineThingId >= 0
                ? pendingTakeoffEngineThingId
                : engine.thingIDNumber;
            var stack = new TravellingStack
            {
                ship = ship,
                mapIds = new List<int>(),
                takeoffEnginePos = takeoffPos,
                takeoffEngineThingId = takeoffThingId,
            };
            for (int i = 0; i < levels.Count; i++)
            {
                stack.mapIds.Add(levels[i].uniqueID);
            }
            stacks.Add(stack);
            Log.Message($"[Strata] Gravship takeoff: {levels.Count} linked level(s) will follow the ship (engine thingID {takeoffThingId}).");
            Messages.Message(
                "Strata_GravshipLevelsTravel".Translate(levels.Count),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
        }

        // Mid-placement (engine PostSwapMap): reparent travelling floors only.
        // Do not restore shafts or clear travelling IDs — packed stairs are not
        // on the map yet, and a temporary restore drops the pocket link.
        public void PrepareLanding(Map newHost)
        {
            if (newHost == null)
            {
                return;
            }
            var maps = CollectTravellingMaps();
            if (maps.Count == 0)
            {
                return;
            }
            int pinnedId = PeekTakeoffEngineThingId();
            Building_GravEngine landEngine = StrataGravshipUtility.FindGravEngineForLandRebind(newHost, pinnedId);
            StrataGravshipStackUtility.RebindAll(maps, newHost);
            StrataGravshipPortalTravel.ReconnectOrRestore(
                newHost, maps, restoreMissingShafts: false, landEngine: landEngine);
            Log.Message($"[Strata] Gravship landing prepare: rebound {maps.Count} linked level(s) to {newHost} (shaft restore deferred).");
        }

        public void CompleteLanding(Gravship ship, Map newHost)
        {
            if (newHost == null)
            {
                return;
            }
            TravellingStack stack = ship != null ? FindStack(ship) : null;
            IntVec3 takeoffPos = pendingTakeoffEnginePos;
            int pinnedThingId = pendingTakeoffEngineThingId;
            var maps = new List<Map>();
            if (stack != null)
            {
                takeoffPos = stack.takeoffEnginePos.IsValid
                    ? stack.takeoffEnginePos
                    : takeoffPos;
                if (stack.takeoffEngineThingId >= 0)
                {
                    pinnedThingId = stack.takeoffEngineThingId;
                }
                for (int i = 0; i < stack.mapIds.Count; i++)
                {
                    Map map = FindMapById(stack.mapIds[i]);
                    if (map != null)
                    {
                        maps.Add(map);
                    }
                }
                stacks.Remove(stack);
            }
            else
            {
                maps.AddRange(CollectTravellingMaps());
            }

            // Also adopt unbound gravship floors already parented to this host
            // (PrepareLanding may have rebound them before the stack cleared).
            AddHostChildLevels(newHost, maps);

            Building_GravEngine landEngine = StrataGravshipUtility.FindGravEngineForLandRebind(
                newHost, pinnedThingId);

            if (maps.Count == 0)
            {
                // Idempotent re-wire: early hook cleared travelling but shafts
                // may still be unwired after packed stairs replaced restores.
                StrataGravshipPortalTravel.ReconnectOrRestore(
                    newHost, null, landEngine: landEngine, landShip: ship);
                StrataGravshipOrphanLevels.CleanupDuplicateLevels(newHost);
                StrataGravshipFootprintSnapshot.RebuildLinkedFloors(
                    newHost, null, landEngine, snapContents: false);
                return;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                travellingMapIds.Remove(maps[i].uniqueID);
            }
            pendingTakeoffEnginePos = IntVec3.Invalid;
            pendingTakeoffEngineThingId = -1;

            StrataGravshipStackUtility.RebindAll(maps, newHost);
            // Block deferred UpperDeckSync content snaps for the rest of this land.
            StrataGravshipPocketAlign.MarkLandAlignComplete();
            // Packed shafts should exist now — restore only truly missing ones.
            StrataGravshipPortalTravel.ReconnectOrRestore(
                newHost, maps, landEngine: landEngine, landShip: ship);
            // Paint destination deck before shifting furniture onto it.
            StrataGravshipFootprintSnapshot.PrePaintLinkedDecks(newHost, maps, landEngine);
            StrataGravshipPocketAlign.AlignPocketsToLandedShip(maps, newHost, takeoffPos, landEngine);
            StrataGravshipOrphanLevels.CleanupDuplicateLevels(newHost);
            // Align already snapped — rebuild paints/substructure only (no second shift).
            StrataGravshipFootprintSnapshot.RebuildLinkedFloors(
                newHost, maps, landEngine, snapContents: false);
            Log.Message($"[Strata] Gravship landing: rebound {maps.Count} linked level(s) to {newHost}"
                + (pinnedThingId >= 0 ? $" (engine thingID {pinnedThingId})." : "."));
            Messages.Message(
                "Strata_GravshipLevelsDocked".Translate(maps.Count),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
        }

        public void RebindOrphans(Map newHost)
        {
            CompleteLanding(ship: null, newHost);
        }

        private List<Map> CollectTravellingMaps()
        {
            var maps = new List<Map>();
            foreach (int id in travellingMapIds)
            {
                Map map = FindMapById(id);
                if (map != null)
                {
                    maps.Add(map);
                }
            }
            return maps;
        }

        private static void AddHostChildLevels(Map host, List<Map> maps)
        {
            if (host?.ChildPocketMaps == null)
            {
                return;
            }
            var seen = new HashSet<int>();
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    seen.Add(maps[i].uniqueID);
                }
            }
            foreach (Map child in host.ChildPocketMaps)
            {
                if (child == null || !seen.Add(child.uniqueID))
                {
                    continue;
                }
                if (StrataGravshipStackUtility.IsStrataLinkedLevel(child)
                    && StrataGravshipUtility.IsGravshipLinkedLevel(child))
                {
                    maps.Add(child);
                }
            }
            Map under = StrataGravshipOrphanLevels.FindAdoptableUnderdeck(host);
            if (under != null && seen.Add(under.uniqueID))
            {
                maps.Add(under);
            }
            Map upper = StrataGravshipOrphanLevels.FindAdoptableUpperDeck(host);
            if (upper != null && seen.Add(upper.uniqueID))
            {
                maps.Add(upper);
            }
        }

        public void UnregisterShip(Gravship ship)
        {
            TravellingStack stack = FindStack(ship);
            if (stack == null)
            {
                return;
            }
            for (int i = 0; i < stack.mapIds.Count; i++)
            {
                travellingMapIds.Remove(stack.mapIds[i]);
            }
            stacks.Remove(stack);
        }

        private TravellingStack FindStack(Gravship ship)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].ship == ship)
                {
                    return stacks[i];
                }
            }
            return null;
        }

        private static Map FindMapById(int uniqueId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == uniqueId)
                {
                    return maps[i];
                }
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref stacks, "strataGravshipStacks", LookMode.Deep);
            Scribe_Values.Look(ref pendingTakeoffEnginePos, "strataPendingTakeoffEnginePos", IntVec3.Invalid);
            Scribe_Values.Look(ref pendingTakeoffEngineThingId, "strataPendingTakeoffEngineThingId", -1);
            List<StrataGravshipPortalTravel.PortalSnapshot> portalSnaps = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                portalSnaps = StrataGravshipPortalTravel.TakeSnapshotsForSave();
            }
            Scribe_Collections.Look(ref portalSnaps, "strataGravshipPortalSnaps", LookMode.Deep);
            List<IntVec3> footprint = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                footprint = StrataGravshipFootprintSnapshot.TakeForSave();
            }
            Scribe_Collections.Look(ref footprint, "strataGravshipFootprintOffsets", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                travellingMapIds.Clear();
                if (stacks == null)
                {
                    stacks = new List<TravellingStack>();
                }
                for (int i = stacks.Count - 1; i >= 0; i--)
                {
                    if (stacks[i]?.mapIds == null)
                    {
                        stacks.RemoveAt(i);
                        continue;
                    }
                    for (int j = 0; j < stacks[i].mapIds.Count; j++)
                    {
                        travellingMapIds.Add(stacks[i].mapIds[j]);
                    }
                }
                StrataGravshipPortalTravel.RestoreSnapshotsFromSave(portalSnaps);
                StrataGravshipFootprintSnapshot.RestoreFromSave(footprint);
            }
        }

        private class TravellingStack : IExposable
        {
            public Gravship ship;
            public List<int> mapIds;
            public IntVec3 takeoffEnginePos = IntVec3.Invalid;
            public int takeoffEngineThingId = -1;

            public void ExposeData()
            {
                Scribe_References.Look(ref ship, "ship");
                Scribe_Collections.Look(ref mapIds, "mapIds", LookMode.Value);
                Scribe_Values.Look(ref takeoffEnginePos, "takeoffEnginePos", IntVec3.Invalid);
                Scribe_Values.Look(ref takeoffEngineThingId, "takeoffEngineThingId", -1);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && mapIds == null)
                {
                    mapIds = new List<int>();
                }
            }
        }
    }
}
