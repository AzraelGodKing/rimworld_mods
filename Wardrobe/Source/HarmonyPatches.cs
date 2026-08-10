using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wardrobe
{
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class Patch_OptimizeApparel_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (WardrobeUtility.IsManaged(pawn))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}
