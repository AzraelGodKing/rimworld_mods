using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // How far below the surface a level sits: 0 = surface, 1 = first excavated
    // level, 2 = the level below that, and so on. Derived from the pocket-map
    // parent chain, so it needs nothing saved.
    public static class StrataDepth
    {
        public static int Of(Map map)
        {
            if (!StrataMapUtility.IsUnderground(map))
            {
                return 0;
            }
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
