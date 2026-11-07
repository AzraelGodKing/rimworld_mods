#nullable enable
using HarmonyLib;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// A run of wonderful dates makes a proposal more likely (AZR-574): +25% per
    /// wonderful date in a row, up to +100%. A postfix on the weight, no transpiler.
    /// </summary>
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.RandomSelectionWeight))]
    public static class Patch_MarriageProposal_Weight
    {
        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (__result <= 0f || initiator == null || recipient == null)
            {
                return;
            }
            CoupleHistory? h = DateNightHistory.Get(initiator, recipient);
            if (h != null && h.wonderfulStreak > 0)
            {
                __result *= DateNightPure.ProposalMultiplier(h.wonderfulStreak);
            }
        }
    }
}
