using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Pocket maps inherit an invalid PlanetTile from their MapParent. New maps
    // get a colony tile before generation; existing saves are repaired on load.
    public static class PocketMapColonyTileUtility
    {
        public static bool IsStrataPocketOrUnderground(Map map)
        {
            return map != null && (map.IsPocketMap || map.Parent is PocketMapParent
                || StrataMapUtility.IsUnderground(map));
        }

        public static void TryAssign(Map map)
        {
            if (map == null || StrataMapUtility.IsWorldGridTile(map.Tile)
                || !IsStrataPocketOrUnderground(map))
            {
                return;
            }
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            if (!StrataMapUtility.IsWorldGridTile(tile) || map.Parent == null)
            {
                return;
            }
            map.Parent.Tile = tile;
        }
    }

    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateMap))]
    public static class Patch_PocketMapColonyTile
    {
        public static void Prefix(MapParent parent, bool isPocketMap)
        {
            if (!isPocketMap || StrataMapUtility.IsWorldGridTile(parent.Tile))
            {
                return;
            }
            if (parent is not PocketMapParent pocket)
            {
                return;
            }
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(pocket.sourceMap);
            if (StrataMapUtility.IsWorldGridTile(tile))
            {
                parent.Tile = tile;
            }
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
    public static class Patch_RepairPocketMapTileOnLoad
    {
        public static void Prefix(Map __instance)
        {
            PocketMapColonyTileUtility.TryAssign(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Patch_RepairPocketMapTileOnInit
    {
        public static void Prefix(Map __instance)
        {
            PocketMapColonyTileUtility.TryAssign(__instance);
        }
    }

    // Fallback when parent tile repair failed (broken save refs, load order).
    [HarmonyPatch(typeof(MapPlantGrowthRateCalculator), nameof(MapPlantGrowthRateCalculator.BuildFor), new[] { typeof(Map) })]
    public static class Patch_UndergroundPlantGrowthCalculator
    {
        private static readonly AccessTools.FieldRef<MapPlantGrowthRateCalculator, PlanetTile> TileRef =
            AccessTools.FieldRefAccess<MapPlantGrowthRateCalculator, PlanetTile>("tile");

        private static readonly AccessTools.FieldRef<MapPlantGrowthRateCalculator, Vector2> LongLatRef =
            AccessTools.FieldRefAccess<MapPlantGrowthRateCalculator, Vector2>("longLat");

        private static readonly AccessTools.FieldRef<MapPlantGrowthRateCalculator, BiomeDef> BiomeRef =
            AccessTools.FieldRefAccess<MapPlantGrowthRateCalculator, BiomeDef>("biome");

        private static readonly AccessTools.FieldRef<MapPlantGrowthRateCalculator, bool> DirtyRef =
            AccessTools.FieldRefAccess<MapPlantGrowthRateCalculator, bool>("dirty");

        private static readonly System.Reflection.MethodInfo ComputeIfDirtyMethod =
            AccessTools.Method(typeof(MapPlantGrowthRateCalculator), "ComputeIfDirty");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Map map, MapPlantGrowthRateCalculator __instance)
        {
            if (map == null || StrataMapUtility.IsWorldGridTile(map.Tile))
            {
                return true;
            }

            PocketMapColonyTileUtility.TryAssign(map);
            PlanetTile planetTile = StrataMapUtility.IsWorldGridTile(map.Tile)
                ? map.Tile
                : StrataMapUtility.ResolveColonyPlanetTile(map);
            if (!StrataMapUtility.IsWorldGridTile(planetTile))
            {
                return true;
            }

            TileRef(__instance) = planetTile;
            LongLatRef(__instance) = Find.WorldGrid.LongLatOf(planetTile);
            BiomeRef(__instance) = map.Biome;
            DirtyRef(__instance) = true;
            ComputeIfDirtyMethod.Invoke(__instance, null);
            return false;
        }
    }
}
