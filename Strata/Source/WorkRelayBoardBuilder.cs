using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// Builds work-board entries. Cheap probes always on main thread; optional
    /// ThreadPool path aggregates packed grow-cell flags (no Verse APIs on workers).
    /// </summary>
    public static class WorkRelayBoardBuilder
    {
        private const byte GrowEmptySowable = 1;
        private const byte GrowHarvestable = 2;

        private sealed class OffThreadJob
        {
            public int mapId;
            public int workVersion;
            public int enqueuedTick;
            public WorkRelayBoard.BoardEntry partial;
            public byte[] growCells;
            public WorkRelayBoard.BoardEntry result;
            public volatile bool completed;
            public bool failed;
            public string error;
        }

        private static readonly object gate = new object();

        private static readonly Dictionary<int, OffThreadJob> pending = new Dictionary<int, OffThreadJob>();

        internal static void ResetSession()
        {
            lock (gate)
            {
                pending.Clear();
            }
        }

        internal static void CancelMap(int mapId)
        {
            lock (gate)
            {
                pending.Remove(mapId);
            }
        }

        internal static bool HasPending(int mapId)
        {
            lock (gate)
            {
                return pending.TryGetValue(mapId, out OffThreadJob job) && !job.completed;
            }
        }

        public static bool OffThreadActive =>
            StrataMod.Settings != null
            && StrataMod.Settings.OffThreadWorkRelayActive;

        /// <summary>Full sync rebuild and publish (fallback + when off-thread off).</summary>
        public static WorkRelayBoard.BoardEntry BuildAndPublishSync(Map map)
        {
            if (map == null)
            {
                return null;
            }
            CancelMap(map.uniqueID);
            WorkRelayBoard.BoardEntry entry = BuildPartialMain(map, out byte[] growCells);
            entry.growing = GrowingFromPacked(growCells);
            entry.workVersion = WorkRelaySignals.WorkVersion;
            entry.builtTick = Find.TickManager.TicksGame;
            WorkRelayBoard.Publish(entry);
            return entry;
        }

        /// <summary>
        /// Snapshot on main; if off-thread and grow cells exist, finish growing on
        /// a worker. Otherwise publish immediately. Returns true if a job was queued.
        /// </summary>
        public static bool TryEnqueueOrPublish(Map map)
        {
            if (map == null)
            {
                return false;
            }

            WorkRelayBoard.BoardEntry partial = BuildPartialMain(map, out byte[] growCells);
            int version = WorkRelaySignals.WorkVersion;
            int now = Find.TickManager.TicksGame;

            if (!OffThreadActive || growCells == null || growCells.Length == 0)
            {
                partial.growing = GrowingFromPacked(growCells);
                partial.workVersion = version;
                partial.builtTick = now;
                WorkRelayBoard.Publish(partial);
                return false;
            }

            lock (gate)
            {
                if (pending.TryGetValue(map.uniqueID, out OffThreadJob existing) && !existing.completed)
                {
                    return false;
                }

                var job = new OffThreadJob
                {
                    mapId = map.uniqueID,
                    workVersion = version,
                    enqueuedTick = now,
                    partial = partial,
                    growCells = growCells,
                };
                pending[map.uniqueID] = job;
                ThreadPool.QueueUserWorkItem(_ => RunGrowJob(job));
                return true;
            }
        }

        public static void DrainCompleted()
        {
            List<OffThreadJob> done = null;
            lock (gate)
            {
                if (pending.Count == 0)
                {
                    return;
                }
                foreach (KeyValuePair<int, OffThreadJob> kv in pending)
                {
                    if (!kv.Value.completed)
                    {
                        continue;
                    }
                    done ??= new List<OffThreadJob>();
                    done.Add(kv.Value);
                }
                if (done == null)
                {
                    return;
                }
                for (int i = 0; i < done.Count; i++)
                {
                    pending.Remove(done[i].mapId);
                }
            }

            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < done.Count; i++)
            {
                OffThreadJob job = done[i];
                if (job.failed)
                {
                    Log.WarningOnce(
                        "[Strata] Off-thread work board failed: " + job.error,
                        job.mapId ^ 0x57A7);
                    continue;
                }
                if (job.workVersion != WorkRelaySignals.WorkVersion
                    || now - job.enqueuedTick > 600)
                {
                    continue;
                }
                if (job.result != null)
                {
                    WorkRelayBoard.Publish(job.result);
                }
            }
        }

        private static void RunGrowJob(OffThreadJob job)
        {
            try
            {
                WorkRelayBoard.BoardEntry entry = job.partial;
                entry.growing = GrowingFromPacked(job.growCells);
                entry.workVersion = job.workVersion;
                entry.builtTick = job.enqueuedTick;
                job.result = entry;
            }
            catch (Exception ex)
            {
                job.failed = true;
                job.error = ex.ToString();
            }
            finally
            {
                job.completed = true;
            }
        }

        private static bool GrowingFromPacked(byte[] growCells)
        {
            if (growCells == null)
            {
                return false;
            }
            for (int i = 0; i < growCells.Length; i++)
            {
                if (growCells[i] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static WorkRelayBoard.BoardEntry BuildPartialMain(Map map, out byte[] growCells)
        {
            growCells = SnapshotGrowCells(map);
            return new WorkRelayBoard.BoardEntry
            {
                mapId = map.uniqueID,
                construction = WorkRelaySignals.ProbeConstruction(map),
                mining = WorkRelaySignals.ProbeMining(map),
                plantCutting = WorkRelaySignals.ProbePlantCutting(map),
                hunting = WorkRelaySignals.ProbeHunting(map),
                hauling = WorkRelaySignals.ProbeHauling(map, out int haulCount),
                haulableCount = haulCount,
                haulExports = WorkRelaySignals.ProbeCrossLevelHaulExports(map),
                cleaning = WorkRelaySignals.ProbeCleaning(map),
                firefighting = WorkRelaySignals.ProbeFirefighting(map),
                research = WorkRelaySignals.ProbeResearch(map),
                flick = WorkRelaySignals.ProbeFlick(map),
                warden = WorkRelaySignals.ProbeWarden(map),
                handling = WorkRelaySignals.ProbeHandling(map),
                childcare = WorkRelaySignals.ProbeChildcare(map),
                blueprintFrames = WorkRelaySignals.CountPlayerThingsPublic(map, ThingRequestGroup.Blueprint)
                    + WorkRelaySignals.CountPlayerThingsPublic(map, ThingRequestGroup.BuildingFrame),
            };
        }

        private static byte[] SnapshotGrowCells(Map map)
        {
            List<Zone> zones = map.zoneManager.AllZones;
            int total = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is Zone_Growing grow && grow.cells.Count > 0)
                {
                    total += grow.cells.Count;
                }
            }
            if (total == 0)
            {
                return Array.Empty<byte>();
            }

            var packed = new byte[total];
            int write = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is not Zone_Growing grow || grow.cells.Count == 0)
                {
                    continue;
                }
                for (int c = 0; c < grow.cells.Count; c++)
                {
                    IntVec3 cell = grow.cells[c];
                    byte flags = 0;
                    if (cell.InBounds(map))
                    {
                        Plant plant = cell.GetPlant(map);
                        if (plant == null)
                        {
                            if (cell.GetEdifice(map) == null && grow.GetPlantDefToGrow() != null)
                            {
                                flags = GrowEmptySowable;
                            }
                        }
                        else if (plant.HarvestableNow)
                        {
                            flags = GrowHarvestable;
                        }
                    }
                    packed[write++] = flags;
                }
            }
            if (write != packed.Length)
            {
                Array.Resize(ref packed, write);
            }
            return packed;
        }
    }
}
