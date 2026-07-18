using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Drafted click-to-move only sees the current map, so a sealed bunker with
    // its own stair (linked underground to an outside stair) reports "no path".
    // Jobs already hop portals; this mirrors that for player-ordered Goto:
    // down exit → cross the linked level → up a return stair that can reach
    // the click cell.
    public static class DraftedPortalPathing
    {
        private const int MaxExitCandidates = 12;
        private const int MaxReturnCandidates = 12;

        private enum Stage
        {
            EnterFirst = 0,
            CrossAndReturn = 1,
        }

        private enum FinishMode
        {
            GotoCell,
            LayDownBed,
        }

        private class Route
        {
            public Map destMap;
            public IntVec3 destCell;
            public int returnPortalId;
            public Stage stage;
            public FinishMode finish;
            public int bedId;
            public bool requireDrafted;
        }

        private static readonly Dictionary<int, Route> routes = new Dictionary<int, Route>();

        // EnterPortal calls ClearPrioritizedWorkAndJobQueue AFTER OnEntered, so
        // any job we start/queue in NotifyPortalArrival is wiped. Continue on
        // the next tick instead (same reason RaidPursuit defers enrollment).
        private static readonly List<int> pendingContinue = new List<int>();

        internal static void ResetSession()
        {
            routes.Clear();
            pendingContinue.Clear();
        }

        public static bool HasDetour(Pawn pawn, IntVec3 destCell)
        {
            return TryFindDetour(pawn, destCell, out _, out _);
        }

        // Float-menu Goto snaps unreachable clicks to the nearest walkable cell
        // (the wall). Recover a standable cell in the clicked region for detours.
        public static IntVec3 ResolveIntendedDest(Pawn pawn, IntVec3 clickCell, IntVec3 gotoLoc)
        {
            Map map = pawn?.Map;
            if (map == null)
            {
                return gotoLoc;
            }

            IntVec3 intended = clickCell.IsValid && clickCell.InBounds(map) ? clickCell : gotoLoc;
            if (!intended.Standable(map))
            {
                IntVec3 near = CellFinder.StandableCellNear(intended, map, 2.9f);
                if (near.IsValid)
                {
                    intended = near;
                }
            }

            return intended.IsValid ? intended : gotoLoc;
        }

        public static bool TryOrderDetour(Pawn pawn, IntVec3 destCell)
        {
            Job job = TryMakeDetourJob(
                pawn,
                destCell,
                FinishMode.GotoCell,
                bedId: -1,
                requireDrafted: true);
            if (job == null)
            {
                return false;
            }

            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        // Undrafted sleep: same-map bed behind walls, reachable only via linked stairs.
        public static Job TryMakeRestDetourJob(Pawn pawn, Building_Bed bed)
        {
            if (pawn == null || bed == null || !bed.Spawned || bed.Map != pawn.Map)
            {
                return null;
            }

            return TryMakeDetourJob(
                pawn,
                bed.Position,
                FinishMode.LayDownBed,
                bed.thingIDNumber,
                requireDrafted: false);
        }

        private static Job TryMakeDetourJob(
            Pawn pawn,
            IntVec3 destCell,
            FinishMode finish,
            int bedId,
            bool requireDrafted)
        {
            if (requireDrafted && !pawn.Drafted)
            {
                return null;
            }

            if (!TryFindDetour(pawn, destCell, out MapPortal exit, out MapPortal ret))
            {
                return null;
            }

            routes[pawn.thingIDNumber] = new Route
            {
                destMap = pawn.Map,
                destCell = destCell,
                returnPortalId = ret.thingIDNumber,
                stage = Stage.EnterFirst,
                finish = finish,
                bedId = bedId,
                requireDrafted = requireDrafted,
            };

            return MakeForcedJob(JobDefOf.EnterPortal, exit, pawn);
        }

        // Called from stair/elevator OnEntered — only schedules work; jobs start
        // next tick after EnterPortal finishes clearing the queue.
        public static void NotifyPortalArrival(Pawn pawn)
        {
            if (pawn == null || !routes.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            if (!pendingContinue.Contains(pawn.thingIDNumber))
            {
                pendingContinue.Add(pawn.thingIDNumber);
            }
        }

        internal static void Tick()
        {
            if (pendingContinue.Count == 0)
            {
                return;
            }

            for (int i = pendingContinue.Count - 1; i >= 0; i--)
            {
                int id = pendingContinue[i];
                pendingContinue.RemoveAt(i);
                ContinueRoute(id);
            }
        }

        private static void ContinueRoute(int pawnId)
        {
            if (!routes.TryGetValue(pawnId, out Route route))
            {
                return;
            }

            Pawn pawn = FindPawn(pawnId);
            if (pawn?.jobs == null || !pawn.Spawned || pawn.Downed || pawn.Dead
                || (route.requireDrafted && !pawn.Drafted))
            {
                routes.Remove(pawnId);
                return;
            }

            if (route.stage == Stage.EnterFirst)
            {
                MapPortal ret = FindPortalById(pawn.Map, route.returnPortalId);
                if (ret == null || !ret.Spawned || !ret.IsEnterable(out _)
                    || StrataPortalUtility.IsSealedPortal(ret)
                    || LevelGraph.OtherMapSafe(ret) != route.destMap)
                {
                    routes.Remove(pawnId);
                    return;
                }

                route.stage = Stage.CrossAndReturn;
                Job go = MakeForcedJob(JobDefOf.Goto, ret, pawn);
                Job enter = MakeForcedJob(JobDefOf.EnterPortal, ret, pawn);
                pawn.jobs.StartJob(go, JobCondition.InterruptForced);
                pawn.jobs.jobQueue.EnqueueFirst(enter, JobTag.Misc);
                return;
            }

            if (route.stage == Stage.CrossAndReturn)
            {
                routes.Remove(pawnId);
                if (pawn.Map != route.destMap)
                {
                    return;
                }

                if (route.finish == FinishMode.LayDownBed)
                {
                    Building_Bed bed = FindBedById(pawn.Map, route.bedId)
                        ?? pawn.ownership?.OwnedBed
                        ?? RestUtility.FindBedFor(pawn);
                    if (bed == null
                        || bed.Map != pawn.Map
                        || !RestUtility.CanUseBedNow(bed, pawn, checkSocialProperness: true)
                        || !pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
                    {
                        return;
                    }

                    pawn.jobs.StartJob(
                        JobMaker.MakeJob(JobDefOf.LayDown, bed),
                        JobCondition.InterruptForced);
                    return;
                }

                if (!ReachabilityUtility.CanReach(pawn, route.destCell, PathEndMode.OnCell, Danger.Deadly))
                {
                    return;
                }

                Job go = MakeForcedJob(JobDefOf.Goto, route.destCell, pawn);
                pawn.jobs.StartJob(go, JobCondition.InterruptForced);
            }
        }

        private static Building_Bed FindBedById(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is Building_Bed bed && bed.thingIDNumber == thingId)
                {
                    return bed;
                }
            }

            return null;
        }

        private static Job MakeForcedJob(JobDef def, LocalTargetInfo target, Pawn pawn)
        {
            Job job = JobMaker.MakeJob(def, target);
            job.playerForced = true;
            if (pawn.Drafted)
            {
                job.locomotionUrgency = LocomotionUrgency.Sprint;
            }

            return job;
        }

        private static Pawn FindPawn(int thingId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> pawns = maps[i].mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    if (pawns[j].thingIDNumber == thingId)
                    {
                        return pawns[j];
                    }
                }
            }

            return null;
        }

        private static bool TryFindDetour(
            Pawn pawn,
            IntVec3 destCell,
            out MapPortal bestExit,
            out MapPortal bestReturn)
        {
            bestExit = null;
            bestReturn = null;
            if (pawn?.Map == null || !pawn.Spawned || pawn.Downed
                || !destCell.IsValid || !destCell.InBounds(pawn.Map))
            {
                return false;
            }

            // Surface path already works — leave that to vanilla.
            if (ReachabilityUtility.CanReach(pawn, destCell, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }

            Map map = pawn.Map;
            if (!LevelGraph.AnyLinkFrom(map))
            {
                return false;
            }

            float bestScore = float.MaxValue;
            int exitsChecked = 0;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal exit || !exit.Spawned)
                {
                    continue;
                }

                if (StrataPortalUtility.IsSealedPortal(exit) || !exit.IsEnterable(out _))
                {
                    continue;
                }

                Map mid = LevelGraph.OtherMapSafe(exit);
                if (mid == null)
                {
                    continue;
                }

                if (!pawn.CanReach(exit, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                IntVec3 midArrive = exit.GetDestinationLocation();
                if (!midArrive.IsValid || !midArrive.InBounds(mid))
                {
                    continue;
                }

                if (++exitsChecked > MaxExitCandidates)
                {
                    break;
                }

                int returnsChecked = 0;
                foreach (Thing midThing in mid.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (midThing is not MapPortal ret || !ret.Spawned || ret == exit)
                    {
                        continue;
                    }

                    if (StrataPortalUtility.IsSealedPortal(ret) || !ret.IsEnterable(out _))
                    {
                        continue;
                    }

                    // Same destination map (the bunker bypass case): return
                    // stair lands somewhere that can walk to the click cell.
                    if (LevelGraph.OtherMapSafe(ret) != map)
                    {
                        continue;
                    }

                    IntVec3 backArrive = ret.GetDestinationLocation();
                    if (!backArrive.IsValid || !backArrive.InBounds(map))
                    {
                        continue;
                    }

                    if (!map.reachability.CanReach(
                            backArrive,
                            destCell,
                            PathEndMode.OnCell,
                            TraverseParms.For(TraverseMode.PassDoors)))
                    {
                        continue;
                    }

                    if (!mid.reachability.CanReach(
                            midArrive,
                            ret,
                            PathEndMode.Touch,
                            TraverseParms.For(TraverseMode.PassDoors)))
                    {
                        continue;
                    }

                    if (++returnsChecked > MaxReturnCandidates)
                    {
                        break;
                    }

                    float score = pawn.Position.DistanceTo(exit.Position)
                        + midArrive.DistanceTo(ret.Position)
                        + backArrive.DistanceTo(destCell);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestExit = exit;
                        bestReturn = ret;
                    }
                }
            }

            return bestExit != null && bestReturn != null;
        }

        private static MapPortal FindPortalById(Map map, int thingId)
        {
            if (map == null)
            {
                return null;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && portal.thingIDNumber == thingId)
                {
                    return portal;
                }
            }

            return null;
        }
    }
}
