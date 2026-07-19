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

        // Engine cell at InitiateTakeoff — used to 1:1-shift pockets on land.
        private IntVec3 pendingTakeoffEnginePos = IntVec3.Invalid;

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
            if (engine != null && engine.Position.IsValid)
            {
                pendingTakeoffEnginePos = engine.Position;
            }
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
            var stack = new TravellingStack
            {
                ship = ship,
                mapIds = new List<int>(),
                takeoffEnginePos = takeoffPos,
            };
            for (int i = 0; i < levels.Count; i++)
            {
                stack.mapIds.Add(levels[i].uniqueID);
            }
            stacks.Add(stack);
            Log.Message($"[Strata] Gravship takeoff: {levels.Count} linked level(s) will follow the ship.");
            Messages.Message(
                "Strata_GravshipLevelsTravel".Translate(levels.Count),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
        }

        public void CompleteLanding(Gravship ship, Map newHost)
        {
            if (ship == null || newHost == null)
            {
                return;
            }
            TravellingStack stack = FindStack(ship);
            if (stack == null)
            {
                // Already landed via another hook, or only MarkTravelling ran.
                if (travellingMapIds.Count > 0)
                {
                    RebindOrphans(newHost);
                }
                return;
            }
            var maps = new List<Map>();
            for (int i = 0; i < stack.mapIds.Count; i++)
            {
                Map map = FindMapById(stack.mapIds[i]);
                if (map != null)
                {
                    maps.Add(map);
                }
                travellingMapIds.Remove(stack.mapIds[i]);
            }
            IntVec3 takeoffPos = stack.takeoffEnginePos;
            stacks.Remove(stack);
            pendingTakeoffEnginePos = IntVec3.Invalid;
            if (maps.Count == 0)
            {
                return;
            }
            StrataGravshipStackUtility.RebindAll(maps, newHost);
            // Restore host shafts first so shaft-snap align can lock landings under them.
            StrataGravshipPortalTravel.ReconnectOrRestore(newHost, maps);
            StrataGravshipPocketAlign.AlignPocketsToLandedShip(maps, newHost, takeoffPos);
            Log.Message($"[Strata] Gravship landing: rebound {maps.Count} linked level(s) to {newHost}.");
            Messages.Message(
                "Strata_GravshipLevelsDocked".Translate(maps.Count),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
        }

        public void RebindOrphans(Map newHost)
        {
            if (newHost == null || travellingMapIds.Count == 0)
            {
                return;
            }
            var maps = new List<Map>();
            foreach (int id in new List<int>(travellingMapIds))
            {
                Map map = FindMapById(id);
                if (map != null)
                {
                    maps.Add(map);
                }
                travellingMapIds.Remove(id);
            }
            IntVec3 takeoffPos = pendingTakeoffEnginePos;
            stacks.Clear();
            pendingTakeoffEnginePos = IntVec3.Invalid;
            if (maps.Count == 0)
            {
                return;
            }
            StrataGravshipStackUtility.RebindAll(maps, newHost);
            StrataGravshipPortalTravel.ReconnectOrRestore(newHost, maps);
            StrataGravshipPocketAlign.AlignPocketsToLandedShip(maps, newHost, takeoffPos);
            Log.Message($"[Strata] Gravship landing: rebound {maps.Count} orphan travelling level(s) to {newHost}.");
            Messages.Message(
                "Strata_GravshipLevelsDocked".Translate(maps.Count),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
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
            List<StrataGravshipPortalTravel.PortalSnapshot> portalSnaps = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                portalSnaps = StrataGravshipPortalTravel.TakeSnapshotsForSave();
            }
            Scribe_Collections.Look(ref portalSnaps, "strataGravshipPortalSnaps", LookMode.Deep);
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
            }
        }

        private class TravellingStack : IExposable
        {
            public Gravship ship;
            public List<int> mapIds;
            public IntVec3 takeoffEnginePos = IntVec3.Invalid;

            public void ExposeData()
            {
                Scribe_References.Look(ref ship, "ship");
                Scribe_Collections.Look(ref mapIds, "mapIds", LookMode.Value);
                Scribe_Values.Look(ref takeoffEnginePos, "takeoffEnginePos", IntVec3.Invalid);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && mapIds == null)
                {
                    mapIds = new List<int>();
                }
            }
        }
    }
}
