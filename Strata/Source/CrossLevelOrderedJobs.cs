using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Player right-click orders issued while viewing another linked floor:
    // store the job, walk the pawn through stairs, then start it on arrival.
    public static class CrossLevelOrderedJobs
    {
        private struct Ordered
        {
            public Job job;
            public JobTag? tag;
            public bool prioritized;
            public WorkGiver giver;
            public IntVec3 cell;
            public int destMapId;
        }

        private class Pending
        {
            public readonly List<Ordered> jobs = new List<Ordered>();
            public bool retry;
            public int attempts;
            public int nextTryTick;
        }

        private const int ResumeAttempts = 8;
        private static readonly List<int> tickIds = new List<int>();

        private static readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();

        internal static void ResetSession()
        {
            pending.Clear();
        }

        public static bool TryInterceptOrderedJob(
            Pawn pawn,
            Job job,
            JobTag? tag,
            bool requestQueueing,
            out bool started)
        {
            started = false;
            if (!ShouldIntercept(pawn, job, out Map destMap))
            {
                return false;
            }

            if (requestQueueing && TryAppend(pawn, job, tag, false, null, IntVec3.Invalid, destMap))
            {
                started = true;
                return true;
            }

            pending[pawn.thingIDNumber] = new Pending();
            pending[pawn.thingIDNumber].jobs.Add(new Ordered
            {
                job = job,
                tag = tag,
                prioritized = false,
                giver = null,
                cell = IntVec3.Invalid,
                destMapId = destMap.uniqueID,
            });

            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                destMap,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: PreferArrivalCell(job, destMap));
            if (hop == null)
            {
                pending.Remove(pawn.thingIDNumber);
                return false;
            }

            started = pawn.jobs.TryTakeOrderedJob(hop, JobTag.Misc, requestQueueing);
            if (!started)
            {
                pending.Remove(pawn.thingIDNumber);
                PortalRelayChain.ClearIntent(pawn);
            }
            return true;
        }

        public static bool TryInterceptPrioritizedWork(
            Pawn pawn,
            Job job,
            WorkGiver giver,
            IntVec3 cell,
            out bool started)
        {
            started = false;
            if (!ShouldIntercept(pawn, job, out Map destMap))
            {
                return false;
            }

            if (TryAppend(pawn, job, JobTag.MiscWork, true, giver, cell, destMap))
            {
                started = true;
                return true;
            }

            pending[pawn.thingIDNumber] = new Pending();
            pending[pawn.thingIDNumber].jobs.Add(new Ordered
            {
                job = job,
                tag = JobTag.MiscWork,
                prioritized = true,
                giver = giver,
                cell = cell,
                destMapId = destMap.uniqueID,
            });

            IntVec3 prefer = PreferArrivalCell(job, destMap);
            if ((!prefer.IsValid || !prefer.InBounds(destMap)) && cell.IsValid && cell.InBounds(destMap))
            {
                prefer = cell;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                destMap,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: prefer);
            if (hop == null)
            {
                pending.Remove(pawn.thingIDNumber);
                return false;
            }

            started = pawn.jobs.TryTakeOrderedJob(hop, JobTag.Misc);
            if (!started)
            {
                pending.Remove(pawn.thingIDNumber);
                PortalRelayChain.ClearIntent(pawn);
            }
            return true;
        }

        public static void Finish(Pawn pawn)
        {
            Apply(pawn, fromArrival: true);
        }

        public static void TickMap(Map map)
        {
            if (map == null || pending.Count == 0)
            {
                return;
            }

            int now = Find.TickManager?.TicksGame ?? 0;
            tickIds.Clear();
            foreach (KeyValuePair<int, Pending> pair in pending)
            {
                if (pair.Value.retry && pair.Value.nextTryTick <= now)
                {
                    tickIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < tickIds.Count; i++)
            {
                Pawn pawn = FindSpawned(map, tickIds[i]);
                if (pawn == null)
                {
                    continue;
                }
                Apply(pawn, fromArrival: false);
            }
        }

        public static void ArmRetry(int pawnId)
        {
            if (pawnId <= 0 || !pending.TryGetValue(pawnId, out Pending order))
            {
                return;
            }
            order.retry = true;
            order.nextTryTick = (Find.TickManager?.TicksGame ?? 0) + 30;
        }

        public static void HoldForArrival(Pawn pawn)
        {
            if (pawn != null && pending.TryGetValue(pawn.thingIDNumber, out Pending order))
            {
                order.retry = false;
            }
        }

        public static void Release(int pawnId)
        {
            if (!pending.TryGetValue(pawnId, out Pending order))
            {
                return;
            }
            pending.Remove(pawnId);
            ReturnAll(order.jobs);
        }

        // A queued job survived EnterPortal. Keep it if its targets live on one floor.
        public static bool KeepQueued(Pawn pawn, Job job, JobTag? tag, int departureMapId)
        {
            if (pawn?.Map == null || job?.def == null || job.def == JobDefOf.EnterPortal)
            {
                return false;
            }
            if (AlreadyStored(pawn, job))
            {
                return true;
            }

            int mapId = ResolveMapId(job, pawn, departureMapId);
            if (mapId <= 0)
            {
                return false;
            }
            if (mapId == pawn.Map.uniqueID)
            {
                pawn.jobs.jobQueue.EnqueueLast(job, tag ?? JobTag.Misc);
                return true;
            }

            Remember(pawn, job, tag, mapId);
            return true;
        }

        internal static List<CrossFloorOrderedJobRecord> CaptureOrdered()
        {
            var saved = new List<CrossFloorOrderedJobRecord>();
            foreach (KeyValuePair<int, Pending> pair in pending)
            {
                Pending order = pair.Value;
                for (int i = 0; i < order.jobs.Count; i++)
                {
                    Ordered ordered = order.jobs[i];
                    if (ordered.job == null)
                    {
                        continue;
                    }
                    saved.Add(new CrossFloorOrderedJobRecord
                    {
                        pawnId = pair.Key,
                        destMapId = ordered.destMapId,
                        job = ordered.job,
                        tag = ordered.tag ?? JobTag.Misc,
                        hasTag = ordered.tag.HasValue,
                        prioritized = ordered.prioritized,
                        giverDefName = ordered.giver?.def?.defName,
                        cell = ordered.cell,
                        attempts = order.attempts,
                    });
                }
            }
            return saved;
        }

        internal static void RestoreOrdered(List<CrossFloorOrderedJobRecord> saved)
        {
            if (saved == null)
            {
                return;
            }
            for (int i = 0; i < saved.Count; i++)
            {
                CrossFloorOrderedJobRecord record = saved[i];
                if (record?.job == null || record.pawnId <= 0 || record.destMapId <= 0)
                {
                    continue;
                }
                WorkGiver giver = null;
                if (!string.IsNullOrEmpty(record.giverDefName))
                {
                    giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(record.giverDefName)?.Worker;
                }
                if (!pending.TryGetValue(record.pawnId, out Pending order))
                {
                    order = new Pending
                    {
                        retry = true,
                        attempts = record.attempts,
                    };
                    pending[record.pawnId] = order;
                }
                order.jobs.Add(new Ordered
                {
                    job = record.job,
                    tag = record.hasTag ? record.tag : (JobTag?)null,
                    prioritized = record.prioritized,
                    giver = giver,
                    cell = record.cell,
                    destMapId = record.destMapId,
                });
            }
        }

        private static void Apply(Pawn pawn, bool fromArrival)
        {
            if (pawn?.jobs == null || pawn.Map == null
                || !pending.TryGetValue(pawn.thingIDNumber, out Pending order))
            {
                return;
            }
            if (pawn.Dead)
            {
                Release(pawn.thingIDNumber);
                return;
            }
            if (!pawn.Spawned || pawn.Downed || pawn.InMentalState
                || pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                order.retry = true;
                order.nextTryTick = (Find.TickManager?.TicksGame ?? 0) + 30;
                return;
            }

            int now = Find.TickManager?.TicksGame ?? 0;
            if (!fromArrival && pawn.Drafted)
            {
                order.retry = true;
                order.nextTryTick = now + 60;
                return;
            }
            if (!fromArrival && PortalRelayChain.HasArrivalWork(pawn))
            {
                order.retry = true;
                order.nextTryTick = now + 120;
                return;
            }

            var here = new List<Ordered>();
            var elsewhere = new List<Ordered>();
            for (int i = 0; i < order.jobs.Count; i++)
            {
                SortOne(order.jobs[i], pawn, here, elsewhere);
            }

            if (here.Count == 0 && elsewhere.Count == 0)
            {
                pending.Remove(pawn.thingIDNumber);
                return;
            }

            if (!fromArrival && pawn.CurJob != null && pawn.CurJob.playerForced)
            {
                for (int i = 0; i < here.Count; i++)
                {
                    pawn.jobs.jobQueue.EnqueueLast(here[i].job, here[i].tag ?? JobTag.Misc);
                }
                if (elsewhere.Count == 0)
                {
                    pending.Remove(pawn.thingIDNumber);
                    return;
                }
                order.jobs.Clear();
                order.jobs.AddRange(elsewhere);
                order.retry = true;
                order.nextTryTick = now + 60;
                return;
            }

            if (here.Count > 0 && !StartHere(pawn, here))
            {
                order.jobs.Clear();
                order.jobs.AddRange(here);
                order.jobs.AddRange(elsewhere);
                order.attempts++;
                order.retry = true;
                order.nextTryTick = now + 25;
                if (order.attempts >= ResumeAttempts)
                {
                    Release(pawn.thingIDNumber);
                }
                return;
            }

            if (elsewhere.Count == 0)
            {
                pending.Remove(pawn.thingIDNumber);
                return;
            }

            Pending follow = new Pending();
            follow.jobs.AddRange(elsewhere);
            pending[pawn.thingIDNumber] = follow;
            if (PortalRelayChain.HasIntent(pawn))
            {
                follow.retry = true;
                follow.nextTryTick = now + 45;
                return;
            }

            Map next = FindMap(elsewhere[0].destMapId);
            if (next == null)
            {
                Release(pawn.thingIDNumber);
                return;
            }
            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                next,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: PreferArrivalCell(elsewhere[0].job, next));
            if (hop == null)
            {
                follow.retry = true;
                follow.attempts = order.attempts + 1;
                follow.nextTryTick = now + 45;
                if (follow.attempts >= ResumeAttempts)
                {
                    Release(pawn.thingIDNumber);
                }
                return;
            }
            if (fromArrival && here.Count == 0)
            {
                pawn.jobs.StartJob(hop, JobCondition.InterruptForced);
            }
            else
            {
                pawn.jobs.jobQueue.EnqueueLast(hop, JobTag.Misc);
            }
        }

        private static void SortOne(Ordered ordered, Pawn pawn, List<Ordered> here, List<Ordered> elsewhere)
        {
            if (ordered.job == null)
            {
                return;
            }
            int thingMap = MapIdOfThings(ordered.job, pawn);
            if (thingMap == MixedOrDead)
            {
                JobMaker.ReturnToPool(ordered.job);
                return;
            }
            if (thingMap == pawn.Map.uniqueID
                || (thingMap == NoThings && (ordered.destMapId == pawn.Map.uniqueID || ordered.destMapId <= 0)))
            {
                here.Add(ordered);
                return;
            }
            if (thingMap > 0)
            {
                ordered.destMapId = thingMap;
                elsewhere.Add(ordered);
                return;
            }
            if (ordered.destMapId > 0 && ordered.destMapId != pawn.Map.uniqueID)
            {
                elsewhere.Add(ordered);
                return;
            }
            JobMaker.ReturnToPool(ordered.job);
        }

        private static bool StartHere(Pawn pawn, List<Ordered> here)
        {
            if (here.Count == 0)
            {
                return false;
            }
            if (pawn.Drafted && pawn.drafter != null)
            {
                pawn.drafter.Drafted = false;
            }
            if (!TryStart(pawn, here[0]))
            {
                Ordered failed = here[0];
                here.RemoveAt(0);
                here.Add(failed);
                return false;
            }
            for (int i = 1; i < here.Count; i++)
            {
                pawn.jobs.jobQueue.EnqueueLast(here[i].job, here[i].tag ?? JobTag.Misc);
            }
            return true;
        }

        private static bool TryStart(Pawn pawn, Ordered ordered)
        {
            if (ordered.job == null)
            {
                return false;
            }
            if (ordered.prioritized && ordered.giver != null)
            {
                IntVec3 cell = ordered.cell.IsValid ? ordered.cell : pawn.Position;
                return pawn.jobs.TryTakeOrderedJobPrioritizedWork(ordered.job, ordered.giver, cell);
            }
            return pawn.jobs.TryTakeOrderedJob(ordered.job, ordered.tag ?? JobTag.Misc);
        }

        private static void Remember(Pawn pawn, Job job, JobTag? tag, int destMapId)
        {
            int id = pawn.thingIDNumber;
            bool arrivalOwnsIt = PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder);
            if (!pending.TryGetValue(id, out Pending existing))
            {
                existing = new Pending();
                pending[id] = existing;
            }
            if (!arrivalOwnsIt)
            {
                existing.retry = true;
                int now = Find.TickManager?.TicksGame ?? 0;
                if (existing.nextTryTick < now + 90)
                {
                    existing.nextTryTick = now + 90;
                }
            }
            existing.jobs.Add(new Ordered
            {
                job = job,
                tag = tag,
                prioritized = false,
                giver = null,
                cell = IntVec3.Invalid,
                destMapId = destMapId,
            });
        }

        private const int NoThings = -1;
        private const int MixedOrDead = -2;

        private static int ResolveMapId(Job job, Pawn pawn, int departureMapId)
        {
            int thingMap = MapIdOfThings(job, pawn);
            if (thingMap == MixedOrDead)
            {
                return -1;
            }
            if (thingMap > 0)
            {
                return thingMap;
            }
            if (thingMap == NoThings && departureMapId > 0)
            {
                return departureMapId;
            }
            return -1;
        }

        private static int MapIdOfThings(Job job, Pawn pawn)
        {
            int mapId = NoThings;
            bool sawCarried = false;
            bool consider(LocalTargetInfo target)
            {
                if (!target.HasThing)
                {
                    return true;
                }
                Thing thing = target.Thing;
                if (thing == null || thing.Destroyed)
                {
                    mapId = MixedOrDead;
                    return false;
                }
                Map held = thing.MapHeld;
                if (held == null)
                {
                    sawCarried = true;
                    return true;
                }
                if (mapId == NoThings)
                {
                    mapId = held.uniqueID;
                    return true;
                }
                if (mapId != held.uniqueID)
                {
                    mapId = MixedOrDead;
                    return false;
                }
                return true;
            }

            if (!consider(job.targetA) || !consider(job.targetB) || !consider(job.targetC))
            {
                return mapId;
            }
            if (!ConsiderQueue(job.targetQueueA, consider) || !ConsiderQueue(job.targetQueueB, consider))
            {
                return mapId;
            }
            if (mapId == NoThings && sawCarried && pawn.Map != null)
            {
                return pawn.Map.uniqueID;
            }
            return mapId;
        }

        private static bool ConsiderQueue(List<LocalTargetInfo> queue, System.Func<LocalTargetInfo, bool> consider)
        {
            if (queue == null)
            {
                return true;
            }
            for (int i = 0; i < queue.Count; i++)
            {
                if (!consider(queue[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AlreadyStored(Pawn pawn, Job job)
        {
            if (!pending.TryGetValue(pawn.thingIDNumber, out Pending order))
            {
                return false;
            }
            for (int i = 0; i < order.jobs.Count; i++)
            {
                if (order.jobs[i].job == job)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ReturnAll(List<Ordered> jobs)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].job != null)
                {
                    JobMaker.ReturnToPool(jobs[i].job);
                }
            }
        }

        private static Pawn FindSpawned(Map map, int pawnId)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].thingIDNumber == pawnId)
                {
                    return pawns[i];
                }
            }
            return null;
        }

        private static Map FindMap(int uniqueId)
        {
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

        // Shift-click while a stair trip is already running: keep the extra order.
        private static bool TryAppend(
            Pawn pawn,
            Job job,
            JobTag? tag,
            bool prioritized,
            WorkGiver giver,
            IntVec3 cell,
            Map destMap)
        {
            if (pawn == null
                || !pending.TryGetValue(pawn.thingIDNumber, out Pending existing)
                || existing.jobs.Count == 0
                || !PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder))
            {
                return false;
            }

            existing.jobs.Add(new Ordered
            {
                job = job,
                tag = tag,
                prioritized = prioritized,
                giver = giver,
                cell = cell,
                destMapId = destMap.uniqueID,
            });
            return true;
        }

        private static bool ShouldIntercept(Pawn pawn, Job job, out Map destMap)
        {
            destMap = null;
            if (pawn?.Map == null || job == null || job.def == JobDefOf.EnterPortal)
            {
                return false;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return false;
            }
            // Drafted pawns usually skip stair orders; pilot console is an exception
            // (M9 — launch from underdeck / tower while drafted).
            if (pawn.Drafted && job.def != JobDefOf.PilotConsole)
            {
                return false;
            }

            destMap = ResolveDestinationMap(pawn, job);
            if (destMap == null || destMap == pawn.Map)
            {
                return false;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, destMap))
            {
                return false;
            }
            return LevelGraph.BestFirstStep(pawn.Map, destMap, pawn.Position, pawn) != null;
        }

        private static Map ResolveDestinationMap(Pawn pawn, Job job)
        {
            // Prefer a target on another linked floor. HaulToContainer often has
            // materials on the pawn's map (targetA) and the frame elsewhere (B/C).
            Map map = FirstOffMapThing(pawn, job.targetB)
                ?? FirstOffMapThing(pawn, job.targetC)
                ?? FirstOffMapThing(pawn, job.targetA);
            if (map != null)
            {
                return map;
            }

            if (job.targetQueueB != null)
            {
                for (int i = 0; i < job.targetQueueB.Count; i++)
                {
                    map = FirstOffMapThing(pawn, job.targetQueueB[i]);
                    if (map != null)
                    {
                        return map;
                    }
                }
            }

            if (job.targetQueueA != null)
            {
                for (int i = 0; i < job.targetQueueA.Count; i++)
                {
                    map = FirstOffMapThing(pawn, job.targetQueueA[i]);
                    if (map != null)
                    {
                        return map;
                    }
                }
            }

            // Cell-only orders while the camera is on another linked floor.
            if (Find.CurrentMap != null
                && Find.CurrentMap != pawn.Map
                && ColonyBedUtility.MapsLinked(pawn.Map, Find.CurrentMap))
            {
                return Find.CurrentMap;
            }

            return null;
        }

        private static Map FirstOffMapThing(Pawn pawn, LocalTargetInfo target)
        {
            Map map = MapOfTarget(target);
            if (map != null && map != pawn.Map)
            {
                return map;
            }
            return null;
        }

        private static Map MapOfTarget(LocalTargetInfo target)
        {
            if (target.HasThing)
            {
                return target.Thing.MapHeld;
            }
            return null;
        }

        private static IntVec3 PreferArrivalCell(Job job, Map destMap)
        {
            if (job == null || destMap == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3? cell = CellOfTarget(job.targetA, destMap)
                ?? CellOfTarget(job.targetB, destMap)
                ?? CellOfTarget(job.targetC, destMap);
            if (cell.HasValue)
            {
                return cell.Value;
            }

            if (job.targetQueueA != null)
            {
                for (int i = 0; i < job.targetQueueA.Count; i++)
                {
                    IntVec3? queued = CellOfTarget(job.targetQueueA[i], destMap);
                    if (queued.HasValue)
                    {
                        return queued.Value;
                    }
                }
            }

            return IntVec3.Invalid;
        }

        private static IntVec3? CellOfTarget(LocalTargetInfo target, Map destMap)
        {
            if (target.HasThing && target.Thing.MapHeld == destMap)
            {
                return target.Thing.PositionHeld;
            }
            if (!target.HasThing && target.Cell.IsValid && target.Cell.InBounds(destMap))
            {
                return target.Cell;
            }
            return null;
        }
    }
}
