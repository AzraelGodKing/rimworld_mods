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
        private const int CacheSlots = 16;

        private static bool scChecked;
        private static bool scLoaded;

        private struct CacheEntry
        {
            public int Tick;
            public int ThingId;
            public int PawnId;
            public OpenBuildsResult Result;
        }

        private static readonly CacheEntry[] encloseCache = new CacheEntry[CacheSlots];

        private enum PendingKind : byte
        {
            None = 0,
            Incompletable = 1,
            StepAside = 2,
        }

        private struct PendingAction
        {
            public PendingKind Kind;
            public int PawnId;
            public int FrameId;
            public IntVec3 Dest;
            public JobDef ResumeDef;
        }

        private static readonly List<PendingAction> pending = new List<PendingAction>();

        internal static bool HasPendingFor(Pawn pawn)
        {
            if (pawn == null || pending.Count == 0)
            {
                return false;
            }

            int id = pawn.thingIDNumber;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].PawnId == id)
                {
                    return true;
                }
            }

            return false;
        }

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
            int thingId = target.thingIDNumber;
            int pawnId = pawn != null ? pawn.thingIDNumber : -1;
            for (int i = 0; i < encloseCache.Length; i++)
            {
                CacheEntry entry = encloseCache[i];
                if (entry.ThingId == thingId
                    && entry.PawnId == pawnId
                    && tick - entry.Tick < CacheTicks)
                {
                    return entry.Result;
                }
            }

            OpenBuildsResult result = Compute(target, pawn);
            int slot = (thingId ^ (pawnId * 397)) & (CacheSlots - 1);
            encloseCache[slot] = new CacheEntry
            {
                Tick = tick,
                ThingId = thingId,
                PawnId = pawnId,
                Result = result,
            };
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

        /// <summary>
        /// Queue a step-aside (or incompletable stop) for the next GameComponent
        /// tick. Must not StartJob/EndCurrentJob here — CompleteConstruction runs
        /// inside the construct toil, and ReadyForNextToil follows immediately.
        /// </summary>
        internal static bool QueueStepAsideThenRetry(Pawn pawn, Frame frame)
        {
            if (pawn?.jobs == null || frame == null)
            {
                return false;
            }

            HashSet<IntVec3> spots = FindSafeSpots(frame);
            if (spots.Count == 0)
            {
                QueueIncompletable(pawn);
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

            JobDef resumeDef = pawn.CurJob != null ? pawn.CurJob.def : JobDefOf.FinishFrame;
            pending.Add(new PendingAction
            {
                Kind = PendingKind.StepAside,
                PawnId = pawn.thingIDNumber,
                FrameId = frame.thingIDNumber,
                Dest = dest,
                ResumeDef = resumeDef,
            });
            return true;
        }

        internal static void QueueIncompletable(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            pending.Add(new PendingAction
            {
                Kind = PendingKind.Incompletable,
                PawnId = pawn.thingIDNumber,
            });
        }

        internal static void TickDeferred()
        {
            if (pending.Count == 0)
            {
                return;
            }

            List<PendingAction> batch = new List<PendingAction>(pending);
            pending.Clear();
            for (int i = 0; i < batch.Count; i++)
            {
                ApplyPending(batch[i]);
            }
        }

        private static void ApplyPending(PendingAction action)
        {
            Pawn pawn = FindPawnAnyMap(action.PawnId);
            if (pawn?.jobs == null || pawn.Destroyed)
            {
                return;
            }

            if (action.Kind == PendingKind.Incompletable)
            {
                if (pawn.CurJob != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }

                return;
            }

            if (action.Kind != PendingKind.StepAside)
            {
                return;
            }

            Frame frame = FindFrame(pawn.Map, action.FrameId);
            if (frame == null || frame.Destroyed)
            {
                return;
            }

            Job resume = JobMaker.MakeJob(action.ResumeDef ?? JobDefOf.FinishFrame, frame);
            pawn.jobs.jobQueue.EnqueueFirst(resume);
            Job walk = JobMaker.MakeJob(JobDefOf.Goto, action.Dest);
            walk.ignoreForbidden = true;
            pawn.jobs.StartJob(walk, JobCondition.InterruptForced);
        }

        private static Pawn FindPawnAnyMap(int id)
        {
            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return null;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                Pawn pawn = FindPawnOnMap(maps[i], id);
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static Pawn FindPawnOnMap(Map map, int id)
        {
            List<Pawn> pawns = map.mapPawns?.AllPawns;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].thingIDNumber == id)
                {
                    return pawns[i];
                }
            }

            return null;
        }

        private static Frame FindFrame(Map map, int id)
        {
            if (map == null)
            {
                return null;
            }

            List<Thing> frames = map.listerThings?.ThingsInGroup(ThingRequestGroup.BuildingFrame);
            if (frames == null)
            {
                return null;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] != null && frames[i].thingIDNumber == id)
                {
                    return frames[i] as Frame;
                }
            }

            return null;
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
