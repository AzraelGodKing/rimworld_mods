using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    /// <summary>
    /// Vanilla Low Food / Low Medicine only read one map's ResourceCounter.
    /// Stockpiles on linked underground / upper floors were invisible, so the
    /// alert fired whenever the surface pantry was empty.
    /// </summary>
    public static class StrataAlertResources
    {
        private static readonly List<Map> tmpNetwork = new List<Map>();

        public static bool ShouldUseNetwork(Map map) =>
            map != null && LevelGraph.AnyLinkFrom(map);

        public static void CollectNetwork(Map root, List<Map> into)
        {
            into.Clear();
            ColonyBedUtility.GetColonyNetworkMaps(root, into);
            if (into.Count == 0 && root != null)
            {
                into.Add(root);
            }
        }

        public static float NetworkHumanEdibleNutrition(Map root)
        {
            CollectNetwork(root, tmpNetwork);
            float total = 0f;
            for (int i = 0; i < tmpNetwork.Count; i++)
            {
                Map map = tmpNetwork[i];
                if (map?.resourceCounter == null)
                {
                    continue;
                }
                total += map.resourceCounter.TotalHumanEdibleNutrition;
            }
            return total;
        }

        public static int NetworkMedicineCount(Map root)
        {
            CollectNetwork(root, tmpNetwork);
            int total = 0;
            for (int i = 0; i < tmpNetwork.Count; i++)
            {
                Map map = tmpNetwork[i];
                if (map?.resourceCounter == null)
                {
                    continue;
                }
                total += map.resourceCounter.GetCountIn(ThingRequestGroup.Medicine);
            }
            return total;
        }

        public static int NetworkFreeColonists(Map root)
        {
            CollectNetwork(root, tmpNetwork);
            int total = 0;
            for (int i = 0; i < tmpNetwork.Count; i++)
            {
                Map map = tmpNetwork[i];
                if (map?.mapPawns == null)
                {
                    continue;
                }
                total += map.mapPawns.FreeColonistsSpawnedCount;
            }
            return total;
        }

        public static int NetworkColonistsAndPrisoners(Map root)
        {
            CollectNetwork(root, tmpNetwork);
            int total = 0;
            for (int i = 0; i < tmpNetwork.Count; i++)
            {
                Map map = tmpNetwork[i];
                if (map?.mapPawns == null)
                {
                    continue;
                }
                total += map.mapPawns.FreeColonistsSpawnedCount
                    + map.mapPawns.PrisonersOfColonyCount;
            }
            return total;
        }

        public static bool NetworkHasAnyColonist(Map root)
        {
            CollectNetwork(root, tmpNetwork);
            for (int i = 0; i < tmpNetwork.Count; i++)
            {
                if (tmpNetwork[i]?.mapPawns?.AnyColonistSpawned == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Prefer evaluating once from the surface home so underground IsPlayerHome
        /// maps do not double-fire.
        /// </summary>
        public static bool IsNetworkAlertRoot(Map map)
        {
            if (map == null || !map.IsPlayerHome)
            {
                return false;
            }
            if (!LevelGraph.AnyLinkFrom(map))
            {
                return true;
            }
            Map surface = ColonyBedUtility.FindSurfacePlayerHome(map);
            return surface == null || surface == map;
        }
    }

    [HarmonyPatch(typeof(Alert_LowFood), "MapWithLowFood")]
    public static class Patch_Alert_LowFood_Network
    {
        private const float NutritionThresholdPerColonist = 4f;

        public static bool Prefix(ref Map __result)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                if (!StrataAlertResources.ShouldUseNetwork(map))
                {
                    if (!map.mapPawns.AnyColonistSpawned)
                    {
                        continue;
                    }
                    int colonists = map.mapPawns.FreeColonistsSpawnedCount;
                    if (map.resourceCounter.TotalHumanEdibleNutrition
                        < NutritionThresholdPerColonist * colonists)
                    {
                        __result = map;
                        return false;
                    }
                    continue;
                }

                if (!StrataAlertResources.IsNetworkAlertRoot(map))
                {
                    continue;
                }
                if (!StrataAlertResources.NetworkHasAnyColonist(map))
                {
                    continue;
                }

                int networkColonists = StrataAlertResources.NetworkFreeColonists(map);
                if (networkColonists <= 0)
                {
                    continue;
                }
                float nutrition = StrataAlertResources.NetworkHumanEdibleNutrition(map);
                if (nutrition < NutritionThresholdPerColonist * networkColonists)
                {
                    __result = map;
                    return false;
                }
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Alert_LowFood), nameof(Alert_LowFood.GetExplanation))]
    public static class Patch_Alert_LowFood_Explanation
    {
        public static bool Prefix(ref TaggedString __result)
        {
            // Re-run the same finder the report uses (now network-aware).
            Map map = null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map candidate = maps[i];
                if (!candidate.IsPlayerHome)
                {
                    continue;
                }
                if (StrataAlertResources.ShouldUseNetwork(candidate))
                {
                    if (!StrataAlertResources.IsNetworkAlertRoot(candidate))
                    {
                        continue;
                    }
                    if (!StrataAlertResources.NetworkHasAnyColonist(candidate))
                    {
                        continue;
                    }
                    int networkColonists = StrataAlertResources.NetworkFreeColonists(candidate);
                    if (networkColonists > 0
                        && StrataAlertResources.NetworkHumanEdibleNutrition(candidate)
                            < 4f * networkColonists)
                    {
                        map = candidate;
                        break;
                    }
                }
                else if (candidate.mapPawns.AnyColonistSpawned
                    && candidate.resourceCounter.TotalHumanEdibleNutrition
                        < 4f * candidate.mapPawns.FreeColonistsSpawnedCount)
                {
                    map = candidate;
                    break;
                }
            }

            if (map == null)
            {
                __result = string.Empty;
                return false;
            }

            float nutrition;
            int eaters;
            if (StrataAlertResources.ShouldUseNetwork(map))
            {
                nutrition = StrataAlertResources.NetworkHumanEdibleNutrition(map);
                eaters = StrataAlertResources.NetworkColonistsAndPrisoners(map);
            }
            else
            {
                nutrition = map.resourceCounter.TotalHumanEdibleNutrition;
                eaters = map.mapPawns.FreeColonistsSpawnedCount
                    + map.mapPawns.PrisonersOfColonyCount;
            }

            int days = eaters > 0 ? Mathf.FloorToInt(nutrition / eaters) : 0;
            __result = "LowFoodDesc".Translate(
                nutrition.ToString("F0"),
                eaters.ToStringCached(),
                days.ToStringCached());
            return false;
        }
    }

    [HarmonyPatch(typeof(Alert_LowMedicine), "MapWithLowMedicine")]
    public static class Patch_Alert_LowMedicine_Network
    {
        private const float MedicinePerColonistThreshold = 2f;

        private static readonly AccessTools.FieldRef<Alert_LowMedicine, Map> mapWithNoMedicine =
            AccessTools.FieldRefAccess<Alert_LowMedicine, Map>("mapWithNoMedicine");

        private static readonly AccessTools.FieldRef<Alert_LowMedicine, int> medicineCount =
            AccessTools.FieldRefAccess<Alert_LowMedicine, int>("medicineCount");

        public static bool Prefix(Alert_LowMedicine __instance, ref Map __result)
        {
            mapWithNoMedicine(__instance) = null;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                if (!StrataAlertResources.ShouldUseNetwork(map))
                {
                    if (!map.mapPawns.AnyColonistSpawned)
                    {
                        continue;
                    }
                    int med = map.resourceCounter.GetCountIn(ThingRequestGroup.Medicine);
                    medicineCount(__instance) = med;
                    if (med < MedicinePerColonistThreshold * map.mapPawns.FreeColonistsSpawnedCount)
                    {
                        mapWithNoMedicine(__instance) = map;
                        __result = map;
                        return false;
                    }
                    continue;
                }

                if (!StrataAlertResources.IsNetworkAlertRoot(map))
                {
                    continue;
                }
                if (!StrataAlertResources.NetworkHasAnyColonist(map))
                {
                    continue;
                }

                int networkColonists = StrataAlertResources.NetworkFreeColonists(map);
                if (networkColonists <= 0)
                {
                    continue;
                }
                int networkMed = StrataAlertResources.NetworkMedicineCount(map);
                medicineCount(__instance) = networkMed;
                if (networkMed < MedicinePerColonistThreshold * networkColonists)
                {
                    mapWithNoMedicine(__instance) = map;
                    __result = map;
                    return false;
                }
            }

            __result = null;
            return false;
        }
    }
}
