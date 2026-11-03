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
        public static void Postfix(JobGiver_GetFood __instance, Pawn pawn, ref Job __result)
        {
            if (__result != null || !PawnRelay.CanLeaveWorkForNeed(pawn))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.foodRelayEnabled)
            {
                return;
            }
            if (CrossFloorJobScan.Replaces(RelayPurpose.Food))
            {
                __result = CrossFloorJobScan.TryScanJobGiver(pawn, __instance, RelayPurpose.Food, 3);
                return;
            }
            var links = LevelGraph.CopyLinks(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksByRoles(links, LevelRole.Freezer, LevelRole.Farm);
            foreach (LevelGraph.LevelLink link in links)
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
