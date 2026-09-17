using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Niceties
{
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), nameof(WorkGiver_Scanner.JobOnThing))]
    internal static class Patch_OpenBuilds_JobOnThing
    {
        private static void Postfix(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (__result == null || forced || !OpenBuilds.Enabled || !OpenBuilds.IsImpassableBuild(t))
            {
                return;
            }

            OpenBuildsResult enclose = OpenBuilds.WouldEnclose(t, pawn);
            if (enclose.EnclosesThings)
            {
                __result = null;
                JobFailReason.Is("Niceties_OpenBuilds_WouldEnclose".Translate());
            }
        }
    }

    [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
    [HarmonyPriority(Priority.First)]
    internal static class Patch_OpenBuilds_CompleteConstruction
    {
        private static bool Prefix(Frame __instance, Pawn worker)
        {
            if (!OpenBuilds.Enabled || worker == null || !OpenBuilds.IsImpassableBuild(__instance))
            {
                return true;
            }

            if (worker.CurJob != null && worker.CurJob.playerForced)
            {
                return true;
            }

            OpenBuildsResult enclose = OpenBuilds.WouldEnclose(__instance, worker);
            if (enclose.EnclosesThings)
            {
                // Skip CompleteConstruction; ReadyForNextToil ends the finish job.
                // No EndCurrentJob here — that would re-enter mid-toil.
                return false;
            }

            if (__instance.Position == worker.Position || enclose.EnclosesSelf)
            {
                return !OpenBuilds.QueueStepAsideThenRetry(worker, __instance);
            }

            return true;
        }
    }

    /// <summary>
    /// After a deferred step-aside is queued, CompleteConstruction's caller still
    /// invokes ReadyForNextToil. Skip that so the finish job stays current until
    /// GameComponentTick replaces it with Goto + queued resume.
    /// </summary>
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.ReadyForNextToil))]
    internal static class Patch_OpenBuilds_ReadyForNextToil
    {
        private static bool Prefix(JobDriver __instance)
        {
            if (!(__instance is JobDriver_ConstructFinishFrame))
            {
                return true;
            }

            Pawn pawn = __instance.pawn;
            return pawn == null || !OpenBuilds.HasPendingFor(pawn);
        }
    }
}
