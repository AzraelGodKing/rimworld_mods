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

        // Resolved once: these run from TicksPerMove and MassUtility.Capacity
        // for every pawn, so compare def references, not defName strings.
        private static bool defsResolved;
        private static ThingDef handcartDef;
        private static ThingDef sledgeDef;
        private static ThingDef loadingDockDef;
        private static TerrainDef packedHaulwayDef;
        private static TerrainDef homesteaderGravelDef;

        private static void ResolveDefs()
        {
            if (defsResolved)
            {
                return;
            }
            handcartDef = DefDatabase<ThingDef>.GetNamedSilentFail("Strata_Handcart");
            sledgeDef = DefDatabase<ThingDef>.GetNamedSilentFail("Strata_Sledge");
            loadingDockDef = DefDatabase<ThingDef>.GetNamedSilentFail("Strata_LoadingDock");
            packedHaulwayDef = DefDatabase<TerrainDef>.GetNamedSilentFail("Strata_PackedHaulway");
            // Homesteader packed gravel is the same idea. No Homesteader -> null.
            homesteaderGravelDef = DefDatabase<TerrainDef>.GetNamedSilentFail("Homesteader_PackedGravel");
            defsResolved = true;
        }

        private const int Handcart = 1;
        private const int Sledge = 2;

        // Bit flags: Handcart and/or Sledge in hand or inventory.
        private static int Carts(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }
            ResolveDefs();
            int carts = CartFlag(pawn.carryTracker?.CarriedThing?.def);
            ThingOwner inv = pawn.inventory?.innerContainer;
            if (inv != null)
            {
                for (int i = 0; i < inv.Count && carts != (Handcart | Sledge); i++)
                {
                    carts |= CartFlag(inv[i]?.def);
                }
            }
            return carts;
        }

        private static int CartFlag(ThingDef def)
        {
            if (def == null)
            {
                return 0;
            }
            if (def == handcartDef)
            {
                return Handcart;
            }
            return def == sledgeDef ? Sledge : 0;
        }

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
            int carts = Carts(pawn);
            if ((carts & Handcart) != 0)
            {
                bonus += 80f;
            }
            if ((carts & Sledge) != 0)
            {
                bonus += 120f;
            }
            return bonus;
        }

        // Movement multiplier for a cart in hand or inventory; 1 when none.
        public static float CartSlowdown(Pawn pawn)
        {
            int carts = Carts(pawn);
            if ((carts & Sledge) != 0)
            {
                return 1.45f;
            }
            return (carts & Handcart) != 0 ? 1.28f : 1f;
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
                if (n != null && n.StartsWith("Homesteader_", System.StringComparison.Ordinal) && n.IndexOf("Crate", System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return pawn.carryTracker?.CarriedThing?.def?.defName is string c
                && c.StartsWith("Homesteader_", System.StringComparison.Ordinal)
                && c.IndexOf("Crate", System.StringComparison.Ordinal) >= 0;
        }

        public static bool OnPackedHaulway(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return false;
            }
            ResolveDefs();
            TerrainDef t = pawn.Position.GetTerrain(pawn.Map);
            return t != null && (t == packedHaulwayDef || t == homesteaderGravelDef);
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
            ResolveDefs();
            if (map == null || loadingDockDef == null)
            {
                return false;
            }
            // Most maps have no dock: skip the 9-cell scan (ThingsOfDef is a lookup).
            if (map.listerThings.ThingsOfDef(loadingDockDef).Count == 0)
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
                if (b != null && b.def == loadingDockDef)
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
            float slow = HaulRelayCapacity.CartSlowdown(__instance);
            if (slow > 1f)
            {
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
