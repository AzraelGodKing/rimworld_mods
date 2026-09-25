using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Extra carry at a dock so a stair trip is more than one armful.
    // Handcart / sledge in inventory: more mass, slower walk — they ride
    // the shaft because they are items, not a second pawn. Homesteader
    // crates at the dock add a little more if that mod is in; without it
    // the crate check just does nothing.
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
            bool pawnAtDock = NearDock(pawn.Position, pawn.Map);
            if (pawnAtDock)
            {
                bonus += DockBonusKg;
                if (HasHomesteaderCrate(pawn))
                {
                    bonus += 15f;
                }
            }
            else if (StairJobPortalNearDock(pawn))
            {
                // Shaft has a dock: one trip from the stockpile, not an armful
                // that only fattens when you already stand on the pad.
                bonus += DockBonusKg;
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
            // Homesteader packed gravel is the same idea. No Homesteader → skip.
            return t.defName == "Strata_PackedHaulway"
                || t.defName == "Homesteader_PackedGravel";
        }

        public static bool IsCarryingHaul(Pawn pawn)
        {
            return pawn?.carryTracker?.CarriedThing != null;
        }

        public static bool StairJobPortalNearDock(Pawn pawn)
        {
            Job job = pawn?.jobs?.curJob;
            if (job == null || job.def != StrataDefOf.Strata_HaulToLevel)
            {
                return false;
            }
            Thing portal = job.targetB.Thing;
            return portal != null && portal.Spawned && NearDock(portal.Position, portal.Map);
        }

        public static int CountForStairHaul(Pawn pawn, Thing t, MapPortal portal)
        {
            if (pawn == null || t == null)
            {
                return 1;
            }
            float cap = MassUtility.Capacity(pawn);
            if (portal != null && portal.Spawned
                && !NearDock(pawn.Position, pawn.Map)
                && NearDock(portal.Position, portal.Map))
            {
                cap += DockBonusKg;
            }
            float used = MassUtility.GearAndInventoryMass(pawn);
            float mass = t.GetStatValue(StatDefOf.Mass);
            int n;
            if (mass <= 0.0001f)
            {
                n = t.stackCount;
            }
            else
            {
                n = (int)((cap - used) / mass);
            }
            if (n < 1)
            {
                n = 1;
            }
            if (n > t.stackCount)
            {
                n = t.stackCount;
            }
            return n;
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

    // 1.6: TicksPerMove returns float (was int). AZR-345.
    [HarmonyPatch(typeof(Pawn), "TicksPerMove")]
    public static class Patch_PackedHaulwayMove
    {
        public static void Postfix(Pawn __instance, ref float __result)
        {
            if (__result <= 1f)
            {
                return;
            }
            if (HaulRelayCapacity.PushingCart(__instance))
            {
                float slow = HaulRelayCapacity.HasCart(__instance, "Strata_Sledge") ? 1.45f : 1.28f;
                __result = Math.Max(1f, __result * slow);
            }
            if (HaulRelayCapacity.IsCarryingHaul(__instance)
                && HaulRelayCapacity.OnPackedHaulway(__instance))
            {
                __result = Math.Max(1f, __result * 0.82f);
            }
        }
    }
}
