using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;

namespace Strata
{
    // Main thread enqueues plain-data snapshots; workers compute; main thread drains
    // and applies. Never touch Verse/RimWorld APIs from worker threads.
    internal static class StrataOffThreadWork
    {
        private const float BreathDiffusionRate = 0.11f;
        private const int MaxStaleBreathDiffusionTicks = 600;

        private static readonly Dictionary<int, BreathDiffusionJob> breathJobsByMapId =
            new Dictionary<int, BreathDiffusionJob>();

        private static readonly object breathGate = new object();

        public static bool OffThreadAtmosphereEnabled =>
            StrataMod.Settings?.offThreadAtmosphere != false
            && StrataMod.Settings?.NaturalGasesActive != false;

        public static void LogStartup()
        {
            Log.Message("[Strata] Off-thread work: enabled=" + OffThreadAtmosphereEnabled + ".");
        }

        public static void CancelMap(int mapUniqueId)
        {
            lock (breathGate)
            {
                breathJobsByMapId.Remove(mapUniqueId);
            }
        }

        public static bool HasPendingBreathDiffusion(int mapUniqueId)
        {
            lock (breathGate)
            {
                return breathJobsByMapId.TryGetValue(mapUniqueId, out BreathDiffusionJob job)
                    && !job.Completed;
            }
        }

        public static bool TryEnqueueBreathDiffusion(
            Map map,
            float[] o2,
            float[] co2,
            bool[] skipDiffusionCell,
            int enqueuedTick)
        {
            if (map == null || o2 == null || co2 == null || skipDiffusionCell == null
                || !OffThreadAtmosphereEnabled)
            {
                return false;
            }

            lock (breathGate)
            {
                if (breathJobsByMapId.TryGetValue(map.uniqueID, out BreathDiffusionJob existing)
                    && !existing.Completed)
                {
                    return false;
                }

                var snapshot = new BreathDiffusionSnapshot
                {
                    mapUniqueId = map.uniqueID,
                    enqueuedTick = enqueuedTick,
                    mapWidth = map.Size.x,
                    mapHeight = map.Size.z,
                    o2 = (float[])o2.Clone(),
                    co2 = (float[])co2.Clone(),
                    skipCell = (bool[])skipDiffusionCell.Clone(),
                };

                var job = new BreathDiffusionJob
                {
                    Snapshot = snapshot,
                    ResultO2 = new float[o2.Length],
                    ResultCo2 = new float[co2.Length],
                };
                breathJobsByMapId[map.uniqueID] = job;

                ThreadPool.QueueUserWorkItem(_ => RunBreathDiffusionJob(job));
                return true;
            }
        }

        public static bool TryDrainBreathDiffusion(
            Map map,
            float[] o2,
            float[] co2,
            int currentTick,
            out bool applied)
        {
            applied = false;
            if (map == null || o2 == null || co2 == null)
            {
                return false;
            }

            BreathDiffusionJob job;
            lock (breathGate)
            {
                if (!breathJobsByMapId.TryGetValue(map.uniqueID, out job))
                {
                    return false;
                }
                if (!job.Completed)
                {
                    return false;
                }
                breathJobsByMapId.Remove(map.uniqueID);
            }

            if (job.Failed)
            {
                Log.Warning("[Strata] Off-thread breath diffusion failed on map "
                    + map.uniqueID + ": " + job.ErrorMessage);
                return true;
            }

            if (job.Snapshot.mapUniqueId != map.uniqueID
                || job.Snapshot.o2.Length != o2.Length
                || currentTick - job.Snapshot.enqueuedTick > MaxStaleBreathDiffusionTicks)
            {
                return true;
            }

            Array.Copy(job.ResultO2, o2, o2.Length);
            Array.Copy(job.ResultCo2, co2, co2.Length);
            applied = true;
            return true;
        }

        private static void RunBreathDiffusionJob(BreathDiffusionJob job)
        {
            try
            {
                ComputeBreathDiffusion(job.Snapshot, job.ResultO2, job.ResultCo2);
            }
            catch (Exception ex)
            {
                job.Failed = true;
                job.ErrorMessage = ex.ToString();
            }
            finally
            {
                job.MarkCompleted();
            }
        }

        private static void ComputeBreathDiffusion(
            BreathDiffusionSnapshot snap,
            float[] outO2,
            float[] outCo2)
        {
            float[] scratchO2 = new float[snap.o2.Length];
            float[] scratchCo2 = new float[snap.co2.Length];
            Array.Copy(snap.o2, scratchO2, snap.o2.Length);
            Array.Copy(snap.co2, scratchCo2, snap.co2.Length);
            Array.Copy(snap.o2, outO2, snap.o2.Length);
            Array.Copy(snap.co2, outCo2, snap.co2.Length);

            int width = snap.mapWidth;
            int height = snap.mapHeight;
            bool[] skip = snap.skipCell;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    if (skip[index])
                    {
                        continue;
                    }

                    float neighborO2 = 0f;
                    float neighborCo2 = 0f;
                    int neighbors = 0;

                    if (x > 0)
                    {
                        int left = index - 1;
                        neighborO2 += snap.o2[left];
                        neighborCo2 += snap.co2[left];
                        neighbors++;
                    }
                    if (x + 1 < width)
                    {
                        int right = index + 1;
                        neighborO2 += snap.o2[right];
                        neighborCo2 += snap.co2[right];
                        neighbors++;
                    }
                    if (z > 0)
                    {
                        int south = index - width;
                        neighborO2 += snap.o2[south];
                        neighborCo2 += snap.co2[south];
                        neighbors++;
                    }
                    if (z + 1 < height)
                    {
                        int north = index + width;
                        neighborO2 += snap.o2[north];
                        neighborCo2 += snap.co2[north];
                        neighbors++;
                    }

                    if (neighbors == 0)
                    {
                        continue;
                    }

                    neighborO2 /= neighbors;
                    neighborCo2 /= neighbors;
                    outO2[index] = Mathf.Lerp(scratchO2[index], neighborO2, BreathDiffusionRate);
                    outCo2[index] = Mathf.Lerp(scratchCo2[index], neighborCo2, BreathDiffusionRate);
                }
            }
        }

        private struct BreathDiffusionSnapshot
        {
            public int mapUniqueId;
            public int enqueuedTick;
            public int mapWidth;
            public int mapHeight;
            public float[] o2;
            public float[] co2;
            public bool[] skipCell;
        }

        private sealed class BreathDiffusionJob
        {
            public BreathDiffusionSnapshot Snapshot;
            public float[] ResultO2;
            public float[] ResultCo2;
            public bool Failed;
            public string ErrorMessage;
            private int completedFlag;

            public bool Completed => Volatile.Read(ref completedFlag) != 0;

            public void MarkCompleted() => Volatile.Write(ref completedFlag, 1);
        }
    }
}
