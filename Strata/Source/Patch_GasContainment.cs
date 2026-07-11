using HarmonyLib;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.GasCanMoveTo))]
    public static class Patch_GasCanMoveTo
    {
        public static void Postfix(Map ___map, IntVec3 cell, ref bool __result)
        {
            if (__result && StrataPortalUtility.CellBlockedBySealedPortal(___map, cell))
            {
                __result = false;
            }
        }
    }
}
