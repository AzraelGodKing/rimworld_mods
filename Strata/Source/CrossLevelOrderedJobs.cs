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
        private class Pending
        {
            public Job job;
            public JobTag? tag;
            public bool prioritized;
            public WorkGiver giver;
            public IntVec3 cell;
        }

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

            pending[pawn.thingIDNumber] = new Pending
            {
                job = job,
                tag = tag,
                prioritized = false,
                giver = null,
                cell = IntVec3.Invalid,
            };

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

            pending[pawn.thingIDNumber] = new Pending
            {
                job = job,
                tag = JobTag.MiscWork,
                prioritized = true,
                giver = giver,
                cell = cell,
            };

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
            if (pawn?.jobs == null
                || !pending.TryGetValue(pawn.thingIDNumber, out Pending order))
            {
                return;
            }

            pending.Remove(pawn.thingIDNumber);
            Job job = order.job;
            if (job == null || job.def == null)
            {
                return;
            }

            // Targets may have despawned while commuting.
            if (job.targetA.HasThing && (job.targetA.Thing.Destroyed || job.targetA.Thing.MapHeld != pawn.Map))
            {
                return;
            }

            if (order.prioritized && order.giver != null)
            {
                IntVec3 cell = order.cell.IsValid ? order.cell : pawn.Position;
                pawn.jobs.TryTakeOrderedJobPrioritizedWork(job, order.giver, cell);
            }
            else
            {
                pawn.jobs.TryTakeOrderedJob(job, order.tag ?? JobTag.Misc);
            }
        }

        public static void Cancel(Pawn pawn)
        {
            if (pawn != null)
            {
                pending.Remove(pawn.thingIDNumber);
            }
        }

        private static bool ShouldIntercept(Pawn pawn, Job job, out Map destMap)
        {
            destMap = null;
            if (pawn?.Map == null || job == null || job.def == JobDefOf.EnterPortal)
            {
                return false;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn) || pawn.Drafted)
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
            Map map = MapOfTarget(job.targetA)
                ?? MapOfTarget(job.targetB)
                ?? MapOfTarget(job.targetC);
            if (map != null)
            {
                return map;
            }

            if (job.targetQueueA != null)
            {
                for (int i = 0; i < job.targetQueueA.Count; i++)
                {
                    map = MapOfTarget(job.targetQueueA[i]);
                    if (map != null)
                    {
                        return map;
                    }
                }
            }

            if (job.targetQueueB != null)
            {
                for (int i = 0; i < job.targetQueueB.Count; i++)
                {
                    map = MapOfTarget(job.targetQueueB[i]);
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
