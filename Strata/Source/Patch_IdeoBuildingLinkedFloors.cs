using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Ideology presence checks only the map you are looking at. An altar on a
    // linked floor then reads as missing (and "desired") the moment you step
    // back onto the surface.
    [HarmonyPatch(typeof(IdeoBuildingPresenceDemand), nameof(IdeoBuildingPresenceDemand.BuildingPresent))]
    public static class Patch_IdeoBuildingPresent_LinkedFloors
    {
        public static void Postfix(IdeoBuildingPresenceDemand __instance, Map map, ref bool __result)
        {
            if (__result || map == null || __instance?.parent == null)
            {
                return;
            }
            __result = IdeoBuildingLinkedFloors.AnyMatching(__instance, map);
        }
    }

    [HarmonyPatch(typeof(IdeoBuildingPresenceDemand), nameof(IdeoBuildingPresenceDemand.RequirementsSatisfied))]
    public static class Patch_IdeoBuildingRequirements_LinkedFloors
    {
        public static void Postfix(IdeoBuildingPresenceDemand __instance, Map map, ref bool __result)
        {
            if (__result || map == null || __instance == null || __instance.roomRequirements.NullOrEmpty())
            {
                return;
            }
            __result = IdeoBuildingLinkedFloors.AnyFullySatisfied(__instance, map);
        }
    }

    [HarmonyPatch(typeof(IdeoBuildingPresenceDemand), nameof(IdeoBuildingPresenceDemand.BestBuilding))]
    public static class Patch_IdeoBuildingBest_LinkedFloors
    {
        public static void Postfix(
            IdeoBuildingPresenceDemand __instance,
            Map map,
            bool ignoreFullyMetRequirements,
            ref Thing __result)
        {
            if (map == null || __instance?.parent == null || !LevelGraph.AnyLinkFrom(map))
            {
                return;
            }

            int bestScore = __result != null
                ? IdeoBuildingLinkedFloors.Score(__instance, __result, ignoreFullyMetRequirements)
                : -1;
            Thing best = __result;
            foreach (Building building in IdeoBuildingLinkedFloors.MatchingOnLinkedMaps(__instance, map))
            {
                int score = IdeoBuildingLinkedFloors.Score(__instance, building, ignoreFullyMetRequirements);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = building;
                }
            }
            __result = best;
        }
    }

    static class IdeoBuildingLinkedFloors
    {
        public static bool AnyMatching(IdeoBuildingPresenceDemand demand, Map map)
        {
            foreach (Building building in MatchingOnLinkedMaps(demand, map))
            {
                if (building.Faction == Faction.OfPlayer)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyFullySatisfied(IdeoBuildingPresenceDemand demand, Map map)
        {
            int needed = demand.roomRequirements.Count;
            foreach (Building building in MatchingOnLinkedMaps(demand, map))
            {
                if (RequirementsMet(demand, building) == needed)
                {
                    return true;
                }
            }
            return false;
        }

        public static int Score(IdeoBuildingPresenceDemand demand, Thing building, bool ignoreFullyMetRequirements)
        {
            int met = RequirementsMet(demand, building);
            if (ignoreFullyMetRequirements
                && demand.roomRequirements != null
                && met == demand.roomRequirements.Count)
            {
                return -1;
            }
            if (demand.GetEffectiveRoom(building) != null)
            {
                met++;
            }
            return met;
        }

        public static IEnumerable<Building> MatchingOnLinkedMaps(IdeoBuildingPresenceDemand demand, Map map)
        {
            if (demand?.parent?.ThingDef == null || !LevelGraph.AnyLinkFrom(map))
            {
                yield break;
            }

            Precept_Building precept = demand.parent;
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(map);
            for (int i = 0; i < links.Count; i++)
            {
                Map other = links[i].map;
                if (other == null)
                {
                    continue;
                }
                List<Thing> things = other.listerThings.ThingsOfDef(precept.ThingDef);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Building building && building.StyleSourcePrecept == precept)
                    {
                        yield return building;
                    }
                }
            }
        }

        private static int RequirementsMet(IdeoBuildingPresenceDemand demand, Thing building)
        {
            if (demand.roomRequirements.NullOrEmpty())
            {
                return 0;
            }
            Room room = demand.GetEffectiveRoom(building);
            if (room == null)
            {
                return 0;
            }
            int met = 0;
            for (int i = 0; i < demand.roomRequirements.Count; i++)
            {
                if (demand.roomRequirements[i].MetOrDisabled(room))
                {
                    met++;
                }
            }
            return met;
        }
    }
}
