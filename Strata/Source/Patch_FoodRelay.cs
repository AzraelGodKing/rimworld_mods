using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A hungry pawn who can't find food on this level heads for a level that
    // has some instead of starving next to a staircase.
    [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
    public static class Patch_FoodRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !PawnRelay.CanRelay(pawn))
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!PawnRelay.HasFoodFor(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Food, 3);
                if (job != null)
                {
                    __result = job;
                    return;
                }
            }
        }
    }
}
