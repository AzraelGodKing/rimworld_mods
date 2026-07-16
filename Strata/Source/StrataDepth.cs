using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // How far below the surface a level sits: 0 = surface, 1 = first excavated
    // level, 2 = the level below that, and so on. Derived from the pocket-map
    // parent chain, so it needs nothing saved. Upper floors use StrataAltitude.
    public static class StrataDepth
    {
        public static int Of(Map map)
        {
            if (!StrataMapUtility.IsUnderground(map))
            {
                return 0;
            }
            return CountLevelsBelowSurface(map);
        }

        // B1 and any underground pocket without a parent chain (depth 0).
        public static bool IsStarterLevel(Map map)
        {
            return StrataMapUtility.IsUnderground(map) && Of(map) <= 1;
        }

        // Same parent-chain walk as Of, but safe during map generation before
        // the biome is fully wired (GenSteps use this for B1 vs B2+ rules).
        public static int CountLevelsBelowSurface(Map map)
        {
            int depth = 0;
            Map current = map;
            int guard = 0;
            while (current?.Parent is PocketMapParent parent && parent.sourceMap != null && guard++ < 64)
            {
                depth++;
                current = parent.sourceMap;
            }
            return depth;
        }

        // Signed stack index: +N upper floors, 0 surface, -N underground.
        public static int Altitude(Map map)
        {
            if (map == null)
            {
                return 0;
            }
            if (StrataMapUtility.IsUpperLevel(map))
            {
                return CountLevelsAboveSurface(map);
            }
            if (StrataMapUtility.IsUnderground(map))
            {
                return -CountLevelsBelowSurface(map);
            }
            return 0;
        }

        public static int CountLevelsAboveSurface(Map map)
        {
            if (!StrataMapUtility.IsUpperLevel(map))
            {
                return 0;
            }
            int height = 0;
            Map current = map;
            int guard = 0;
            while (current?.Parent is PocketMapParent parent && parent.sourceMap != null && guard++ < 64)
            {
                height++;
                current = parent.sourceMap;
                if (!StrataMapUtility.IsUpperLevel(current))
                {
                    break;
                }
            }
            return height;
        }

        // A geothermal gradient: the deeper you dig, the warmer the rock.
        public static float GeothermalOutdoorTemp(int depth)
        {
            return Mathf.Min(8f + depth * 3.5f, 45f);
        }
    }

    // Underground levels sit at a constant temperature that rises with depth,
    // so the surface "outdoor" temperature is meaningless down there - replace
    // it with the geothermal baseline. Feeds the stairwell heat exchange too.
    [HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.OutdoorTemp), MethodType.Getter)]
    public static class Patch_GeothermalOutdoorTemp
    {
        private static readonly AccessTools.FieldRef<MapTemperature, Map> MapRef =
            AccessTools.FieldRefAccess<MapTemperature, Map>("map");

        public static void Postfix(MapTemperature __instance, ref float __result)
        {
            Map map = MapRef(__instance);
            if (map != null && StrataMapUtility.IsUnderground(map))
            {
                __result = StrataDepth.GeothermalOutdoorTemp(StrataDepth.Of(map));
            }
        }
    }
}
