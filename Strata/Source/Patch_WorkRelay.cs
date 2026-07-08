using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // When a colonist finds no work on their own level, look for work signals on
    // linked levels and send them down (or up) the stairs. Vanilla AI picks the
    // actual job the moment they arrive.
    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
    public static class Patch_WorkRelay
    {
        public static void Postfix(JobGiver_Work __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (__result.Job != null || __instance.emergency)
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!PawnRelay.HasWorkFor(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.MakeRelayJob(pawn, link.firstStep);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                }
                return;
            }
        }
    }
}
