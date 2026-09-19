using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Fail-open Stormproof / Odyssey drought inspect on Homesteader wells and cisterns.
    /// String-hook only — no hard dependency on Stormproof.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class StormproofDroughtInspect
    {
        private static readonly HashSet<string> WaterDefNames = new HashSet<string>
        {
            "Wellspring_HandDugWell",
            "Wellspring_DeepWell",
            "Wellspring_RainBarrel",
            "Wellspring_Cistern",
            "Wellspring_WaterTower",
            "Wellspring_SolarStill",
        };

        private static readonly MethodInfo DroughtProtecting;

        static StormproofDroughtInspect()
        {
            Type hazard = AccessTools.TypeByName("Stormproof.HazardProtection");
            DroughtProtecting = hazard != null
                ? AccessTools.Method(hazard, "DroughtProtecting", new[] { typeof(Map) })
                : null;

            try
            {
                new Harmony("azraelgodking.homesteader.stormproof.drought").Patch(
                    AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString)),
                    postfix: new HarmonyMethod(typeof(StormproofDroughtInspect), nameof(InspectPostfix)));
            }
            catch (Exception e)
            {
                Log.Warning("[Homesteader] Stormproof drought inspect soft-compat failed: " + e.Message);
            }
        }

        private static bool MapHasDrought(Map map)
        {
            if (map?.GameConditionManager == null)
            {
                return false;
            }

            GameConditionManager gcm = map.GameConditionManager;
            foreach (GameCondition c in gcm.ActiveConditions)
            {
                string name = c?.def?.defName;
                if (name != null
                    && name.IndexOf("Drought", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CondenserProtecting(Map map)
        {
            if (DroughtProtecting == null || map == null)
            {
                return false;
            }
            try
            {
                return (bool)DroughtProtecting.Invoke(null, new object[] { map });
            }
            catch
            {
                return false;
            }
        }

        public static void InspectPostfix(ThingWithComps __instance, ref string __result)
        {
            if (__instance?.def == null || __instance.Map == null
                || !WaterDefNames.Contains(__instance.def.defName))
            {
                return;
            }

            if (!MapHasDrought(__instance.Map))
            {
                return;
            }

            string line = CondenserProtecting(__instance.Map)
                ? "Homesteader_DroughtInspect_Protected".Translate()
                : "Homesteader_DroughtInspect_Active".Translate();
            if (__result.NullOrEmpty())
            {
                __result = line;
            }
            else
            {
                __result = __result + "\n" + line;
            }
        }
    }
}
