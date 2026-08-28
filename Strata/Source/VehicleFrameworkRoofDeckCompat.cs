using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Soft-compat for SmashPhil Vehicle Framework on A+ roof decks.
    // Strata_RoofDeck already exposes Heavy affordance; OpenSky is Impassable.
    // When VF is loaded, force RoofDeck cells walkable for vehicles and keep
    // OpenSky blocked with a clear failure reason for landing/park checks.
    [StaticConstructorOnStartup]
    public static class VehicleFrameworkRoofDeckCompat
    {
        private const string PackageId = "smashphil.vehicleframework";

        private static bool applied;

        static VehicleFrameworkRoofDeckCompat()
        {
            TryApply();
        }

        private static void TryApply()
        {
            if (applied || !ModsConfig.IsActive(PackageId))
            {
                return;
            }
            applied = true;

            try
            {
                Harmony harmony = new Harmony("azrael.strata.vehicleframework.roofdeck");
                int patched = 0;
                patched += TryPatch(harmony, "Vehicles.VehiclePawn", "CanEnterCell",
                    nameof(CanEnterCell_Postfix));
                patched += TryPatch(harmony, "Vehicles.VehiclePawn", "CanStandAt",
                    nameof(CanStandAt_Postfix));
                patched += TryPatch(harmony, "Vehicles.World.VehiclePathGrid", "CalculatedCostAt",
                    nameof(CalculatedCostAt_Postfix));
                patched += TryPatch(harmony, "Vehicles.Pathing.VehiclePathGrid", "CalculatedCostAt",
                    nameof(CalculatedCostAt_Postfix));
                patched += TryPatch(harmony, "Vehicles.Mapping.VehiclePathGrid", "CalculatedCostAt",
                    nameof(CalculatedCostAt_Postfix));

                if (patched == 0)
                {
                    StrataLog.Verbose("[Strata] Vehicle Framework loaded; RoofDeck Heavy affordance applies — no extra path hooks matched this VF build.");
                }
                else
                {
                    StrataLog.Verbose("[Strata] Vehicle Framework roof-deck soft-compat: patched " + patched + " hook(s).");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Vehicle Framework soft-compat failed: " + e);
            }
        }

        private static int TryPatch(Harmony harmony, string typeName, string methodName, string postfix)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return 0;
            }
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                return 0;
            }
            MethodInfo postfixMethod = AccessTools.Method(typeof(VehicleFrameworkRoofDeckCompat), postfix);
            if (postfixMethod == null)
            {
                return 0;
            }
            harmony.Patch(method, postfix: new HarmonyMethod(postfixMethod));
            return 1;
        }

        // VehiclePawn.CanEnterCell(IntVec3, ...) -> bool
        public static void CanEnterCell_Postfix(IntVec3 cell, ref bool __result, Thing __instance)
        {
            ApplyRoofDeckPassable(__instance?.Map, cell, ref __result);
        }

        public static void CanStandAt_Postfix(IntVec3 cell, ref bool __result, Thing __instance)
        {
            ApplyRoofDeckPassable(__instance?.Map, cell, ref __result);
        }

        // Path cost: RoofDeck = cheap, OpenSky = impassable-high
        public static void CalculatedCostAt_Postfix(IntVec3 cell, ref int __result, object __instance)
        {
            Map map = Traverse.Create(__instance).Field("map").GetValue<Map>()
                ?? Traverse.Create(__instance).Property("Map").GetValue<Map>();
            if (map == null || !cell.InBounds(map) || !StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }
            string defName = cell.GetTerrain(map)?.defName;
            if (defName == UpperDeckUtility.RoofDeckDefName)
            {
                __result = Math.Min(__result, 10);
            }
            else if (defName == UpperDeckUtility.OpenSkyDefName)
            {
                __result = 10000;
            }
        }

        private static void ApplyRoofDeckPassable(Map map, IntVec3 cell, ref bool result)
        {
            if (map == null || !cell.InBounds(map) || !StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }
            string defName = cell.GetTerrain(map)?.defName;
            if (defName == UpperDeckUtility.RoofDeckDefName)
            {
                result = true;
            }
            else if (defName == UpperDeckUtility.OpenSkyDefName)
            {
                result = false;
            }
        }
    }
}
