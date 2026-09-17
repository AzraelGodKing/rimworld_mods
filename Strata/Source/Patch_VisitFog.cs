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
        private static readonly AccessTools.FieldRef<FogGrid, Map> MapField =
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

    // AZR-232 — dug cells can stay fogged when vanilla skips FloodUnfogAdjacent.
    // Notify_FogBlockerRemoved only floods if an already-unfogged walkable neighbor
    // exists; expanding through still-fogged dug cells (or mass Destroy carve order)
    // fails that check and leaves open floor + mining chunks under fog. Force a
    // reveal when the blocker is gone and the cell is still dark.
    [HarmonyPatch(typeof(FogGrid), nameof(FogGrid.Notify_FogBlockerRemoved))]
    public static class Patch_FogBlockerRemoved_DugReveal
    {
        private static readonly AccessTools.FieldRef<FogGrid, Map> MapField =
            AccessTools.FieldRefAccess<FogGrid, Map>("map");

        public static void Postfix(FogGrid __instance, Thing thing)
        {
            if (Current.ProgramState != ProgramState.Playing || thing == null)
            {
                return;
            }

            Map map = MapField(__instance);
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            IntVec3 cell = thing.Position;
            if (!cell.InBounds(map) || !__instance.IsFogged(cell))
            {
                return;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null && edifice.def.MakeFog)
            {
                return;
            }

            __instance.FloodUnfogAdjacent(cell, sendLetters: false);
        }

        // Load heal for saves that already have stuck dug fog next to clear space.
        // Leaves sealed empty pockets behind undug rock fogged (hidden chambers).
        public static void HealStuckDugFog(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }

            FogGrid fog = map.fogGrid;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!fog.IsFogged(cell))
                {
                    continue;
                }

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def.MakeFog)
                {
                    continue;
                }

                bool nearClear = false;
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 n = cell + GenAdj.AdjacentCells[i];
                    if (!n.InBounds(map) || fog.IsFogged(n))
                    {
                        continue;
                    }

                    Building neighbor = n.GetEdifice(map);
                    if (neighbor == null || !neighbor.def.MakeFog || neighbor.def.IsDoor)
                    {
                        nearClear = true;
                        break;
                    }
                }

                if (nearClear)
                {
                    fog.FloodUnfogAdjacent(cell, sendLetters: false);
                }
            }
        }
    }
}
