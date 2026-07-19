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
            int checkedCells = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                TerrainDef terrain = cell.GetTerrain(map);
                if (GravshipDeckUtility.IsManagedDeckTerrain(terrain)
                    || UpperDeckUtility.IsManagedUpperTerrain(terrain))
                {
                    return true;
                }
                if (++checkedCells > 2048)
                {
                    break;
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
