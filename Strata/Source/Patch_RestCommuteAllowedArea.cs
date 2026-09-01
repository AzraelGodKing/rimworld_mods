using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Allowed areas are per-map. A B1 bedroom zone often does not include the
    // shaft (or any cell on the bed's floor), so vanilla CanReach refuses the
    // stair and JobGiver_GetRest lays them down in the dirt. Rest commute to an
    // assigned bed is going home — treat the shaft and that bed as allowed.
    [HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.InAllowedArea))]
    public static class Patch_RestCommuteInAllowedArea
    {
        public static void Postfix(IntVec3 c, Pawn pawn, ref bool __result)
        {
            if (__result || pawn == null)
            {
                return;
            }

            if (SleepRelay.ShouldBypassAllowedAreaForRest(pawn))
            {
                __result = true;
            }
        }
    }
}
