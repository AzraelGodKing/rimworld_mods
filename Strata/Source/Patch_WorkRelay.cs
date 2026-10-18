using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Colonist work decisions go through JobRelay. This patch only hands the
    // vanilla result across.
    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
    public static class Patch_WorkRelay
    {
        public static void Postfix(JobGiver_Work __instance, Pawn pawn, ref ThinkResult __result)
        {
            JobRelay.Apply(pawn, __instance, __instance.emergency, ref __result);
        }
    }
}
