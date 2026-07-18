using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Infestations, hive diggers, and similar events must never claim or
    // destroy Strata stairwells, elevators, dig shafts, or their landings.
    [HarmonyPatch(typeof(InfestationCellFinder), "GetScoreAt")]
    public static class Patch_InfestationScoreAtPortalBlock
    {
        public static void Postfix(IntVec3 cell, Map map, ref float __result)
        {
            if (__result > 0f && StrataPortalUtility.CellBlockedByProtectedPortal(map, cell))
            {
                __result = -1f;
            }
        }
    }

    [HarmonyPatch(typeof(CompSpawnerHives), "CanSpawnHiveAt")]
    public static class Patch_CanSpawnHiveAtPortalBlock
    {
        public static void Postfix(IntVec3 loc, Map map, ref bool __result)
        {
            if (__result && StrataPortalUtility.CellBlockedByProtectedPortal(map, loc))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4) })]
    public static class Patch_GenSpawnHiveOnPortalBlock
    {
        public static bool Prefix(Thing newThing, IntVec3 loc, Map map, Rot4 rot, ref Thing __result)
        {
            if (newThing == null || map == null)
            {
                return true;
            }
            ThingDef def = newThing.def;
            if (def != ThingDefOf.Hive && def != ThingDefOf.TunnelHiveSpawner)
            {
                return true;
            }
            if (!StrataPortalUtility.CellBlockedByProtectedPortal(map, loc)
                && !StrataPortalUtility.RectBlockedByProtectedPortal(map, loc, rot, def.size))
            {
                return true;
            }
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
    public static class Patch_PortalDestroyImmunity
    {
        public static bool Prefix(Thing __instance, DestroyMode mode)
        {
            return !StrataPortalUtility.ShouldBlockPortalDestroy(__instance, mode);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Patch_PortalDeSpawnImmunity
    {
        public static bool Prefix(Thing __instance)
        {
            if (!StrataPortalUtility.IsProtectedPortal(__instance))
            {
                return true;
            }
            // Allow our own map-gen / landing repair despawn cycles.
            if (PocketMapUtility.currentlyGeneratingPortal != null)
            {
                return true;
            }
            return false;
        }
    }
}
