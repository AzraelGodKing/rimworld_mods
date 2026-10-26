using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Soft-compat for [JPT] Open The Windows (JPT.OpenTheWindows).
    //
    // Windows decide facing with "one side blocked, the other unroofed".
    // Strata B1+ is mountain-roofed, so both sides fail and the window
    // never faces, never fills WindowCells, and never adds sky glow.
    //
    // During OTW's own scan/ghost only, excavated cells count as open
    // sky. Vanilla Roofed() is unchanged. Underground glow then uses the
    // linked surface sky when the pocket map's own sky is dark.
    public static class StrataOpenTheWindowsSoftCompat
    {
        public const string PackageId = "JPT.OpenTheWindows";

        [ThreadStatic]
        private static int otwRoofQuery;

        private static bool bound;
        private static AccessTools.FieldRef<RoofGrid, Map> roofGridMap;
        private static FieldInfo mapCompsCacheField;
        private static MethodInfo isUnderWindowMethod;
        private static FieldInfo lightTransmissionField;
        private static PropertyInfo lightTransmissionProp;

        public static void TryPatch(Harmony harmony)
        {
            if (harmony == null || bound)
            {
                return;
            }

            bound = true;
            if (!ModsConfig.IsActive(PackageId)
                && ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) == null)
            {
                return;
            }

            Type windowType = AccessTools.TypeByName("OpenTheWindows.Building_Window");
            Type utilityType = AccessTools.TypeByName("OpenTheWindows.WindowUtility");
            Type mapCompType = AccessTools.TypeByName("OpenTheWindows.MapComp_Windows");
            if (windowType == null || utilityType == null)
            {
                Log.Warning("[Strata] Open The Windows is loaded but window types were not found.");
                return;
            }

            try
            {
                roofGridMap = AccessTools.FieldRefAccess<RoofGrid, Map>("map");
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Open The Windows soft-compat: RoofGrid.map bind failed: " + e.Message);
                return;
            }

            mapCompsCacheField = AccessTools.Field(mapCompType, "MapCompsCache");
            isUnderWindowMethod = AccessTools.Method(mapCompType, "IsUnderWindow", new[] { typeof(IntVec3) });
            Type settingsType = AccessTools.TypeByName("OpenTheWindows.OpenTheWindowsSettings")
                ?? AccessTools.TypeByName("OpenTheWindows.Settings");
            if (settingsType != null)
            {
                lightTransmissionProp = AccessTools.Property(settingsType, "LightTransmission");
                lightTransmissionField = AccessTools.Field(settingsType, "LightTransmission");
            }

            int n = 0;
            Type scanLine = AccessTools.Inner(windowType, "ScanLine");
            n += PatchPrefixFinalizer(harmony, scanLine, "Unobstructed",
                nameof(BeginOtwRoofQuery), nameof(EndOtwRoofQuery));
            n += PatchPrefixFinalizer(harmony, utilityType, "UnobstructedGhost",
                nameof(BeginOtwRoofQuery), nameof(EndOtwRoofQuery));
            n += PatchPrefixFinalizer(harmony, utilityType, "GetWindowObfuscation",
                nameof(BeginOtwRoofQuery), nameof(EndOtwRoofQuery));

            MethodInfo roofedCell = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), new[] { typeof(IntVec3) });
            if (roofedCell != null)
            {
                harmony.Patch(roofedCell, prefix: new HarmonyMethod(typeof(StrataOpenTheWindowsSoftCompat), nameof(RoofedCell_Prefix)));
                n++;
            }

            MethodInfo roofedIndex = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), new[] { typeof(int) });
            if (roofedIndex != null)
            {
                harmony.Patch(roofedIndex, prefix: new HarmonyMethod(typeof(StrataOpenTheWindowsSoftCompat), nameof(RoofedIndex_Prefix)));
                n++;
            }

            MethodInfo glow = AccessTools.Method(typeof(GlowGrid), "GroundGlowAt")
                ?? AccessTools.Method(typeof(GlowGrid), "GameGlowAt");
            if (glow != null)
            {
                harmony.Patch(glow, postfix: new HarmonyMethod(typeof(StrataOpenTheWindowsSoftCompat), nameof(GroundGlowAt_Postfix)));
                n++;
            }

            StrataLog.Verbose("[Strata] Open The Windows soft-compat: underground windows use excavated cells as sky ("
                + n + " hooks).");
        }

        public static void BeginOtwRoofQuery()
        {
            otwRoofQuery++;
        }

        public static void EndOtwRoofQuery()
        {
            if (otwRoofQuery > 0)
            {
                otwRoofQuery--;
            }
        }

        public static bool RoofedCell_Prefix(RoofGrid __instance, IntVec3 c, ref bool __result)
        {
            if (otwRoofQuery <= 0 || roofGridMap == null)
            {
                return true;
            }

            Map map = roofGridMap(__instance);
            if (!IsExcavatedOpenSky(map, c))
            {
                return true;
            }

            __result = false;
            return false;
        }

        public static bool RoofedIndex_Prefix(RoofGrid __instance, int index, ref bool __result)
        {
            if (otwRoofQuery <= 0 || roofGridMap == null)
            {
                return true;
            }

            Map map = roofGridMap(__instance);
            if (map == null || index < 0 || index >= map.cellIndices.NumGridCells)
            {
                return true;
            }

            IntVec3 cell = map.cellIndices.IndexToCell(index);
            if (!IsExcavatedOpenSky(map, cell))
            {
                return true;
            }

            __result = false;
            return false;
        }

        public static void GroundGlowAt_Postfix(IntVec3 c, Map ___map, ref float __result)
        {
            if (__result >= 0.05f || otwRoofQuery > 0)
            {
                return;
            }

            Map map = ___map;
            if (map == null || !StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }

            if (!IsUnderWindow(map, c))
            {
                return;
            }

            if (map.skyManager != null && map.skyManager.CurSkyGlow > 0.05f)
            {
                return;
            }

            Map surface = ColonyBedUtility.FindSurfacePlayerHome(map);
            if (surface?.skyManager == null)
            {
                return;
            }

            __result = Mathf.Max(__result, surface.skyManager.CurSkyGlow * LightTransmission());
        }

        private static bool IsExcavatedOpenSky(Map map, IntVec3 cell)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return false;
            }

            if (!cell.InBounds(map) || !cell.CanBeSeenOverFast(map))
            {
                return false;
            }

            Building edifice = cell.GetEdifice(map);
            return edifice?.def?.building?.isNaturalRock != true;
        }

        private static bool IsUnderWindow(Map map, IntVec3 cell)
        {
            if (mapCompsCacheField == null || isUnderWindowMethod == null)
            {
                return false;
            }

            try
            {
                object cache = mapCompsCacheField.GetValue(null);
                if (cache is not IDictionary dict)
                {
                    return false;
                }

                object comp = dict[map.uniqueID];
                if (comp == null)
                {
                    return false;
                }

                return (bool)isUnderWindowMethod.Invoke(comp, new object[] { cell });
            }
            catch
            {
                return false;
            }
        }

        private static float LightTransmission()
        {
            try
            {
                if (lightTransmissionProp != null)
                {
                    return Convert.ToSingle(lightTransmissionProp.GetValue(null, null));
                }

                if (lightTransmissionField != null)
                {
                    return Convert.ToSingle(lightTransmissionField.GetValue(null));
                }
            }
            catch
            {
                // Use the OTW default.
            }

            return 1f;
        }

        private static int PatchPrefixFinalizer(Harmony harmony, Type type, string methodName, string prefix, string finalizer)
        {
            if (type == null)
            {
                return 0;
            }

            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                return 0;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(StrataOpenTheWindowsSoftCompat), prefix),
                finalizer: new HarmonyMethod(typeof(StrataOpenTheWindowsSoftCompat), finalizer));
            return 1;
        }
    }
}
