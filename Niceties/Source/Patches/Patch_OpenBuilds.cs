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
                worker.jobs.EndCurrentJob(JobCondition.Incompletable);
                return false;
            }

            if (__instance.Position == worker.Position || enclose.EnclosesSelf)
            {
                return !OpenBuilds.StepAsideThenRetry(worker, __instance);
            }

            return true;
        }
    }
}
