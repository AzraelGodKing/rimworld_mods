using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Niceties
{
    internal struct OpenBuildsResult
    {
        public bool EnclosesThings;
        public bool EnclosesSelf;
    }

    /// <summary>
    /// Pawns skip finishing an impassable building that would trap someone or
    /// block leftover frames. They step aside before closing themselves in.
    /// Replace Stuff frames count as unfinished walls; a finished wall on the
    /// same cell stays solid so in-place freezer swaps still work.
    /// </summary>
    internal static class OpenBuilds
    {
        internal const string SmarterConstructionId = "dhultgren.smarterconstruction";
        private const int MaxRegionSize = 80;
        private const int CacheTicks = 15;

        private static bool scChecked;
        private static bool scLoaded;
        private static int cacheTick;
        private static int cacheThingId = -1;
        private static int cachePawnId = -1;
        private static OpenBuildsResult cacheResult;

        internal static bool Enabled
        {
            get
            {
                if (NicetiesMod.Settings == null || !NicetiesMod.Settings.enableLeaveAWayOut)
                {
                    return false;
                }

                return !SmarterConstructionLoaded;
            }
        }

        internal static bool SmarterConstructionLoaded
        {
            get
            {
                if (!scChecked)
                {
                    scChecked = true;
                    scLoaded = ModLister.GetActiveModWithIdentifier(SmarterConstructionId, ignorePostfix: true) != null;
                }

                return scLoaded;
            }
        }

        internal static bool IsImpassableBuild(Thing t)
        {
            return t != null
                && t.def != null
                && t.def.entityDefToBuild != null
                && t.def.entityDefToBuild.passability == Traversability.Impassable;
        }

        internal static OpenBuildsResult WouldEnclose(Thing target, Pawn pawn)
        {
            OpenBuildsResult empty = new OpenBuildsResult();
            if (target == null || target.Map == null)
            {
                return empty;
            }

            int tick = Find.TickManager.TicksGame;
            if (cacheThingId == target.thingIDNumber
                && cachePawnId == (pawn != null ? pawn.thingIDNumber : -1)
                && tick - cacheTick < CacheTicks)
            {
                return cacheResult;
            }

            OpenBuildsResult result = Compute(target, pawn);
            cacheTick = tick;
            cacheThingId = target.thingIDNumber;
            cachePawnId = pawn != null ? pawn.thingIDNumber : -1;
            cacheResult = result;
            return result;
        }

        internal static HashSet<IntVec3> FindSafeSpots(Thing target)
        {
            HashSet<IntVec3> spots = new HashSet<IntVec3>();
            if (target?.Map == null)
            {
                return spots;
            }

            HashSet<IntVec3> blockers = Occupied(target);
            HashSet<IntVec3> enclosed = ClosedRegion(target.Map, blockers);
            foreach (IntVec3 cell in CardinalNeighbors(blockers))
            {
                if (enclosed.Contains(cell))
                {
                    continue;
                }

                if (WalkableForEnclose(target.Map, cell, blockers))
                {
                    spots.Add(cell);
                }
            }

            return spots;
        }

        internal static bool StepAsideThenRetry(Pawn pawn, Frame frame)
        {
            if (pawn?.jobs == null || frame == null)
            {
                return false;
            }

            HashSet<IntVec3> spots = FindSafeSpots(frame);
            if (spots.Count == 0)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

            if (spots.Contains(pawn.Position))
            {
                return false;
            }

            IntVec3 dest = default(IntVec3);
            foreach (IntVec3 cell in spots)
            {
                dest = cell;
                break;
            }

            if (pawn.CurJob == null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

            Job resume = JobMaker.MakeJob(pawn.CurJob.def, frame);
            pawn.jobs.jobQueue.EnqueueFirst(resume);
            Job walk = JobMaker.MakeJob(JobDefOf.Goto, dest);
            walk.ignoreForbidden = true;
            pawn.jobs.StartJob(walk, JobCondition.InterruptForced);
            return true;
        }

        internal static bool IsConstructionFrame(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            if (thing is Frame)
            {
                return true;
            }

            return thing.def != null && thing.def.IsFrame;
        }

        private static OpenBuildsResult Compute(Thing target, Pawn pawn)
        {
            OpenBuildsResult result = new OpenBuildsResult();
            HashSet<IntVec3> blockers = Occupied(target);
            HashSet<IntVec3> enclosed = ClosedRegion(target.Map, blockers);
            if (enclosed.Count == 0)
            {
                return result;
            }

            foreach (IntVec3 cell in enclosed)
            {
                List<Thing> things = cell.GetThingList(target.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing is Blueprint || IsConstructionFrame(thing))
                    {
                        result.EnclosesThings = true;
                    }

                    Pawn other = thing as Pawn;
                    if (other == null || other.Faction == null || !other.Faction.IsPlayer)
                    {
                        continue;
                    }

                    if (other == pawn)
                    {
                        result.EnclosesSelf = true;
                    }
                    else
                    {
                        result.EnclosesThings = true;
                    }
                }
            }

            return result;
        }

        private static HashSet<IntVec3> ClosedRegion(Map map, HashSet<IntVec3> blockers)
        {
            HashSet<IntVec3> closed = new HashSet<IntVec3>();
            HashSet<IntVec3> seen = new HashSet<IntVec3>();
            foreach (IntVec3 start in CardinalNeighbors(blockers))
            {
                if (!seen.Add(start))
                {
                    continue;
                }

                HashSet<IntVec3> fill = Flood(map, start, blockers);
                foreach (IntVec3 cell in fill)
                {
                    seen.Add(cell);
                }

                if (fill.Count > 0 && fill.Count < MaxRegionSize)
                {
                    closed.UnionWith(fill);
                }
            }

            return closed;
        }

        private static HashSet<IntVec3> Flood(Map map, IntVec3 start, HashSet<IntVec3> blockers)
        {
            HashSet<IntVec3> fill = new HashSet<IntVec3>();
            if (!WalkableForEnclose(map, start, blockers))
            {
                return fill;
            }

            Queue<IntVec3> queue = new Queue<IntVec3>();
            queue.Enqueue(start);
            while (queue.Count > 0 && fill.Count < MaxRegionSize)
            {
                IntVec3 cell = queue.Dequeue();
                if (!fill.Add(cell))
                {
                    continue;
                }

                for (int i = 0; i < 4; i++)
                {
                    IntVec3 next = cell + GenAdj.CardinalDirections[i];
                    if (!WalkableForEnclose(map, next, blockers) || fill.Contains(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            return fill;
        }

        private static bool WalkableForEnclose(Map map, IntVec3 loc, HashSet<IntVec3> futureBlockers)
        {
            if (futureBlockers.Contains(loc) || !loc.InBounds(map))
            {
                return false;
            }

            if (map.pathing.Normal.pathGrid.Walkable(loc))
            {
                return true;
            }

            return WalkableIgnoringFrames(map, loc);
        }

        internal static bool WalkableIgnoringFrames(Map map, IntVec3 loc)
        {
            if (map == null || !loc.InBounds(map))
            {
                return false;
            }

            TerrainDef terrain = map.terrainGrid.TerrainAt(loc);
            if (terrain != null && terrain.passability == Traversability.Impassable)
            {
                return false;
            }

            List<Thing> things = loc.GetThingList(map);
            bool hasFrame = false;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.def == null)
                {
                    continue;
                }

                if (IsConstructionFrame(thing))
                {
                    hasFrame = true;
                    continue;
                }

                if (thing is Pawn)
                {
                    continue;
                }

                if (thing.def.passability == Traversability.Impassable)
                {
                    return false;
                }
            }

            return hasFrame;
        }

        private static HashSet<IntVec3> Occupied(Thing target)
        {
            HashSet<IntVec3> cells = new HashSet<IntVec3>();
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(target))
            {
                cells.Add(cell);
            }

            return cells;
        }

        private static HashSet<IntVec3> CardinalNeighbors(HashSet<IntVec3> cells)
        {
            HashSet<IntVec3> neighbors = new HashSet<IntVec3>();
            foreach (IntVec3 cell in cells)
            {
                for (int i = 0; i < 4; i++)
                {
                    neighbors.Add(cell + GenAdj.CardinalDirections[i]);
                }
            }

            neighbors.ExceptWith(cells);
            return neighbors;
        }
    }
}
