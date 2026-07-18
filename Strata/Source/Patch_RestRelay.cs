using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_RestRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (RestBedCommute.ShouldYieldToCommute(pawn))
            {
                // Do not replace an in-progress stair commute with floor sleep.
                if (__result != null
                    && __result.def == JobDefOf.LayDown
                    && __result.targetA.Thing is not Building_Bed)
                {
                    __result = null;
                }

                return;
            }

            Job commute = RestBedCommute.TryMakeJob(pawn, __result);
            if (commute != null)
            {
                __result = commute;
            }
        }
    }

    // Exhaustion collapses via ForceSleepNow (never calls GetRest). It also
    // re-fires every think pass — without yielding, that stomps EnterPortal
    // and the pawn passes out on the floor mid-commute.
    [HarmonyPatch(typeof(JobGiver_ForceSleepNow), "TryGiveJob")]
    public static class Patch_ForceSleepNow_RestRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (RestBedCommute.ShouldYieldToCommute(pawn))
            {
                __result = null;
                return;
            }

            Job commute = RestBedCommute.TryMakeJob(pawn, __result);
            if (commute != null)
            {
                __result = commute;
            }
        }
    }
}
