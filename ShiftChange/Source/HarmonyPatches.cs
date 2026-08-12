using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ShiftChange
{
    /// <summary>
    /// While Shift Change is managing a pawn, suppress vanilla apparel optimization
    /// so it does not fight sleepwear / work kits mid-shift.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class Patch_OptimizeApparel_SkipManaged
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp != null && comp.IsManaged(pawn))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}
