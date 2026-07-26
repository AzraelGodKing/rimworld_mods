using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Soft-compat: ReGrowth Core Simple FX splash cache walks terrainGrid.topGrid
    // and indexes terrainDef without a null check. Strata gravship underdecks can
    // FinalizeInit before a silhouette paint (empty ValidSubstructure), leaving
    // null terrain cells — RebuildCache then NREs and aborts pocket generation.
    // Rain splashes are irrelevant on sealed underground / ship-linked floors.
    [StaticConstructorOnStartup]
    public static class StrataReGrowthSoftCompat
    {
        private static bool applied;

        static StrataReGrowthSoftCompat()
        {
            TryApply();
        }

        private static void TryApply()
        {
            if (applied)
            {
                return;
            }

            Type utility = AccessTools.TypeByName("ReGrowthCore.SplashesUtility");
            if (utility == null)
            {
                return;
            }

            applied = true;
            try
            {
                Harmony harmony = new Harmony("azraelgodking.strata.regrowth.splashes");
                int patched = 0;
                patched += TryPatch(harmony, utility, "RebuildCache",
                    nameof(RebuildCache_Prefix), nameof(RebuildCache_Finalizer));
                patched += TryPatch(harmony, utility, "UpdateCache",
                    nameof(UpdateCache_Prefix), finalizer: null);
                Log.Message("[Strata] ReGrowth SimpleFX splash soft-compat: patched "
                    + patched + " hook(s).");
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] ReGrowth splash soft-compat failed: " + e);
            }
        }

        private static int TryPatch(
            Harmony harmony,
            Type type,
            string methodName,
            string prefix,
            string finalizer)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                return 0;
            }

            HarmonyMethod prefixMethod = prefix != null
                ? new HarmonyMethod(typeof(StrataReGrowthSoftCompat), prefix)
                : null;
            HarmonyMethod finalizerMethod = finalizer != null
                ? new HarmonyMethod(typeof(StrataReGrowthSoftCompat), finalizer)
                : null;
            harmony.Patch(method, prefix: prefixMethod, finalizer: finalizerMethod);
            return 1;
        }

        public static bool RebuildCache_Prefix(Map map)
        {
            return !ShouldSkipSplashCache(map);
        }

        public static Exception RebuildCache_Finalizer(Exception __exception, Map map)
        {
            if (__exception == null || !ShouldSkipSplashCache(map))
            {
                return __exception;
            }

            Log.Warning("[Strata] Suppressed ReGrowth splash cache error on linked floor: "
                + __exception.GetType().Name + ": " + __exception.Message);
            return null;
        }

        public static bool UpdateCache_Prefix(Map map)
        {
            return !ShouldSkipSplashCache(map);
        }

        internal static bool ShouldSkipSplashCache(Map map)
        {
            if (map == null)
            {
                return true;
            }

            if (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return true;
            }

            if (StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return true;
            }

            MapGeneratorDef gen = map.generatorDef;
            if (gen?.defName != null && gen.defName.StartsWith("Strata_"))
            {
                return true;
            }

            return false;
        }
    }
}
