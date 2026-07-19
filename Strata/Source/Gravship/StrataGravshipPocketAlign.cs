using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Strata
{
    // Keeps gravship-linked pocket maps 1:1 with the ship when it moves:
    // shift pocket contents by (landEngine - takeoffEngine), then repaint decks.
    public static class StrataGravshipPocketAlign
    {
        public static void AlignPocketsToLandedShip(
            List<Map> pockets,
            Map newHost,
            IntVec3 takeoffEnginePos)
        {
            if (pockets == null || newHost == null || !takeoffEnginePos.IsValid)
            {
                return;
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngineOnMap(newHost);
            if (engine == null || !engine.Spawned)
            {
                return;
            }

            IntVec3 delta = engine.Position - takeoffEnginePos;
            if (delta == IntVec3.Zero)
            {
                RepaintAll(pockets, newHost, engine);
                return;
            }

            int shifted = 0;
            for (int i = 0; i < pockets.Count; i++)
            {
                Map pocket = pockets[i];
                if (pocket == null || !Find.Maps.Contains(pocket))
                {
                    continue;
                }
                shifted += TranslateMapContents(pocket, delta);
            }

            RepaintAll(pockets, newHost, engine);

            Log.Message("[Strata] Gravship land align: delta " + delta
                + ", moved " + shifted + " thing(s) on "
                + pockets.Count + " linked level(s).");
        }

        private static void RepaintAll(List<Map> pockets, Map host, Building_GravEngine engine)
        {
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
                        ceilingOnDeck: true);
                }
                StrataGravshipSubstructureSync.SyncMap(pocket, host, engine);
            }
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
                // Skip natural rock / filth that paint will replace; keep player stuff + pawns.
                if (thing.def.category == ThingCategory.Ethereal && thing is not Blueprint && thing is not Frame)
                {
                    continue;
                }

                IntVec3 dest = thing.Position + delta;
                moves.Add((thing, dest, thing.Rotation));
            }

            // Despawn largest buildings first so multi-cell clears cleanly.
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
            // Respawn small things first (floors/substructure), then buildings, then pawns.
            moves.Sort((a, b) => SpawnOrder(a.thing).CompareTo(SpawnOrder(b.thing)));

            for (int i = 0; i < moves.Count; i++)
            {
                (Thing thing, IntVec3 dest, Rot4 rot) = moves[i];
                if (thing.Destroyed || thing.Spawned)
                {
                    continue;
                }

                if (!TrySpawnNear(thing, dest, map, rot))
                {
                    // Last resort: drop at map center so contents are not lost mid-flight.
                    if (!TrySpawnNear(thing, map.Center, map, rot))
                    {
                        Log.Warning("[Strata] Gravship align: could not place "
                            + thing.LabelCap + " after shift " + delta);
                        continue;
                    }
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

        private static bool TrySpawnNear(Thing thing, IntVec3 dest, Map map, Rot4 rot)
        {
            if (dest.InBounds(map) && CanAccept(thing, dest, map, rot))
            {
                return GenSpawn.Spawn(thing, dest, map, rot) != null;
            }

            if (CellFinder.TryFindRandomCellNear(
                    dest.ClampInsideMap(map),
                    map,
                    8,
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
            CellRect rect = GenAdj.OccupiedRect(cell, rot, thing.def.Size);
            if (!rect.InBounds(map))
            {
                return false;
            }
            // Allow wipe of projected substructure / filth; GenSpawn VanishOrMoveAside handles rest.
            return true;
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
                    // Thing designations move with the thing.
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
