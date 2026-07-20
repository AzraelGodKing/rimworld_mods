using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // After a bad land, travelling underdecks can sit unbound while an unwired
    // shaft would otherwise GeneratePocketMap a second empty "New" floor.
    public static class StrataGravshipOrphanLevels
    {
        public static Map FindAdoptableUnderdeck(Map host)
        {
            return FindAdoptable(host, wantUpper: false);
        }

        public static Map FindAdoptableUpperDeck(Map host)
        {
            return FindAdoptable(host, wantUpper: true);
        }

        public static Map FindMapById(int uniqueId)
        {
            if (uniqueId < 0)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == uniqueId)
                {
                    return maps[i];
                }
            }
            return null;
        }

        // Prefer the populated travelling floor; never generate a second empty one.
        private static Map FindAdoptable(Map host, bool wantUpper)
        {
            if (host == null)
            {
                return null;
            }

            Map best = null;
            int bestScore = int.MinValue;

            void Consider(Map map)
            {
                if (map == null || !Find.Maps.Contains(map))
                {
                    return;
                }
                if (wantUpper)
                {
                    if (!StrataMapUtility.IsUpperLevel(map))
                    {
                        return;
                    }
                }
                else if (!StrataMapUtility.IsUnderground(map))
                {
                    return;
                }
                if (!LooksLikeGravshipLinkedLevel(map, host))
                {
                    return;
                }
                int score = Score(map);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = map;
                }
            }

            if (host.ChildPocketMaps != null)
            {
                foreach (Map child in host.ChildPocketMaps)
                {
                    Consider(child);
                }
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.Parent is not PocketMapParent pocket)
                {
                    continue;
                }
                if (pocket.sourceMap != null && pocket.sourceMap != host)
                {
                    continue;
                }
                Consider(map);
            }

            // Takeoff snapshots remember the exact pocket that belonged to a shaft.
            foreach (StrataGravshipPortalTravel.PortalSnapshot snap
                in StrataGravshipPortalTravel.PeekSnapshots())
            {
                if (snap == null || snap.isTower != wantUpper)
                {
                    continue;
                }
                Consider(FindMapById(snap.pocketMapId));
            }

            return best;
        }

        // Adopt a detached travelling floor for an unwired gravship shaft
        // instead of generating a fresh empty level. Called from the gravship
        // shaft GeneratePocketMapInt overrides (currentlyGeneratingPortal ==
        // shaft there, so a freshly spawned landing self-wires). Returns the
        // adopted map, or null when there is nothing suitable to adopt.
        public static Map TryAdoptOrphanFor(Building_StairsDown shaft, bool wantUpper)
        {
            if (shaft?.Map == null)
            {
                return null;
            }
            Map orphan = wantUpper
                ? FindAdoptableUpperDeck(shaft.Map)
                : FindAdoptableUnderdeck(shaft.Map);
            if (orphan == null)
            {
                return null;
            }

            // Never hijack a level another live shaft still owns — re-pointing
            // its landing would break that shaft's return trip. Multi-shaft
            // sharing goes through ExistingGravshipLevel* (second landing).
            foreach (Thing thing in shaft.Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing != shaft && thing is MapPortal other && other.Spawned
                    && other.PocketMapExists && other.PocketMap == orphan)
                {
                    return null;
                }
            }

            MapPortal landing = StrataGravshipPortalTravel.FindLandingOn(orphan);
            if (landing is PocketMapExit exitLanding)
            {
                exitLanding.entrance = shaft;
                shaft.exit = exitLanding;
                if (orphan.Parent is PocketMapParent parent)
                {
                    parent.sourceMap = shaft.Map;
                }
                Log.Message("[Strata] Gravship shaft adopted orphaned "
                    + (wantUpper ? "upper deck" : "underdeck") + " " + orphan.uniqueID + ".");
                return orphan;
            }

            IntVec3 cell = shaft.FindLandingCell(orphan);
            if (cell.IsValid)
            {
                StrataPortalUtility.SpawnLanding(shaft.def.portal.exitDef, cell, orphan);
                if (orphan.Parent is PocketMapParent parent)
                {
                    parent.sourceMap = shaft.Map;
                }
                Log.Message("[Strata] Gravship shaft adopted orphaned "
                    + (wantUpper ? "upper deck" : "underdeck") + " " + orphan.uniqueID
                    + " (spawned new landing).");
                return orphan;
            }

            return null;
        }

        // True when this host carries gravship relink damage worth repairing on
        // load: a detached level that still looks gravship-linked. An unwired
        // shaft alone is NOT damage — freshly built, never-used shafts have no
        // pocket either, and flagging them would rerun repair on every load.
        public static bool HasRepairWork(Map host)
        {
            if (host == null)
            {
                return false;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.Parent is not PocketMapParent pocket || pocket.sourceMap != null)
                {
                    continue;
                }
                if (!StrataMapUtility.IsUnderground(map) && !StrataMapUtility.IsUpperLevel(map))
                {
                    continue;
                }
                if (LooksLikeGravshipLinkedLevel(map, host))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LooksLikeGravshipLinkedLevel(Map map, Map host)
        {
            if (StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return true;
            }
            if (map.Parent is PocketMapParent pocket
                && (pocket.sourceMap == host || pocket.sourceMap == null))
            {
                // Travelling underdecks detach sourceMap at takeoff; still adopt
                // when they carry gravship deck terrain or projected substructure.
                if (HasGravshipDeckTerrain(map))
                {
                    return true;
                }
                ThingDef subDef = StrataGravshipSubstructureSync.SubstructureDef;
                return subDef != null
                    && map.listerThings != null
                    && map.listerThings.ThingsOfDef(subDef).Count > 0;
            }
            return false;
        }

        private static bool HasGravshipDeckTerrain(Map map)
        {
            if (map?.terrainGrid == null)
            {
                return false;
            }
            // Stride-sample the whole grid (~16k probes ≈ every 4th cell on a
            // 250x250 map) — scanning only the first 2048 cells of AllCells
            // covers a few edge rows and misses centrally-placed deck terrain,
            // wrongly rejecting real orphans; a sparse stride could still slip
            // past a small deck footprint.
            int total = map.cellIndices.NumGridCells;
            int stride = total > 16384 ? total / 16384 : 1;
            for (int i = 0; i < total; i += stride)
            {
                TerrainDef terrain = map.terrainGrid.TerrainAt(i);
                if (GravshipDeckUtility.IsManagedDeckTerrain(terrain)
                    || UpperDeckUtility.IsManagedUpperTerrain(terrain))
                {
                    return true;
                }
            }
            return false;
        }

        private static int Score(Map map)
        {
            int score = 0;
            if (map.mapPawns != null)
            {
                score += map.mapPawns.AllPawnsSpawned.Count * 1000;
            }
            if (map.listerBuildings != null)
            {
                score += map.listerBuildings.allBuildingsColonist.Count * 10;
            }
            // Prefer floors that already have a landing pad.
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (StrataGravshipUtility.IsGravshipLanding(thing))
                {
                    score += 500;
                    break;
                }
            }
            return score;
        }

        // After land: if an empty duplicate underdeck was spawned, move survivors
        // onto the kept floor and destroy the empty one.
        public static void CleanupDuplicateLevels(Map host)
        {
            if (host == null)
            {
                return;
            }
            CleanupDuplicates(host, wantUpper: false);
            CleanupDuplicates(host, wantUpper: true);
        }

        private static void CleanupDuplicates(Map host, bool wantUpper)
        {
            var levels = new List<Map>();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null)
                {
                    continue;
                }
                if (wantUpper ? !StrataMapUtility.IsUpperLevel(map) : !StrataMapUtility.IsUnderground(map))
                {
                    continue;
                }
                if (!LooksLikeGravshipLinkedLevel(map, host))
                {
                    continue;
                }
                if (map.Parent is PocketMapParent pocket
                    && pocket.sourceMap != null
                    && pocket.sourceMap != host)
                {
                    continue;
                }
                levels.Add(map);
            }

            if (levels.Count <= 1)
            {
                return;
            }

            levels.Sort((a, b) => Score(b).CompareTo(Score(a)));
            Map keep = levels[0];
            for (int i = 1; i < levels.Count; i++)
            {
                Map discard = levels[i];
                if (Score(discard) >= 500)
                {
                    // Still has a landing / real content — leave it; may be a
                    // second intentional shaft stack.
                    continue;
                }
                RelocatePawns(discard, keep);
                Log.Message("[Strata] Gravship land: removing empty duplicate "
                    + (wantUpper ? "upper deck" : "underdeck")
                    + " " + discard.uniqueID + "; kept " + keep.uniqueID + ".");
                PocketMapUtility.DestroyPocketMap(discard);
            }
        }

        private static void RelocatePawns(Map from, Map to)
        {
            if (from?.mapPawns == null || to == null)
            {
                return;
            }
            List<Pawn> pawns = new List<Pawn>(from.mapPawns.AllPawnsSpawned);
            MapPortal landing = null;
            foreach (Thing thing in to.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (StrataGravshipUtility.IsGravshipLanding(thing) && thing is MapPortal portal)
                {
                    landing = portal;
                    break;
                }
            }
            IntVec3 dest = landing != null
                ? CellFinder.RandomClosewalkCellNear(landing.Position, to, 6)
                : CellFinder.RandomClosewalkCellNear(to.Center, to, 12);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed)
                {
                    continue;
                }
                if (pawn.Spawned)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                }
                if (!dest.IsValid)
                {
                    dest = to.Center;
                }
                GenSpawn.Spawn(pawn, dest, to, WipeMode.Vanish);
                pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
            }
        }
    }
}
