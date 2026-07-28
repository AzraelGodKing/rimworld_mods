using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    // DoPawnsToxicDamage is private in vanilla; patch by name.
    [HarmonyPatch(typeof(GameCondition_ToxicFallout), "DoPawnsToxicDamage")]
    public static class Patch_ToxicFallout_DoPawnsToxicDamage
    {
        public static bool Prefix(Map map) =>
            !HazardProtection.BarrierProtecting(map);
    }

    [HarmonyPatch(typeof(GameCondition_ToxicFallout), nameof(GameCondition_ToxicFallout.DoCellSteadyEffects))]
    public static class Patch_ToxicFallout_DoCellSteadyEffects
    {
        public static bool Prefix(Map map) =>
            !HazardProtection.BarrierProtecting(map);
    }

    [HarmonyPatch(typeof(GameCondition_HeatWave), nameof(GameCondition_HeatWave.TemperatureOffset))]
    public static class Patch_HeatWave_TemperatureOffset
    {
        public static void Postfix(GameCondition_HeatWave __instance, ref float __result)
        {
            ZeroIfClimateProtected(__instance, ref __result);
        }

        internal static void ZeroIfClimateProtected(GameCondition condition, ref float result)
        {
            if (result == 0f || condition == null)
            {
                return;
            }
            foreach (Map map in condition.AffectedMaps)
            {
                if (HazardProtection.ClimateProtecting(map))
                {
                    result = 0f;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameCondition_ColdSnap), nameof(GameCondition_ColdSnap.TemperatureOffset))]
    public static class Patch_ColdSnap_TemperatureOffset
    {
        public static void Postfix(GameCondition_ColdSnap __instance, ref float __result) =>
            Patch_HeatWave_TemperatureOffset.ZeroIfClimateProtected(__instance, ref __result);
    }

    [HarmonyPatch(typeof(GameCondition_VolcanicWinter), nameof(GameCondition_VolcanicWinter.TemperatureOffset))]
    public static class Patch_VolcanicWinter_TemperatureOffset
    {
        public static void Postfix(GameCondition_VolcanicWinter __instance, ref float __result) =>
            Patch_HeatWave_TemperatureOffset.ZeroIfClimateProtected(__instance, ref __result);
    }

    [HarmonyPatch(typeof(GameCondition_HeatDome), nameof(GameCondition_HeatDome.TemperatureOffset))]
    public static class Patch_HeatDome_TemperatureOffset
    {
        public static void Postfix(GameCondition_HeatDome __instance, ref float __result) =>
            Patch_HeatWave_TemperatureOffset.ZeroIfClimateProtected(__instance, ref __result);
    }

    [HarmonyPatch(typeof(GameCondition_PolarFront), nameof(GameCondition_PolarFront.TemperatureOffset))]
    public static class Patch_PolarFront_TemperatureOffset
    {
        public static void Postfix(GameCondition_PolarFront __instance, ref float __result) =>
            Patch_HeatWave_TemperatureOffset.ZeroIfClimateProtected(__instance, ref __result);
    }

    [HarmonyPatch(typeof(GameCondition_NoSunlight), nameof(GameCondition_NoSunlight.SkyTarget))]
    public static class Patch_NoSunlight_SkyTarget
    {
        public static void Postfix(GameCondition_NoSunlight __instance, Map map, ref SkyTarget? __result) =>
            SkyRestoreUtility.RestoreSky(map, ref __result);
    }

    [HarmonyPatch(typeof(GameCondition_VolcanicWinter), nameof(GameCondition_VolcanicWinter.SkyTarget))]
    public static class Patch_VolcanicWinter_SkyTarget
    {
        public static void Postfix(GameCondition_VolcanicWinter __instance, Map map, ref SkyTarget? __result) =>
            SkyRestoreUtility.RestoreSky(map, ref __result);
    }

    public static class SkyRestoreUtility
    {
        public static void RestoreSky(Map map, ref SkyTarget? result)
        {
            if (result == null || !HazardProtection.SkyProtecting(map))
            {
                return;
            }
            SkyTarget sky = result.Value;
            sky.glow = Mathf.Max(sky.glow, 0.85f);
            result = sky;
        }
    }

    // Odyssey / Biotech hooks applied only when those types exist.
    [StaticConstructorOnStartup]
    public static class Patch_OptionalHazardTypes
    {
        static Patch_OptionalHazardTypes()
        {
            Harmony harmony = new Harmony("azraelgodking.stormproof.optionalhazards");
            TryPatch(harmony, "GameCondition_VolcanicAsh", "GiveOrUpdateHediff",
                typeof(Patch_OptionalHazardTypes), nameof(SkipAshHediffIfBarrier));
            TryPatch(harmony, "GameCondition_VolcanicAsh", "SkyTarget",
                typeof(Patch_OptionalHazardTypes), nameof(RestoreAshSky));
            TryPatchGetter(harmony, "Plant", "GrowthRateFactor_Drought",
                typeof(Patch_OptionalHazardTypes), nameof(DroughtGrowthPostfix));
            TryPatchGetter(harmony, "Plant", "GrowthRateFactor_NoxiousHaze",
                typeof(Patch_OptionalHazardTypes), nameof(NoxiousGrowthPostfix));
        }

        private static void TryPatch(Harmony harmony, string typeName, string methodName,
            Type patchType, string patchMethod)
        {
            Type type = AccessTools.TypeByName(typeName) ?? AccessTools.TypeByName("RimWorld." + typeName);
            MethodInfo target = type != null ? AccessTools.Method(type, methodName) : null;
            MethodInfo patch = AccessTools.Method(patchType, patchMethod);
            if (target == null || patch == null)
            {
                return;
            }
            if (patchMethod.StartsWith("Skip"))
            {
                harmony.Patch(target, prefix: new HarmonyMethod(patch));
            }
            else
            {
                harmony.Patch(target, postfix: new HarmonyMethod(patch));
            }
        }

        private static void TryPatchGetter(Harmony harmony, string typeName, string propertyName,
            Type patchType, string patchMethod)
        {
            Type type = AccessTools.TypeByName(typeName) ?? AccessTools.TypeByName("RimWorld." + typeName);
            PropertyInfo prop = type?.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getter = prop?.GetGetMethod(true);
            MethodInfo patch = AccessTools.Method(patchType, patchMethod);
            if (getter == null || patch == null)
            {
                return;
            }
            harmony.Patch(getter, postfix: new HarmonyMethod(patch));
        }

        public static bool SkipAshHediffIfBarrier(Pawn target)
        {
            return target?.Map == null || !HazardProtection.BarrierProtecting(target.Map);
        }

        public static void RestoreAshSky(Map map, ref SkyTarget? __result) =>
            SkyRestoreUtility.RestoreSky(map, ref __result);

        public static void DroughtGrowthPostfix(Plant __instance, ref float __result)
        {
            if (__result >= 0.999f || __instance?.Map == null)
            {
                return;
            }
            if (HazardProtection.DroughtProtecting(__instance.Map))
            {
                __result = 1f;
            }
        }

        public static void NoxiousGrowthPostfix(Plant __instance, ref float __result)
        {
            if (__result >= 0.999f || __instance?.Map == null)
            {
                return;
            }
            if (HazardProtection.BarrierProtecting(__instance.Map))
            {
                __result = 1f;
            }
        }
    }
}
