using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // AZR-57 — undug rock on underground levels stays fogged until mined.
    // Vanilla mining unfogs neighboring rock; we put that darkness back.
    [HarmonyPatch(typeof(FogGrid), nameof(FogGrid.Unfog))]
    public static class Patch_VisitFog
    {
        internal static readonly AccessTools.FieldRef<FogGrid, Map> MapField =
            AccessTools.FieldRefAccess<FogGrid, Map>("map");

        private static bool suppressing;

        public static void Postfix(FogGrid __instance, IntVec3 c)
        {
            if (suppressing)
            {
                return;
            }

            Map map = MapField(__instance);
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            Building edifice = c.GetEdifice(map);
            if (edifice == null || !edifice.def.mineable)
            {
                return;
            }

            suppressing = true;
            try
            {
                __instance.Refog(new CellRect(c.x, c.z, 1, 1));
            }
            finally
            {
                suppressing = false;
            }
        }
    }

    // Mining a fog blocker should open the adjacent walkable cells. Vanilla
    // does this; VisitFog's Refog on leftover rock made dug walls look inverted.
    [HarmonyPatch(typeof(FogGrid), nameof(FogGrid.Notify_FogBlockerRemoved))]
    public static class Patch_VisitFog_BlockerRemoved
    {
        public static void Postfix(FogGrid __instance, IntVec3 c)
        {
            Map map = Patch_VisitFog.MapField(__instance);
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            FloodFillerFog.FloodUnfog(c, map);
        }
    }
}
