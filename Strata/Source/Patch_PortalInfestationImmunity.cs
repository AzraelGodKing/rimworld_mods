using System.Reflection;
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
        private static bool Prepare()
        {
            MethodInfo method = AccessTools.Method(typeof(InfestationCellFinder), "GetScoreAt");
            if (method == null)
            {
                Log.Warning("[Strata] InfestationCellFinder.GetScoreAt not found; skipping portal infestation patch.");
                return false;
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(IntVec3) || ps[1].ParameterType != typeof(Map))
            {
                Log.Warning("[Strata] Unexpected InfestationCellFinder.GetScoreAt signature; skipping portal infestation patch.");
                return false;
            }
            return true;
        }

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
        private static bool Prepare()
        {
            MethodInfo method = AccessTools.Method(typeof(CompSpawnerHives), "CanSpawnHiveAt");
            if (method == null)
            {
                Log.Warning("[Strata] CompSpawnerHives.CanSpawnHiveAt not found; skipping portal hive patch.");
                return false;
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length < 2 || ps[0].Name != "c")
            {
                Log.Warning("[Strata] Unexpected CompSpawnerHives.CanSpawnHiveAt signature; skipping portal hive patch.");
                return false;
            }
            return true;
        }

        public static void Postfix(IntVec3 c, Map map, ref bool __result)
        {
            if (__result && StrataPortalUtility.CellBlockedByProtectedPortal(map, c))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[]
    {
        typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4),
        typeof(WipeMode), typeof(bool), typeof(bool)
    })]
    public static class Patch_GenSpawnHiveOnPortalBlock
    {
        private static bool Prepare()
        {
            MethodInfo method = AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[]
            {
                typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4),
                typeof(WipeMode), typeof(bool), typeof(bool)
            });
            if (method == null)
            {
                Log.Warning("[Strata] GenSpawn.Spawn hive overload not found; skipping portal hive spawn patch.");
                return false;
            }
            return true;
        }

        public static bool Prefix(
            Thing newThing,
            IntVec3 loc,
            Map map,
            Rot4 rot,
            WipeMode wipeMode,
            ref Thing __result)
        {
            // Only when placing another Strata portal. WipeMode cannot remove
            // destroyable=false shafts — but running this for pawns/items would
            // ForceDestroy the landing underfoot when colonists EnterPortal.
            if (newThing is MapPortal
                && newThing.def?.defName != null
                && newThing.def.defName.StartsWith("Strata_")
                && map != null
                && (wipeMode == WipeMode.Vanish
                    || wipeMode == WipeMode.VanishOrMoveAside
                    || wipeMode == WipeMode.FullRefund))
            {
                StrataPortalUtility.PrefireWipeStrataPortals(
                    map, loc, rot, newThing.def.Size, newThing);
            }

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

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn), new[] { typeof(DestroyMode) })]
    public static class Patch_PortalDeSpawnImmunity
    {
        public static bool Prefix(Thing __instance, DestroyMode mode)
        {
            return StrataPortalUtility.ShouldAllowPortalDeSpawn(__instance, mode);
        }
    }
}
