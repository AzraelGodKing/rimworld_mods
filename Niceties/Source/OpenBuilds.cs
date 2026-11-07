#nullable enable
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

        /// <summary>True when finishing would enclose the builder (possibly alone).</summary>
        public bool EnclosesBuilder => EnclosesSelf;
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
        private const int CachePruneSize = 64;

        private static bool scChecked;
        private static bool scLoaded;

        /// <summary>Flood-fill result for one frame. Pooled, so cache misses do not allocate.</summary>
        private sealed class Geometry
        {
            public int Tick;
            public int MapId;
            public IntVec3 Anchor;
            public readonly List<int> Blockers = new List<int>();
            public readonly List<int> Enclosed = new List<int>();
            public readonly List<IntVec3> SafeSpots = new List<IntVec3>();
            public bool SafeSpotsReady;

            public void Reset()
            {
                Blockers.Clear();
                Enclosed.Clear();
                SafeSpots.Clear();
                SafeSpotsReady = false;
            }
        }

        /// <summary>Map adapter for <see cref="EncloseFill"/>; a struct, so no boxing or virtual calls.</summary>
        private readonly struct MapGrid : IEncloseGrid
        {
            private readonly Map map;

            public MapGrid(Map map)
            {
                this.map = map;
            }

            public int Width => map.Size.x;

            public int Height => map.Size.z;

            public bool Walkable(int index)
            {
                IntVec3 loc = map.cellIndices.IndexToCell(index);
                return map.pathing.Normal.pathGrid.Walkable(loc) || WalkableIgnoringFrames(map, loc);
            }
        }

        private static readonly Dictionary<int, Geometry> GeometryCache = new Dictionary<int, Geometry>();
        private static readonly Stack<Geometry> Pool = new Stack<Geometry>();
        private static readonly Geometry Fresh = new Geometry();
        private static readonly List<int> PruneScratch = new List<int>();
        private static readonly EncloseScratch FillScratch = new EncloseScratch();
        private static readonly List<int> CandX = new List<int>();
        private static readonly List<int> CandZ = new List<int>();
        private static readonly List<bool> CandReach = new List<bool>();

        internal static void ClearCache()
        {
            foreach (Geometry geo in GeometryCache.Values)
            {
                Release(geo);
            }

            GeometryCache.Clear();
        }

        /// <summary>
        /// A finished wall only changes flood fills that could reach it. A fill stops at
        /// <see cref="MaxRegionSize"/> cells, so anything cached on another map or further
        /// away than that stays valid (AZR-701).
        /// </summary>
        internal static void ClearCacheNear(Map map, IntVec3 cell)
        {
            int reach = MaxRegionSize + 2;
            int reachSq = reach * reach;
            PruneScratch.Clear();
            foreach (KeyValuePair<int, Geometry> kv in GeometryCache)
            {
                if (kv.Value.MapId == map.uniqueID && kv.Value.Anchor.DistanceToSquared(cell) <= reachSq)
                {
                    PruneScratch.Add(kv.Key);
                }
            }

            RemoveScratchKeys();
        }

        internal static bool Enabled
        {
            get
            {
                if (!NicetiesSim.Settings.enableLeaveAWayOut)
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

        internal static bool IsImpassableBuild(Thing? t)
        {
            return t?.def?.entityDefToBuild != null
                && t.def.entityDefToBuild.passability == Traversability.Impassable;
        }

        internal static OpenBuildsResult WouldEnclose(Thing target, Pawn pawn)
        {
            if (target.Map == null)
            {
                return default;
            }

            return Compute(target, pawn, GeometryFor(target, allowCache: true));
        }

        /// <summary>Final CompleteConstruction check must not reuse a stale cache (AZR-548).</summary>
        internal static OpenBuildsResult WouldEncloseFresh(Thing target, Pawn pawn)
        {
            if (target.Map == null)
            {
                return default;
            }

            return Compute(target, pawn, GeometryFor(target, allowCache: false));
        }

        internal static List<IntVec3> FindSafeSpots(Thing target)
        {
            Geometry geo = GeometryFor(target, allowCache: true);
            if (geo.SafeSpotsReady)
            {
                return geo.SafeSpots;
            }

            Map map = target.Map;
            MapGrid grid = new MapGrid(map);
            for (int i = 0; i < geo.Blockers.Count; i++)
            {
                IntVec3 blocker = map.cellIndices.IndexToCell(geo.Blockers[i]);
                for (int d = 0; d < 4; d++)
                {
                    IntVec3 cell = blocker + GenAdj.CardinalDirections[d];
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    int index = map.cellIndices.CellToIndex(cell);
                    if (geo.Blockers.Contains(index) || geo.Enclosed.Contains(index)
                        || geo.SafeSpots.Contains(cell) || !grid.Walkable(index))
                    {
                        continue;
                    }

                    geo.SafeSpots.Add(cell);
                }
            }

            geo.SafeSpotsReady = true;
            return geo.SafeSpots;
        }

        /// <summary>
        /// Flood fill per frame, reused by every builder and by FindSafeSpots for a few ticks.
        /// Keyed by frame so several colonists finishing different walls do not evict each other.
        /// </summary>
        private static Geometry GeometryFor(Thing target, bool allowCache)
        {
            Map map = target.Map;
            int tick = Find.TickManager.TicksGame;
            int mapId = map.uniqueID;
            if (allowCache
                && GeometryCache.TryGetValue(target.thingIDNumber, out Geometry? cached)
                && cached.MapId == mapId
                && tick >= cached.Tick
                && tick - cached.Tick < CacheTicks)
            {
                return cached;
            }

            Geometry geo;
            if (!allowCache)
            {
                geo = Fresh;
            }
            else
            {
                if (GeometryCache.TryGetValue(target.thingIDNumber, out Geometry? stale))
                {
                    GeometryCache.Remove(target.thingIDNumber);
                    Release(stale);
                }

                if (GeometryCache.Count >= CachePruneSize)
                {
                    Prune(tick);
                }

                geo = Pool.Count > 0 ? Pool.Pop() : new Geometry();
                GeometryCache[target.thingIDNumber] = geo;
            }

            geo.Reset();
            geo.Tick = tick;
            geo.MapId = mapId;
            geo.Anchor = target.Position;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(target))
            {
                if (cell.InBounds(map))
                {
                    geo.Blockers.Add(map.cellIndices.CellToIndex(cell));
                }
            }

            MapGrid grid = new MapGrid(map);
            EncloseFill.ClosedRegions(ref grid, geo.Blockers, MaxRegionSize, FillScratch, geo.Enclosed);
            return geo;
        }

        private static void Release(Geometry geo)
        {
            if (!ReferenceEquals(geo, Fresh) && Pool.Count < CachePruneSize)
            {
                geo.Reset();
                Pool.Push(geo);
            }
        }

        private static void Prune(int tick)
        {
            PruneScratch.Clear();
            foreach (KeyValuePair<int, Geometry> kv in GeometryCache)
            {
                if (tick < kv.Value.Tick || tick - kv.Value.Tick >= CacheTicks)
                {
                    PruneScratch.Add(kv.Key);
                }
            }

            RemoveScratchKeys();
            if (GeometryCache.Count >= CachePruneSize)
            {
                ClearCache();
            }
        }

        private static void RemoveScratchKeys()
        {
            for (int i = 0; i < PruneScratch.Count; i++)
            {
                if (GeometryCache.TryGetValue(PruneScratch[i], out Geometry? geo))
                {
                    GeometryCache.Remove(PruneScratch[i]);
                    Release(geo);
                }
            }

            PruneScratch.Clear();
        }

        internal static bool StepAsideThenRetry(Pawn pawn, Frame frame)
        {
            if (pawn.jobs == null)
            {
                return false;
            }

            List<IntVec3> spots = FindSafeSpots(frame);
            if (spots.Count == 0)
            {
                JobFailReason.Is("Niceties_OpenBuilds_WouldTrapSelf".Translate());
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

            if (spots.Contains(pawn.Position))
            {
                return false;
            }

            CandX.Clear();
            CandZ.Clear();
            CandReach.Clear();
            for (int i = 0; i < spots.Count; i++)
            {
                CandX.Add(spots[i].x);
                CandZ.Add(spots[i].z);
                CandReach.Add(pawn.CanReach(spots[i], PathEndMode.OnCell, Danger.Some));
            }

            // Nearest, then lowest z, then lowest x: the same cell on every machine.
            int pick = EncloseFill.PickStepAside(pawn.Position.x, pawn.Position.z, CandX, CandZ, CandReach);
            if (pick < 0)
            {
                JobFailReason.Is("Niceties_OpenBuilds_WouldTrapSelf".Translate());
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

            IntVec3 dest = spots[pick];
            if (pawn.CurJob == null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

            // Prefer ending incompletable and queueing resume+walk so ReadyForNextToil
            // cannot clobber a mid-toil StartJob (AZR-548).
            Job resume = JobMaker.MakeJob(pawn.CurJob.def, frame);
            Job walk = JobMaker.MakeJob(JobDefOf.Goto, dest);
            walk.ignoreForbidden = true;
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            pawn.jobs.jobQueue.EnqueueFirst(resume);
            pawn.jobs.jobQueue.EnqueueFirst(walk);
            return true;
        }

        internal static bool IsConstructionFrame(Thing? thing)
        {
            if (thing == null)
            {
                return false;
            }

            return thing is Frame || (thing.def != null && thing.def.IsFrame);
        }

        private static OpenBuildsResult Compute(Thing target, Pawn pawn, Geometry geo)
        {
            OpenBuildsResult result = new OpenBuildsResult();
            Map map = target.Map;
            for (int c = 0; c < geo.Enclosed.Count; c++)
            {
                List<Thing> things = map.thingGrid.ThingsListAtFast(geo.Enclosed[c]);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing is Blueprint || IsConstructionFrame(thing))
                    {
                        result.EnclosesThings = true;
                        continue;
                    }

                    if (thing is not Pawn other || other.Faction == null || !other.Faction.IsPlayer)
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

        internal static bool WalkableIgnoringFrames(Map map, IntVec3 loc)
        {
            if (!loc.InBounds(map))
            {
                return false;
            }

            TerrainDef? terrain = map.terrainGrid.TerrainAt(loc);
            if (terrain != null && terrain.passability == Traversability.Impassable)
            {
                return false;
            }

            List<Thing> things = loc.GetThingList(map);
            bool hasFrame = false;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing?.def == null)
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
    }
}
