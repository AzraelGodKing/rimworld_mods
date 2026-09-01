using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    /// <summary>AZR-65 — noisy rooms drain rest while sleeping.</summary>
    [HarmonyPatch(typeof(Need_Rest), nameof(Need_Rest.NeedInterval))]
    public static class Patch_Need_Rest_QuietHours
    {
        private static readonly AccessTools.FieldRef<Need, Pawn> PawnField =
            AccessTools.FieldRefAccess<Need, Pawn>("pawn");

        public static void Postfix(Need_Rest __instance)
        {
            Pawn pawn = PawnField(__instance);
            if (pawn == null) return;
            QuietHoursUtility.DrainRestIfNoisy(__instance, pawn);
            EstateUtility.TickUnclaimedBeds(pawn);
        }
    }
}
