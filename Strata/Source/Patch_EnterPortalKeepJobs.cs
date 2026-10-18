using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Vanilla EnterPortal calls ClearPrioritizedWorkAndJobQueue after OnEntered,
    // and that returns every queued job to the pool. Each pawn keeps their own
    // snapshot so two pawns taking stairs on the same tick cannot mix orders.
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.ClearQueuedJobs))]
    public static class Patch_EnterPortalKeepJobs
    {
        private static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> pawnField =
            AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        private static readonly Dictionary<int, List<Kept>> keptByPawn = new Dictionary<int, List<Kept>>();

        private struct Kept
        {
            public Job job;
            public JobTag? tag;
        }

        public static void Prefix(Pawn_JobTracker __instance)
        {
            Pawn pawn = pawnField(__instance);
            if (pawn?.CurJobDef != JobDefOf.EnterPortal)
            {
                return;
            }
            if (keptByPawn.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }
            keptByPawn[pawn.thingIDNumber] = new List<Kept>();
        }

        public static void Postfix(Pawn_JobTracker __instance)
        {
            Pawn pawn = pawnField(__instance);
            if (pawn == null || !keptByPawn.TryGetValue(pawn.thingIDNumber, out List<Kept> kept))
            {
                return;
            }
            keptByPawn.Remove(pawn.thingIDNumber);
            if (pawn.jobs == null)
            {
                Release(kept);
                return;
            }

            int departure = PortalRelayChain.DepartureMapId(pawn);
            for (int i = 0; i < kept.Count; i++)
            {
                Job job = kept[i].job;
                if (job == null)
                {
                    continue;
                }
                if (!CrossLevelOrderedJobs.KeepQueued(pawn, job, kept[i].tag, departure))
                {
                    JobMaker.ReturnToPool(job);
                }
            }
        }

        public static void ResetSession()
        {
            foreach (KeyValuePair<int, List<Kept>> pair in keptByPawn)
            {
                Release(pair.Value);
            }
            keptByPawn.Clear();
        }

        private static void Release(List<Kept> kept)
        {
            if (kept == null)
            {
                return;
            }
            for (int i = 0; i < kept.Count; i++)
            {
                if (kept[i].job != null)
                {
                    JobMaker.ReturnToPool(kept[i].job);
                }
            }
        }

        // Skip pooling while this pawn's portal clear is in progress.
        [HarmonyPatch(typeof(QueuedJob), nameof(QueuedJob.Cleanup))]
        public static class Patch_KeepQueuedJobDuringPortal
        {
            public static bool Prefix(QueuedJob __instance, Pawn pawn)
            {
                if (__instance?.job == null || pawn == null
                    || !keptByPawn.TryGetValue(pawn.thingIDNumber, out List<Kept> kept))
                {
                    return true;
                }
                if (__instance.job.def == JobDefOf.EnterPortal)
                {
                    return true;
                }
                kept.Add(new Kept
                {
                    job = __instance.job,
                    tag = __instance.tag,
                });
                pawn.ClearReservationsForJob(__instance.job);
                __instance.job = null;
                return false;
            }
        }
    }
}
