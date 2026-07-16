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

            var tracker = pocket.GetComponent<MapComponent_StrataProjectedSubstructure>();
            if (tracker == null)
            {
                tracker = new MapComponent_StrataProjectedSubstructure(pocket);
                pocket.components.Add(tracker);
            }

            bool upper = StrataMapUtility.IsUpperLevel(pocket);
            foreach (IntVec3 cell in pocket.AllCells)
            {
                bool onDeck = upper
                    ? UpperDeckUtility.SourceSupportsUpperDeck(pocket, cell, host)
                    : HostSubstructureProjectsTo(pocket, host, cell);
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
            MapComponent_StrataProjectedSubstructure tracker)
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
            foreach (Thing blocker in cell.GetThingList(map).ToList())
            {
                if (blocker.def.category == ThingCategory.Building
                    && blocker.def.defName != SubstructureDefName
                    && !blocker.def.IsBlueprint && !blocker.def.IsFrame)
                {
                    return;
                }
            }
            EnsureWalkableDeckTerrain(map, cell);
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

        private static void EnsureWalkableDeckTerrain(Map map, IntVec3 cell)
        {
            TerrainDef deck = StrataMapUtility.IsUpperLevel(map)
                ? UpperDeckUtility.RoofDeck
                : GravshipDeckUtility.DeckTerrain;
            TerrainDef current = cell.GetTerrain(map);
            if (current == null || !current.affordances.Contains(TerrainAffordanceDefOf.Walkable))
            {
                map.terrainGrid.SetTerrain(cell, deck);
            }
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
