using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // AZR-202 first slice — extra carry mass at a loading dock so the haul
    // relay moves more than one armful per stair trip.
    internal static class HaulRelayCapacity
    {
        private const float DockBonusKg = 35f;

        public static float BonusKg(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return 0f;
            }
            return NearDock(pawn.Position, pawn.Map) ? DockBonusKg : 0f;
        }

        public static bool NearDock(IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return false;
            }
            foreach (IntVec3 offset in GenAdj.AdjacentCellsAndInside)
            {
                IntVec3 n = cell + offset;
                if (!n.InBounds(map))
                {
                    continue;
                }
                Building b = n.GetEdifice(map);
                if (b != null && b.def.defName == "Strata_LoadingDock")
                {
                    return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity))]
    public static class Patch_HaulRelayCapacity
    {
        public static void Postfix(Pawn p, ref float __result)
        {
            __result += HaulRelayCapacity.BonusKg(p);
        }
    }
}
