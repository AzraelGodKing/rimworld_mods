using System;
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
            float bonus = 0f;
            if (NearDock(pawn.Position, pawn.Map))
            {
                bonus += DockBonusKg;
                if (HasHomesteaderCrate(pawn))
                {
                    bonus += 15f;
                }
            }
            if (HasCart(pawn, "Strata_Handcart"))
            {
                bonus += 80f;
            }
            if (HasCart(pawn, "Strata_Sledge"))
            {
                bonus += 120f;
            }
            return bonus;
        }

        public static bool PushingCart(Pawn pawn)
        {
            return HasCart(pawn, "Strata_Handcart") || HasCart(pawn, "Strata_Sledge");
        }

        public static bool HasCart(Pawn pawn, string defName)
        {
            if (pawn == null)
            {
                return false;
            }
            if (pawn.carryTracker?.CarriedThing?.def?.defName == defName)
            {
                return true;
            }
            if (pawn.inventory?.innerContainer == null)
            {
                return false;
            }
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing?.def?.defName == defName)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasHomesteaderCrate(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return false;
            }
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                string n = thing?.def?.defName;
                if (n != null && n.StartsWith("Homesteader_") && n.IndexOf("Crate", System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return pawn.carryTracker?.CarriedThing?.def?.defName is string c
                && c.StartsWith("Homesteader_")
                && c.IndexOf("Crate", System.StringComparison.Ordinal) >= 0;
        }

        public static bool OnPackedHaulway(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return false;
            }
            TerrainDef t = pawn.Position.GetTerrain(pawn.Map);
            if (t == null)
            {
                return false;
            }
            // Homesteader packed gravel is the same idea — fail-open if that mod is absent.
            return t.defName == "Strata_PackedHaulway"
                || t.defName == "Homesteader_PackedGravel";
        }

        public static bool IsCarryingHaul(Pawn pawn)
        {
            return pawn?.carryTracker?.CarriedThing != null;
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

    [HarmonyPatch(typeof(Pawn), "TicksPerMove")]
    public static class Patch_PackedHaulwayMove
    {
        public static void Postfix(Pawn __instance, ref int __result)
        {
            if (__result <= 1)
            {
                return;
            }
            if (HaulRelayCapacity.PushingCart(__instance))
            {
                float slow = HaulRelayCapacity.HasCart(__instance, "Strata_Sledge") ? 1.45f : 1.28f;
                __result = Math.Max(1, (int)(__result * slow));
            }
            if (HaulRelayCapacity.IsCarryingHaul(__instance)
                && HaulRelayCapacity.OnPackedHaulway(__instance))
            {
                __result = Math.Max(1, (int)(__result * 0.82f));
            }
        }
    }
}
