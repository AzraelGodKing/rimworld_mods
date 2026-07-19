using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Takeoff: remember the ship silhouette as offsets from the grav engine.
    // Land: snap travelling contents under the host shaft/footprint, grow deck,
    // rebuild projected GravshipSubstructure, then clear empty orphan pads.
    public static class StrataGravshipFootprintSnapshot
    {
        private static readonly List<IntVec3> offsets = new List<IntVec3>();

        public static IReadOnlyList<IntVec3> PeekOffsets() => offsets;

        public static void Clear() => offsets.Clear();

        public static void Capture(Building_GravEngine engine)
        {
            offsets.Clear();
            if (engine?.Map == null || !engine.Spawned)
            {
                return;
            }
            HashSet<IntVec3> sub = engine.ValidSubstructure;
            if (sub == null || sub.Count == 0)
            {
                sub = engine.AllConnectedSubstructure;
            }
            if (sub == null || sub.Count == 0)
            {
                return;
            }
            IntVec3 origin = engine.Position;
            foreach (IntVec3 cell in sub)
            {
                offsets.Add(cell - origin);
            }
            Log.Message("[Strata] Gravship takeoff: snapshotted " + offsets.Count
                + " substructure cell(s) for linked-floor rebuild.");
        }

        public static List<IntVec3> TakeForSave() => new List<IntVec3>(offsets);

        public static void RestoreFromSave(List<IntVec3> saved)
        {
            offsets.Clear();
            if (saved != null)
            {
                offsets.AddRange(saved);
            }
        }

        public static void RebuildLinkedFloors(Map host, List<Map> pockets)
        {
            if (host == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(host);
            if (engine == null || !StrataGravshipUtility.EngineHasSubstructure(engine))
            {
                return;
            }

            var deckCells = BuildDeckCellSet(engine);
            if (deckCells.Count == 0)
            {
                return;
            }

            var targets = CollectTargetFloors(host, pockets);
            int snapped = 0;
            int painted = 0;
            int subCells = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Map pocket = targets[i];
                if (!StrataGravshipStackUtility.IsStrataLinkedLevel(pocket))
                {
                    continue;
                }

                // 1) Move the furnished room onto the ship footprint / under shaft
                //    BEFORE painting — otherwise we get an empty Gravship pad + old room.
                snapped += StrataGravshipPocketAlign.SnapContentsToHostFootprint(
                    pocket, host, deckCells);

                // 2) Grow deck onto footprint at the (now aligned) coords.
                painted += ApplyDeckFootprint(pocket, host, deckCells);
                GravshipDeckUtility.RestoreDeckUnderBuildings(pocket);
                if (StrataMapUtility.IsUpperLevel(pocket))
                {
                    UpperDeckUtility.RestoreRoofDeckUnderBuildings(pocket);
                }

                // 3) Projected GravshipSubstructure on every local deck cell.
                subCells += StrataGravshipSubstructureSync.RebuildOnLocalDeck(pocket, engine);

                // 4) Drop empty silhouette islands left from earlier bad lands.
                if (StrataMapUtility.IsUpperLevel(pocket))
                {
                    UpperDeckUtility.CleanupEmptySilhouetteIslands(pocket);
                }
                else
                {
                    GravshipDeckUtility.CleanupEmptySilhouetteIslands(pocket);
                }
            }

            StrataGravshipPortalTravel.RewireHostShafts(host, targets);

            Log.Message("[Strata] Gravship land rebuild: " + targets.Count + " floor(s), snapped "
                + snapped + " thing(s), " + painted + " deck cell(s), " + subCells
                + " projected substructure cell(s).");
        }

        private static List<Map> CollectTargetFloors(Map host, List<Map> pockets)
        {
            var targets = new List<Map>();
            var seen = new HashSet<Map>();
            void Add(Map map)
            {
                if (map != null && Find.Maps.Contains(map) && seen.Add(map))
                {
                    targets.Add(map);
                }
            }

            if (pockets != null)
            {
                for (int i = 0; i < pockets.Count; i++)
                {
                    Add(pockets[i]);
                }
            }
            Add(StrataGravshipOrphanLevels.FindAdoptableUnderdeck(host));
            Add(StrataGravshipOrphanLevels.FindAdoptableUpperDeck(host));
            foreach (Thing thing in host.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is IStrataGravshipPortal && thing is MapPortal portal
                    && portal.PocketMapExists)
                {
                    Add(portal.PocketMap);
                }
            }
            return targets;
        }

        private static HashSet<IntVec3> BuildDeckCellSet(Building_GravEngine engine)
        {
            var cells = new HashSet<IntVec3>();
            IntVec3 origin = engine.Position;
            Map host = engine.Map;

            for (int i = 0; i < offsets.Count; i++)
            {
                IntVec3 cell = origin + offsets[i];
                if (cell.InBounds(host))
                {
                    cells.Add(cell);
                }
            }

            HashSet<IntVec3> live = engine.ValidSubstructure;
            if (live == null || live.Count == 0)
            {
                live = engine.AllConnectedSubstructure;
            }
            if (live != null)
            {
                foreach (IntVec3 cell in live)
                {
                    if (cell.InBounds(host))
                    {
                        cells.Add(cell);
                    }
                }
            }
            return cells;
        }

        private static int ApplyDeckFootprint(Map pocket, Map host, HashSet<IntVec3> hostDeckCells)
        {
            if (pocket == null || host == null || hostDeckCells.Count == 0)
            {
                return 0;
            }

            bool upper = StrataMapUtility.IsUpperLevel(pocket);
            TerrainDef deck = upper ? UpperDeckUtility.RoofDeck : GravshipDeckUtility.DeckTerrain;
            bool sameSize = pocket.Size == host.Size;
            int painted = 0;

            foreach (IntVec3 hostCell in hostDeckCells)
            {
                IntVec3 pocketCell = sameSize
                    ? hostCell
                    : StrataMapUtility.ProportionalCell(hostCell, host, pocket);
                if (!pocketCell.InBounds(pocket))
                {
                    continue;
                }

                TerrainDef current = pocketCell.GetTerrain(pocket);
                bool alreadyDeck = upper
                    ? current?.defName == UpperDeckUtility.RoofDeckDefName
                    : current?.defName == GravshipDeckUtility.DeckDefName;
                if (alreadyDeck)
                {
                    painted++;
                    continue;
                }

                bool managedOff = upper
                    ? current?.defName == UpperDeckUtility.OpenSkyDefName
                    : current?.defName == GravshipDeckUtility.HullDefName;
                if (current == null || managedOff
                    || current.passability == Traversability.Impassable)
                {
                    pocket.terrainGrid.SetTerrain(pocketCell, deck);
                    if (!upper)
                    {
                        pocket.roofGrid.SetRoof(pocketCell, RoofDefOf.RoofConstructed);
                    }
                    painted++;
                }
            }
            return painted;
        }
    }
}
