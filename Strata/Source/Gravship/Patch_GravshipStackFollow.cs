using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Detach linked Strata floors early so abandon cannot cascade-destroy them.
    [HarmonyPatch(typeof(WorldComponent_GravshipController), nameof(WorldComponent_GravshipController.InitiateTakeoff))]
    public static class Patch_Gravship_InitiateTakeoff
    {
        public static void Prefix(Building_GravEngine engine)
        {
            if (engine == null)
            {
                return;
            }
            List<Map> levels = StrataGravshipStackUtility.CollectTravellingLevels(engine);
            WorldComponent_StrataGravshipStacks.Get()?.MarkTravelling(levels);
        }
    }

    // Associate the packed Gravship WorldObject with those floors.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.GenerateGravship))]
    public static class Patch_Gravship_GenerateRegisterStack
    {
        public static void Postfix(Building_GravEngine engine, Gravship __result)
        {
            if (__result == null || engine == null)
            {
                return;
            }
            WorldComponent_StrataGravshipStacks.Get()?.RegisterTakeoff(__result, engine);
        }
    }

    // Detach any remaining children when the launch map is abandoned.
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.AbandonMap))]
    public static class Patch_Gravship_AbandonMap
    {
        public static void Prefix(Map map)
        {
            if (map?.ChildPocketMaps == null)
            {
                return;
            }
            foreach (Map child in map.ChildPocketMaps.ToList())
            {
                if (!StrataGravshipStackUtility.IsStrataLinkedLevel(child))
                {
                    continue;
                }
                if (child.Parent is PocketMapParent pocket)
                {
                    pocket.sourceMap = null;
                }
            }
        }
    }

    // Never destroy a map registered as travelling with a gravship.
    [HarmonyPatch(typeof(PocketMapUtility), nameof(PocketMapUtility.DestroyPocketMap))]
    public static class Patch_Gravship_DestroyPocketMapGuard
    {
        public static bool Prefix(Map map)
        {
            var comp = WorldComponent_StrataGravshipStacks.Get();
            if (comp != null && comp.IsTravelling(map))
            {
                Log.Message($"[Strata] Blocked DestroyPocketMap on travelling level {map}.");
                return false;
            }
            return true;
        }
    }

    // Rebind after the ship is placed into a destination map.
    [HarmonyPatch(typeof(GravshipPlacementUtility), nameof(GravshipPlacementUtility.PlaceGravshipInMap))]
    public static class Patch_Gravship_PlaceInMap
    {
        public static void Postfix(Gravship gravship, Map map)
        {
            if (gravship == null || map == null)
            {
                return;
            }
            WorldComponent_StrataGravshipStacks.Get()?.CompleteLanding(gravship, map);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveExistingMap))]
    public static class Patch_Gravship_ArriveExistingMap
    {
        public static void Postfix(Gravship gravship)
        {
            GravshipLandHooks.TryLand(gravship);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveNewMap))]
    public static class Patch_Gravship_ArriveNewMap
    {
        public static void Postfix(Gravship gravship)
        {
            GravshipLandHooks.TryLand(gravship);
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.PostSwapMap))]
    public static class Patch_Gravship_EnginePostSwapMap
    {
        public static void Postfix(Building_GravEngine __instance)
        {
            if (__instance?.Map == null)
            {
                return;
            }
            var comp = WorldComponent_StrataGravshipStacks.Get();
            if (comp == null)
            {
                return;
            }
            Gravship ship = Find.CurrentGravship;
            if (ship != null)
            {
                comp.CompleteLanding(ship, __instance.Map);
            }
            else
            {
                comp.RebindOrphans(__instance.Map);
            }
        }
    }

    internal static class GravshipLandHooks
    {
        public static void TryLand(Gravship gravship)
        {
            if (gravship == null)
            {
                return;
            }
            Map host = FindHostMap();
            if (host == null)
            {
                return;
            }
            WorldComponent_StrataGravshipStacks.Get()?.CompleteLanding(gravship, host);
        }

        private static Map FindHostMap()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
                {
                    continue;
                }
                if (map.listerBuildings.AllBuildingsColonistOfClass<Building_GravEngine>().Any())
                {
                    return map;
                }
            }
            return Find.CurrentMap != null
                && !StrataMapUtility.IsUnderground(Find.CurrentMap)
                && !StrataMapUtility.IsUpperLevel(Find.CurrentMap)
                ? Find.CurrentMap
                : null;
        }
    }
}
