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
            if (SleepRelay.ShouldYieldToCommute(pawn))
            {
                // Do not replace an in-progress stair commute with floor sleep —
                // ground OR a random unoccupied bed on this floor (AZR-231).
                // ForceSleepNow already nulls the whole result while yielding;
                // GetRest must match or owned-bed trips keep a local bunk.
                if (__result != null
                    && (__result.def == JobDefOf.LayDown || __result.forceSleep))
                {
                    __result = null;
                }

                return;
            }

            Job commute = SleepRelay.TryMakeJob(pawn, __result);
            if (commute != null)
            {
                __result = commute;
                return;
            }
            if (SleepRelay.ShouldBlockGroundSleep(pawn, __result))
            {
                __result = null;
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
            if (SleepRelay.ShouldYieldToCommute(pawn))
            {
                __result = null;
                return;
            }

            Job commute = SleepRelay.TryMakeJob(pawn, __result);
            if (commute != null)
            {
                __result = commute;
                return;
            }
            if (SleepRelay.ShouldBlockGroundSleep(pawn, __result))
            {
                __result = null;
            }
        }
    }
}
