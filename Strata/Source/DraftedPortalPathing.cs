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
            FinalGoto = 2,
        }

        private class Route
        {
            public Map destMap;
            public IntVec3 destCell;
            public int returnPortalId;
            public Stage stage;
        }

        private static readonly Dictionary<int, Route> routes = new Dictionary<int, Route>();

        internal static void ResetSession()
        {
            routes.Clear();
        }

        public static bool HasDetour(Pawn pawn, IntVec3 destCell)
        {
            return TryFindDetour(pawn, destCell, out _, out _);
        }

        public static bool TryOrderDetour(Pawn pawn, IntVec3 destCell)
        {
            if (!TryFindDetour(pawn, destCell, out MapPortal exit, out MapPortal ret))
            {
                return false;
            }

            routes[pawn.thingIDNumber] = new Route
            {
                destMap = pawn.Map,
                destCell = destCell,
                returnPortalId = ret.thingIDNumber,
                stage = Stage.EnterFirst,
            };

            Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, exit);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        // Called from stair/elevator OnEntered after the pawn has spawned on
        // the far side. Continues a pending drafted bunker-bypass route.
        public static void NotifyPortalArrival(Pawn pawn)
        {
            if (pawn?.jobs == null || !routes.TryGetValue(pawn.thingIDNumber, out Route route))
            {
                return;
            }

            if (!pawn.Drafted || pawn.Downed || pawn.Dead || !pawn.Spawned)
            {
                routes.Remove(pawn.thingIDNumber);
                return;
            }

            if (route.stage == Stage.EnterFirst)
            {
                MapPortal ret = FindPortalById(pawn.Map, route.returnPortalId);
                if (ret == null || !ret.Spawned || !ret.IsEnterable(out _)
                    || StrataPortalUtility.IsSealedPortal(ret)
                    || LevelGraph.OtherMapSafe(ret) != route.destMap)
                {
                    routes.Remove(pawn.thingIDNumber);
                    return;
                }

                route.stage = Stage.CrossAndReturn;
                Job go = JobMaker.MakeJob(JobDefOf.Goto, ret);
                Job enter = JobMaker.MakeJob(JobDefOf.EnterPortal, ret);
                pawn.jobs.StartJob(go, JobCondition.InterruptForced);
                pawn.jobs.jobQueue.EnqueueFirst(enter, JobTag.Misc);
                return;
            }

            if (route.stage == Stage.CrossAndReturn)
            {
                routes.Remove(pawn.thingIDNumber);
                if (pawn.Map != route.destMap
                    || !pawn.CanReach(route.destCell, PathEndMode.OnCell, Danger.Deadly))
                {
                    return;
                }

                Job go = JobMaker.MakeJob(JobDefOf.Goto, route.destCell);
                pawn.jobs.StartJob(go, JobCondition.InterruptForced);
            }
        }

        private static bool TryFindDetour(
            Pawn pawn,
            IntVec3 destCell,
            out MapPortal bestExit,
            out MapPortal bestReturn)
        {
            bestExit = null;
            bestReturn = null;
            if (pawn?.Map == null || !pawn.Spawned || !pawn.Drafted || pawn.Downed
                || !destCell.IsValid || !destCell.InBounds(pawn.Map))
            {
                return false;
            }

            // Surface path already works — leave that to vanilla.
            if (pawn.CanReach(destCell, PathEndMode.OnCell, Danger.Deadly))
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
