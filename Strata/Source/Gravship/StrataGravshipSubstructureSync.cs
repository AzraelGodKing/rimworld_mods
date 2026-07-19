using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Strata
{
    // Tracks and spawns projected GravshipSubstructure on linked A+/B+ maps.
    public class MapComponent_StrataProjectedSubstructure : MapComponent
    {
        private HashSet<IntVec3> projectedCells = new HashSet<IntVec3>();

        public MapComponent_StrataProjectedSubstructure(Map map) : base(map)
        {
        }

        public bool IsProjected(IntVec3 cell) => projectedCells.Contains(cell);

        public void MarkProjected(IntVec3 cell) => projectedCells.Add(cell);

        public void UnmarkProjected(IntVec3 cell) => projectedCells.Remove(cell);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref projectedCells, "strataProjectedSubstructure", LookMode.Value);
            projectedCells ??= new HashSet<IntVec3>();
        }
    }

    public static class StrataGravshipSubstructureSync
    {
        public const string SubstructureDefName = "GravshipSubstructure";

        private static ThingDef substructureDef;

        public static ThingDef SubstructureDef =>
            substructureDef ??= DefDatabase<ThingDef>.GetNamedSilentFail(SubstructureDefName);

        public static bool HasSubstructure(Map map, IntVec3 cell)
        {
            return SubstructureAt(map, cell) != null;
        }

        public static Thing SubstructureAt(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return null;
            }
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing?.def?.defName == SubstructureDefName && !thing.Destroyed)
                {
                    return thing;
                }
            }
            return null;
        }

        public static void SyncAllLinkedFromHost(Map host)
        {
            if (host == null || !StrataGravshipUtility.OdysseyActive || SubstructureDef == null)
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
            if (engine == null)
            {
                return;
            }
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not IStrataGravshipPortal || thing is not MapPortal portal
                    || !portal.PocketMapExists)
                {
                    continue;
                }
                SyncMap(portal.PocketMap, host, engine);
            }
            engine.ForceSubstructureDirty();
        }

        public static void SyncMap(Map pocket, Map host, Building_GravEngine engine)
        {
            if (pocket == null || host == null || engine == null || SubstructureDef == null
                || !StrataGravshipUtility.IsGravshipLinkedLevel(pocket))
            {
                return;
            }
            // Empty host set would destroy every projected GravshipSubstructure tile.
            if (!StrataGravshipUtility.EngineHasSubstructure(engine))
            {
                return;
            }

            var tracker = GetOrAddTracker(pocket);
            bool upper = StrataMapUtility.IsUpperLevel(pocket);
            foreach (IntVec3 cell in pocket.AllCells)
            {
                bool onDeck = upper
                    ? UpperDeckUtility.SourceSupportsUpperDeck(pocket, cell, host)
                    : HostSubstructureProjectsTo(pocket, host, cell);
                // Also keep anything that is already local walkable deck after a
                // footprint rebuild (grow-only may leave travelling deck offline).
                if (!onDeck)
                {
                    onDeck = IsLocalWalkableDeck(pocket, cell);
                }
                if (onDeck)
                {
                    EnsureProjectedSubstructure(pocket, cell, tracker);
                }
                else
                {
                    RemoveProjectedSubstructure(pocket, cell, tracker);
                }
            }
        }

        // Land rebuild: place/mark GravshipSubstructure on every local deck cell
        // (empty cells get the Thing; occupied cells stay marked for ValidSubstructureAt).
        public static int RebuildOnLocalDeck(Map pocket, Building_GravEngine engine)
        {
            if (pocket == null || engine == null || SubstructureDef == null)
            {
                return 0;
            }
            var tracker = GetOrAddTracker(pocket);
            int count = 0;
            foreach (IntVec3 cell in pocket.AllCells)
            {
                if (!IsLocalWalkableDeck(pocket, cell)
                    && !StrataGravshipUtility.IsLinkedDeckCell(pocket, cell, engine))
                {
                    continue;
                }
                EnsureProjectedSubstructure(pocket, cell, tracker, markWhenBlocked: true);
                if (tracker.IsProjected(cell) || SubstructureAt(pocket, cell) != null)
                {
                    count++;
                }
            }
            engine.ForceSubstructureDirty();
            return count;
        }

        private static MapComponent_StrataProjectedSubstructure GetOrAddTracker(Map pocket)
        {
            var tracker = pocket.GetComponent<MapComponent_StrataProjectedSubstructure>();
            if (tracker == null)
            {
                tracker = new MapComponent_StrataProjectedSubstructure(pocket);
                pocket.components.Add(tracker);
            }
            return tracker;
        }

        private static bool IsLocalWalkableDeck(Map pocket, IntVec3 cell)
        {
            if (StrataMapUtility.IsUpperLevel(pocket))
            {
                return cell.GetTerrain(pocket)?.defName == UpperDeckUtility.RoofDeckDefName;
            }
            return GravshipDeckUtility.IsWalkableDeckCell(pocket, cell);
        }

        private static bool HostSubstructureProjectsTo(Map pocket, Map host, IntVec3 pocketCell)
        {
            if (host == null || pocket == null)
            {
                return false;
            }
            IntVec3 hostCell = pocket.Size == host.Size
                ? pocketCell
                : StrataMapUtility.ProportionalCell(pocketCell, pocket, host);
            return StrataGravshipUtility.CellOnGravship(host, hostCell);
        }

        private static void EnsureProjectedSubstructure(
            Map map,
            IntVec3 cell,
            MapComponent_StrataProjectedSubstructure tracker,
            bool markWhenBlocked = false)
        {
            if (SubstructureAt(map, cell) != null)
            {
                tracker.MarkProjected(cell);
                return;
            }
            if (!cell.InBounds(map))
            {
                return;
            }
            bool blocked = false;
            foreach (Thing blocker in cell.GetThingList(map).ToList())
            {
                if (blocker.def.category == ThingCategory.Building
                    && blocker.def.defName != SubstructureDefName
                    && !blocker.def.IsBlueprint && !blocker.def.IsFrame)
                {
                    blocked = true;
                    break;
                }
            }
            if (blocked)
            {
                // Furniture/walls sit on the deck — still count as substructure for
                // Odyssey placement checks via IsLinkedDeckCell / tracker.
                if (markWhenBlocked)
                {
                    tracker.MarkProjected(cell);
                }
                return;
            }
            // Do not create fresh deck terrain here — that seeds empty silhouette
            // islands when land Paint was skipped. Terrain comes from
            // GravshipDeckUtility paint / the travelling underdeck.
            if (!GravshipDeckUtility.IsWalkableDeckCell(map, cell)
                && cell.GetTerrain(map)?.defName != UpperDeckUtility.RoofDeckDefName)
            {
                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain == null || !terrain.affordances.Contains(TerrainAffordanceDefOf.Walkable))
                {
                    return;
                }
            }
            Thing thing = ThingMaker.MakeThing(SubstructureDef);
            thing.SetFactionDirect(Faction.OfPlayer);
            if (GenSpawn.Spawn(thing, cell, map, WipeMode.Vanish) != null)
            {
                tracker.MarkProjected(cell);
            }
        }

        private static void RemoveProjectedSubstructure(
            Map map,
            IntVec3 cell,
            MapComponent_StrataProjectedSubstructure tracker)
        {
            if (!tracker.IsProjected(cell))
            {
                return;
            }
            Thing sub = SubstructureAt(map, cell);
            if (sub != null && !sub.def.IsBlueprint && !sub.def.IsFrame)
            {
                sub.Destroy(DestroyMode.Vanish);
            }
            tracker.UnmarkProjected(cell);
        }

        public static bool CanPlaceSubstructureOn(Map map, IntVec3 cell, Building_GravEngine engine)
        {
            if (map == null || engine == null || !cell.InBounds(map))
            {
                return false;
            }
            if (map == engine.Map)
            {
                return false;
            }
            if (!StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return false;
            }
            if (StrataGravshipUtility.IsLinkedDeckCell(map, cell, engine))
            {
                return true;
            }
            foreach (IntVec3 adjacent in GenAdj.AdjacentCells)
            {
                IntVec3 next = cell + adjacent;
                if (next.InBounds(map) && HasSubstructure(map, next))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
