using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Odyssey only packs pawns standing on ValidSubstructure of the host map.
    // Prisoners/babies on a colony dig, or on the host map off the deck, are
    // abandoned with AbandonMap. Pull them onto the ship (or a travelling
    // underdeck) at takeoff so they leave with the stack.
    public static class StrataGravshipPawnRescue
    {
        public static void RescueAtTakeoff(Building_GravEngine engine)
        {
            if (engine?.Map == null || !StrataGravshipUtility.OdysseyActive)
            {
                return;
            }

            Map host = engine.Map;
            var travelling = new HashSet<Map>(StrataGravshipStackUtility.CollectTravellingLevels(engine));
            var toRescue = new List<Pawn>();

            CollectFromMap(host, engine, onlyOffSubstructure: true, toRescue);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(host))
            {
                Map map = link.map;
                if (map == null || travelling.Contains(map))
                {
                    continue;
                }
                // Non-travelling linked floors are left at the old site — pull
                // prisoners/babies off them before abandon.
                CollectFromMap(map, engine, onlyOffSubstructure: false, toRescue);
            }

            if (toRescue.Count == 0)
            {
                return;
            }

            int moved = 0;
            for (int i = 0; i < toRescue.Count; i++)
            {
                if (TryRelocateOntoShip(toRescue[i], engine, travelling))
                {
                    moved++;
                }
            }

            if (moved > 0)
            {
                Log.Message($"[Strata] Gravship takeoff: moved {moved} prisoner/baby pawn(s) onto the ship stack.");
                Messages.Message(
                    "Strata_GravshipRescuedPawns".Translate(moved),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false);
            }
        }

        public static void CollectLeftBehindWarnings(
            Building_GravEngine engine,
            List<Pawn> people,
            List<Pawn> animals)
        {
            if (engine?.Map == null)
            {
                return;
            }

            var travelling = new HashSet<Map>(StrataGravshipStackUtility.CollectTravellingLevels(engine));
            var seen = new HashSet<Pawn>(people);
            seen.UnionWith(animals);

            void Consider(Pawn pawn)
            {
                if (pawn == null || !seen.Add(pawn) || !WouldBeLeftBehind(pawn, engine, travelling))
                {
                    return;
                }
                if (pawn.RaceProps.Humanlike)
                {
                    people.Add(pawn);
                }
                else
                {
                    animals.Add(pawn);
                }
            }

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(engine.Map))
            {
                if (link.map == null || travelling.Contains(link.map))
                {
                    continue;
                }
                IReadOnlyList<Pawn> spawned = link.map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn pawn = spawned[i];
                    if (pawn.Faction == Faction.OfPlayer
                        || pawn.IsPrisonerOfColony
                        || pawn.IsSlaveOfColony)
                    {
                        Consider(pawn);
                    }
                }
            }
        }

        private static void CollectFromMap(
            Map map,
            Building_GravEngine engine,
            bool onlyOffSubstructure,
            List<Pawn> result)
        {
            if (map == null)
            {
                return;
            }

            IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn pawn = spawned[i];
                if (!ShouldRescue(pawn))
                {
                    continue;
                }
                if (onlyOffSubstructure
                    && map == engine.Map
                    && engine.ValidSubstructureAt(pawn.PositionHeld))
                {
                    continue;
                }
                if (!result.Contains(pawn))
                {
                    result.Add(pawn);
                }
            }
        }

        private static bool ShouldRescue(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }
            if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            {
                return true;
            }
            if (!ModsConfig.BiotechActive || !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer && !pawn.IsColonist)
            {
                return false;
            }
            return pawn.DevelopmentalStage.Baby();
        }

        private static bool WouldBeLeftBehind(
            Pawn pawn,
            Building_GravEngine engine,
            HashSet<Map> travelling)
        {
            if (pawn?.Map == null)
            {
                return false;
            }
            if (travelling.Contains(pawn.Map))
            {
                return false;
            }
            if (pawn.Map == engine.Map && engine.ValidSubstructureAt(pawn.PositionHeld))
            {
                return false;
            }
            return true;
        }

        private static bool TryRelocateOntoShip(
            Pawn pawn,
            Building_GravEngine engine,
            HashSet<Map> travelling)
        {
            if (pawn?.Map == null)
            {
                return false;
            }

            // Prefer a cell on the host deck so vanilla Gravship packing picks them up.
            // TryFindSpotOnGravship needs the pawn on the host map — find a deck cell ourselves.
            if (TryFindHostDeckCell(engine, out IntVec3 spot)
                && TeleportTo(pawn, engine.Map, spot))
            {
                return true;
            }

            // Fall back: any travelling underdeck/tower near a portal.
            foreach (Map level in travelling)
            {
                if (level == null || !Find.Maps.Contains(level))
                {
                    continue;
                }
                IntVec3 cell = FindCellNearPortal(level) ?? CellFinder.RandomClosewalkCellNear(level.Center, level, 12);
                if (cell.IsValid && TeleportTo(pawn, level, cell))
                {
                    return true;
                }
            }

            Log.Warning($"[Strata] Gravship takeoff: could not find a ship cell for {pawn.LabelShort}.");
            return false;
        }

        private static bool TryFindHostDeckCell(Building_GravEngine engine, out IntVec3 spot)
        {
            spot = IntVec3.Invalid;
            HashSet<IntVec3> cells = engine.ValidSubstructure;
            if (cells == null || cells.Count == 0)
            {
                cells = engine.AllConnectedSubstructure;
            }
            if (cells == null || cells.Count == 0 || engine.Map == null)
            {
                return false;
            }

            Map map = engine.Map;
            IntVec3 fallback = IntVec3.Invalid;
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                if (!fallback.IsValid)
                {
                    fallback = cell;
                }
                if (cell.Standable(map) && cell.GetFirstPawn(map) == null)
                {
                    spot = cell;
                    return true;
                }
            }

            if (fallback.IsValid)
            {
                IntVec3 near = CellFinder.RandomClosewalkCellNear(fallback, map, 6);
                spot = near.IsValid ? near : fallback;
                return spot.IsValid;
            }
            return false;
        }

        private static IntVec3? FindCellNearPortal(Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || !portal.Spawned)
                {
                    continue;
                }
                IntVec3 near = CellFinder.RandomClosewalkCellNear(portal.Position, map, 6);
                if (near.IsValid)
                {
                    return near;
                }
            }
            return null;
        }

        private static bool TeleportTo(Pawn pawn, Map map, IntVec3 cell)
        {
            if (pawn == null || map == null || !cell.IsValid)
            {
                return false;
            }

            IntVec3 stand = cell;
            if (!stand.Standable(map) || stand.GetFirstPawn(map) != null)
            {
                stand = CellFinder.RandomClosewalkCellNear(cell, map, 8);
            }
            if (!stand.IsValid)
            {
                return false;
            }

            if (pawn.Spawned)
            {
                if (pawn.Map == map)
                {
                    pawn.Position = stand;
                    pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
                    return true;
                }
                pawn.DeSpawn(DestroyMode.Vanish);
            }
            else if (pawn.holdingOwner != null)
            {
                pawn.holdingOwner.Remove(pawn);
            }

            GenSpawn.Spawn(pawn, stand, map, WipeMode.Vanish);
            pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
            return true;
        }
    }
}
