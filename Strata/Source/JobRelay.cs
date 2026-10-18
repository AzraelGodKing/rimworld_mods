using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Automatic cross-floor work. A pawn is assigned to one floor and one work
    // type, walked there, and kept on that work until the floor no longer has it.
    public static class JobRelay
    {
        private sealed class Assignment
        {
            public int mapId;
            public string workDefName;
            public int stuckSince;
        }

        private const int StuckTicks = 12000;
        private const int WorkScanCap = 80;
        private const int WorkCellCheckCap = 16;

        private static readonly Dictionary<int, Assignment> assigned = new Dictionary<int, Assignment>();

        internal static void ResetSession()
        {
            assigned.Clear();
        }

        public static bool IsAssigned(Pawn pawn)
        {
            return pawn != null && assigned.ContainsKey(pawn.thingIDNumber);
        }

        public static void Remember(Pawn pawn, Map map, WorkTypeDef type)
        {
            if (pawn == null || map == null || type == null)
            {
                return;
            }

            assigned[pawn.thingIDNumber] = new Assignment
            {
                mapId = map.uniqueID,
                workDefName = type.defName,
                stuckSince = 0,
            };
        }

        public static void Remember(int pawnId, int mapId, string workDefName)
        {
            if (pawnId <= 0 || mapId <= 0 || string.IsNullOrEmpty(workDefName))
            {
                return;
            }
            if (assigned.ContainsKey(pawnId))
            {
                return;
            }

            assigned[pawnId] = new Assignment
            {
                mapId = mapId,
                workDefName = workDefName,
            };
        }

        public static void Release(Pawn pawn)
        {
            if (pawn != null)
            {
                assigned.Remove(pawn.thingIDNumber);
            }
        }

        // One decision for every work-seeking think pass.
        public static void Apply(Pawn pawn, ThinkNode node, bool emergency, ref ThinkResult result)
        {
            if (!CanConsider(pawn) || node == null)
            {
                return;
            }

            if (DraftedPortalPathing.HasActiveRoute(pawn))
            {
                result = default;
                return;
            }

            if (TryHold(pawn, node, ref result))
            {
                return;
            }

            if (emergency || StrataGuardsForMeSoftCompat.IsOnDuty(pawn))
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.WorkRelayActive)
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map) || !StrataPawnUtility.CanWorkRelay(pawn) || !PawnRelay.CanRelay(pawn))
            {
                return;
            }

            if (result.Job != null)
            {
                TryLeaveForBetterWork(pawn, node, ref result);
                return;
            }

            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }

            if (TryAssignBest(pawn, node, ref result, out bool sawCandidate))
            {
                return;
            }

            if (sawCandidate)
            {
                PawnRelay.TouchColonistWorkScanFailed(pawn);
            }
            else
            {
                PawnRelay.TouchColonistWorkScanEmpty(pawn);
            }
        }

        // True while an assignment exists. The pawn stays on it.
        private static bool TryHold(Pawn pawn, ThinkNode node, ref ThinkResult result)
        {
            if (!assigned.TryGetValue(pawn.thingIDNumber, out Assignment assignment))
            {
                return false;
            }

            WorkTypeDef wanted = DefDatabase<WorkTypeDef>.GetNamedSilentFail(assignment.workDefName);
            Map dest = FindMap(assignment.mapId);
            if (wanted == null || dest == null)
            {
                assigned.Remove(pawn.thingIDNumber);
                return false;
            }

            if (pawn.Drafted || pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }

            if (pawn.Map != dest)
            {
                if (PortalRelayChain.HasIntent(pawn, RelayPurpose.Work))
                {
                    return true;
                }

                Job hop = PawnRelay.TryRelayToMap(pawn, dest, touchCooldown: false, RelayPurpose.Work);
                if (hop != null)
                {
                    result = new ThinkResult(hop, node, JobTag.MiscWork);
                    assignment.stuckSince = 0;
                    return true;
                }

                result = default;
                return !ReleaseIfStuck(pawn, assignment);
            }

            WorkTypeDef issued = result.Job?.workGiverDef?.workType;
            if (issued == WorkTypeDefOf.Firefighter || issued == WorkTypeDefOf.Doctor)
            {
                assignment.stuckSince = 0;
                return true;
            }

            if (!WorkRelaySignals.WorkTypeRemains(pawn, dest, wanted))
            {
                assigned.Remove(pawn.thingIDNumber);
                return false;
            }

            if (issued == wanted && JobTargetReachable(pawn, result.Job))
            {
                assignment.stuckSince = 0;
                return true;
            }

            if (TryMakeWorkJob(pawn, wanted, out Job job))
            {
                if (JobTargetReachable(pawn, job))
                {
                    result = new ThinkResult(job, node, JobTag.MiscWork);
                    assignment.stuckSince = 0;
                    return true;
                }

                if (TryDetourToJob(pawn, job, node, ref result))
                {
                    return true;
                }
            }

            if (TryDetourToBlockedWork(pawn, wanted, node, ref result))
            {
                return true;
            }

            result = default;
            return !ReleaseIfStuck(pawn, assignment);
        }

        private static bool ReleaseIfStuck(Pawn pawn, Assignment assignment)
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            if (assignment.stuckSince <= 0)
            {
                assignment.stuckSince = now;
                return false;
            }
            if (now - assignment.stuckSince < StuckTicks)
            {
                return false;
            }

            if (pawn.Map != null)
            {
                WorkRelayAntiLoop.Blacklist(pawn, pawn.Map);
            }
            assigned.Remove(pawn.thingIDNumber);
            return true;
        }

        private static void TryLeaveForBetterWork(Pawn pawn, ThinkNode node, ref ThinkResult result)
        {
            if (Find.PlaySettings == null || !Find.PlaySettings.useWorkPriorities)
            {
                return;
            }
            if (PawnRelay.IsColonistWorkScanCooldown(pawn))
            {
                return;
            }

            WorkTypeDef localType = result.Job?.workGiverDef?.workType;
            if (localType == WorkTypeDefOf.Firefighter || localType == WorkTypeDefOf.Doctor)
            {
                return;
            }

            int maxDepth = StrataMod.Settings.priorityWorkMaxDepth;
            bool emergencyOverride = StrataMod.Settings.priorityWorkEmergency;
            if (maxDepth <= 0 && !emergencyOverride)
            {
                return;
            }

            int localPriority = localType != null && pawn.workSettings != null
                ? pawn.workSettings.GetPriority(localType)
                : 4;
            List<LevelGraph.LevelLink> links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            int best = -1;
            int bestPriority = int.MaxValue;
            WorkTypeDef bestType = null;
            bool bestEmergency = false;
            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                if (link.map == null || WorkRelayAntiLoop.IsBlacklisted(pawn, link.map))
                {
                    continue;
                }
                if (!WorkRelaySignals.TryBestSignalled(pawn, link.map, out int remotePriority, out WorkTypeDef remoteType, out bool emergency))
                {
                    continue;
                }

                bool emergencyTrip = emergencyOverride && emergency && remoteType != null
                    && (remoteType == WorkTypeDefOf.Firefighter || remoteType == WorkTypeDefOf.Doctor);
                bool better = maxDepth > 0 && link.depth <= maxDepth && remotePriority < localPriority;
                if (!emergencyTrip && !better)
                {
                    continue;
                }

                bool wins = best < 0
                    || (emergencyTrip && !bestEmergency)
                    || (emergencyTrip == bestEmergency && remotePriority < bestPriority)
                    || (emergencyTrip == bestEmergency && remotePriority == bestPriority && link.depth < links[best].depth);
                if (!wins)
                {
                    continue;
                }

                best = i;
                bestPriority = remotePriority;
                bestType = remoteType;
                bestEmergency = emergencyTrip;
            }

            if (best < 0)
            {
                return;
            }

            int cap = WorkRelaySignals.WorkClaimCap(links[best].map);
            Job job = PawnRelay.TryClaimAndRelay(pawn, links[best], RelayPurpose.Work, cap, intendedWork: bestType);
            if (job != null)
            {
                result = new ThinkResult(job, node, JobTag.MiscWork);
            }
        }

        private static bool TryAssignBest(Pawn pawn, ThinkNode node, ref ThinkResult result, out bool sawCandidate)
        {
            sawCandidate = false;
            List<LevelGraph.LevelLink> links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            int count = links.Count;
            if (count == 0)
            {
                return false;
            }

            var open = new bool[count];
            var priorities = new int[count];
            var types = new WorkTypeDef[count];
            for (int i = 0; i < count; i++)
            {
                Map map = links[i].map;
                if (map == null || !PawnRelay.HasWorkFor(pawn, map))
                {
                    continue;
                }

                open[i] = true;
                priorities[i] = 4;
                if (WorkRelaySignals.TryBestSignalled(pawn, map, out int priority, out WorkTypeDef type, out _))
                {
                    priorities[i] = priority;
                    types[i] = type;
                }
            }

            while (true)
            {
                int best = -1;
                for (int i = 0; i < count; i++)
                {
                    if (!open[i])
                    {
                        continue;
                    }
                    if (best < 0
                        || priorities[i] < priorities[best]
                        || (priorities[i] == priorities[best] && links[i].depth < links[best].depth))
                    {
                        best = i;
                    }
                }
                if (best < 0)
                {
                    return false;
                }

                sawCandidate = true;
                open[best] = false;
                if (types[best] == null)
                {
                    continue;
                }

                int cap = WorkRelaySignals.WorkClaimCap(links[best].map);
                Job job = PawnRelay.TryClaimAndRelay(pawn, links[best], RelayPurpose.Work, cap, intendedWork: types[best]);
                if (job == null)
                {
                    continue;
                }

                result = new ThinkResult(job, node, JobTag.MiscWork);
                return true;
            }
        }

        private static bool CanConsider(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Map != null && pawn.jobs != null;
        }

        private static Map FindMap(int mapId)
        {
            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return null;
            }
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == mapId)
                {
                    return maps[i];
                }
            }
            return null;
        }

        private static bool TryDetourToBlockedWork(Pawn pawn, WorkTypeDef wanted, ThinkNode node, ref ThinkResult result)
        {
            if (!TryFindBlockedWorkCell(pawn, wanted, out IntVec3 cell))
            {
                return false;
            }

            Job detour = DraftedPortalPathing.TryMakeSameMapDetourJob(pawn, cell);
            if (detour == null)
            {
                return false;
            }

            result = new ThinkResult(detour, node, JobTag.MiscWork);
            return true;
        }

        private static bool TryDetourToJob(Pawn pawn, Job job, ThinkNode node, ref ThinkResult result)
        {
            IntVec3 cell = job.targetA.Cell;
            if (!cell.IsValid)
            {
                return false;
            }

            Job detour = DraftedPortalPathing.TryMakeSameMapDetourJob(pawn, cell);
            if (detour == null)
            {
                return false;
            }

            result = new ThinkResult(detour, node, JobTag.MiscWork);
            return true;
        }

        private static bool JobTargetReachable(Pawn pawn, Job job)
        {
            if (pawn?.Map == null || job == null || !job.targetA.IsValid)
            {
                return true;
            }

            LocalTargetInfo target = job.targetA;
            if (target.HasThing)
            {
                Thing thing = target.Thing;
                if (thing == null || thing.MapHeld != pawn.Map)
                {
                    return false;
                }
                return pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly);
            }

            return target.Cell.InBounds(pawn.Map)
                && pawn.CanReach(target.Cell, PathEndMode.OnCell, Danger.Deadly);
        }

        private static bool TryFindBlockedWorkCell(Pawn pawn, WorkTypeDef type, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (type?.workGiversByPriority == null)
            {
                return false;
            }

            int best = int.MaxValue;
            for (int i = 0; i < type.workGiversByPriority.Count; i++)
            {
                if (type.workGiversByPriority[i]?.Worker is not WorkGiver_Scanner scanner)
                {
                    continue;
                }
                if (!GiverUsable(pawn, scanner) || scanner.ShouldSkip(pawn))
                {
                    continue;
                }

                ConsiderBlockedThings(pawn, scanner, ref cell, ref best);
                ConsiderBlockedCells(pawn, scanner, ref cell, ref best);
            }

            return cell.IsValid;
        }

        private static void ConsiderBlockedThings(Pawn pawn, WorkGiver_Scanner scanner, ref IntVec3 cell, ref int best)
        {
            System.Collections.IEnumerable things = scanner.PotentialWorkThingsGlobal(pawn);
            if (things == null)
            {
                return;
            }

            int seen = 0;
            foreach (Thing thing in things)
            {
                seen++;
                if (seen > WorkScanCap || thing == null || !thing.Spawned || thing.Map != pawn.Map)
                {
                    if (seen > WorkScanCap)
                    {
                        break;
                    }
                    continue;
                }
                if (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                int dist = pawn.Position.DistanceToSquared(thing.Position);
                if (dist < best)
                {
                    best = dist;
                    cell = thing.Position;
                }
            }
        }

        private static void ConsiderBlockedCells(Pawn pawn, WorkGiver_Scanner scanner, ref IntVec3 cell, ref int best)
        {
            IEnumerable<IntVec3> cells = scanner.PotentialWorkCellsGlobal(pawn);
            if (cells == null)
            {
                return;
            }

            int seen = 0;
            foreach (IntVec3 candidate in cells)
            {
                seen++;
                if (seen > WorkScanCap)
                {
                    break;
                }
                if (!candidate.InBounds(pawn.Map) || pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                int dist = pawn.Position.DistanceToSquared(candidate);
                if (dist < best)
                {
                    best = dist;
                    cell = candidate;
                }
            }
        }

        private static bool TryMakeWorkJob(Pawn pawn, WorkTypeDef type, out Job job)
        {
            job = null;
            if (pawn?.workSettings == null || type?.workGiversByPriority == null)
            {
                return false;
            }

            for (int i = 0; i < type.workGiversByPriority.Count; i++)
            {
                WorkGiver giver = type.workGiversByPriority[i]?.Worker;
                if (giver == null || !GiverUsable(pawn, giver) || giver.ShouldSkip(pawn))
                {
                    continue;
                }

                try
                {
                    Job direct = giver.NonScanJob(pawn);
                    if (direct != null)
                    {
                        job = direct;
                        return true;
                    }
                    if (giver is WorkGiver_Scanner scanner
                        && (TryScanThings(pawn, scanner, out job) || TryScanCells(pawn, scanner, out job)))
                    {
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    StrataLog.Verbose("[Strata] Job relay giver " + giver.def.defName
                        + " failed: " + e.GetType().Name);
                }
            }

            return false;
        }

        private static bool GiverUsable(Pawn pawn, WorkGiver giver)
        {
            WorkTypeDef type = giver.def?.workType;
            if (type != null && pawn.workSettings != null && !pawn.workSettings.WorkIsActive(type))
            {
                return false;
            }
            return giver.MissingRequiredCapacity(pawn) == null;
        }

        private static bool TryScanThings(Pawn pawn, WorkGiver_Scanner scanner, out Job job)
        {
            job = null;
            System.Collections.IEnumerable globals = scanner.PotentialWorkThingsGlobal(pawn);
            if (globals != null)
            {
                int seen = 0;
                foreach (Thing thing in globals)
                {
                    seen++;
                    if (seen > WorkScanCap)
                    {
                        break;
                    }
                    if (thing == null || !thing.Spawned || thing.Map != pawn.Map || thing.IsForbidden(pawn))
                    {
                        continue;
                    }
                    if (!scanner.HasJobOnThing(pawn, thing))
                    {
                        continue;
                    }

                    Job next = scanner.JobOnThing(pawn, thing);
                    if (next != null)
                    {
                        job = next;
                        return true;
                    }
                }
                return false;
            }

            ThingRequest request = scanner.PotentialWorkThingRequest;
            if (request.group == ThingRequestGroup.Undefined)
            {
                return false;
            }

            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                request,
                scanner.PathEndMode,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()),
                9999f,
                thing => thing != null && !thing.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, thing));
            if (found == null)
            {
                return false;
            }

            job = scanner.JobOnThing(pawn, found);
            return job != null;
        }

        private static bool TryScanCells(Pawn pawn, WorkGiver_Scanner scanner, out Job job)
        {
            job = null;
            IEnumerable<IntVec3> cells = scanner.PotentialWorkCellsGlobal(pawn);
            if (cells == null)
            {
                return false;
            }

            var nearest = new List<IntVec3>(WorkCellCheckCap);
            var nearestDist = new List<int>(WorkCellCheckCap);
            int seen = 0;
            foreach (IntVec3 cell in cells)
            {
                seen++;
                if (seen > WorkScanCap)
                {
                    break;
                }
                if (!cell.InBounds(pawn.Map) || !cell.InAllowedArea(pawn))
                {
                    continue;
                }

                int dist = pawn.Position.DistanceToSquared(cell);
                int slot = nearest.Count;
                if (slot < WorkCellCheckCap)
                {
                    nearest.Add(cell);
                    nearestDist.Add(dist);
                }
                else
                {
                    int worst = 0;
                    for (int i = 1; i < nearestDist.Count; i++)
                    {
                        if (nearestDist[i] > nearestDist[worst])
                        {
                            worst = i;
                        }
                    }
                    if (dist >= nearestDist[worst])
                    {
                        continue;
                    }
                    nearest[worst] = cell;
                    nearestDist[worst] = dist;
                }
            }

            for (int i = 0; i < nearest.Count; i++)
            {
                if (!scanner.HasJobOnCell(pawn, nearest[i]))
                {
                    continue;
                }

                Job next = scanner.JobOnCell(pawn, nearest[i]);
                if (next != null)
                {
                    job = next;
                    return true;
                }
            }

            return false;
        }

        internal static List<JobRelayAssignmentRecord> Capture()
        {
            var saved = new List<JobRelayAssignmentRecord>(assigned.Count);
            foreach (KeyValuePair<int, Assignment> pair in assigned)
            {
                Assignment assignment = pair.Value;
                if (assignment == null || string.IsNullOrEmpty(assignment.workDefName))
                {
                    continue;
                }
                saved.Add(new JobRelayAssignmentRecord
                {
                    pawnId = pair.Key,
                    mapId = assignment.mapId,
                    workDefName = assignment.workDefName,
                    stuckSince = assignment.stuckSince,
                });
            }
            return saved;
        }

        internal static void Restore(List<JobRelayAssignmentRecord> saved)
        {
            if (saved == null)
            {
                return;
            }
            for (int i = 0; i < saved.Count; i++)
            {
                JobRelayAssignmentRecord record = saved[i];
                if (record == null || record.pawnId <= 0 || record.mapId <= 0 || string.IsNullOrEmpty(record.workDefName))
                {
                    continue;
                }
                assigned[record.pawnId] = new Assignment
                {
                    mapId = record.mapId,
                    workDefName = record.workDefName,
                    stuckSince = record.stuckSince,
                };
            }
        }
    }
}
