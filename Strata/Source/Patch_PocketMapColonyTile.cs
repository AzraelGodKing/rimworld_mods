using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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

    // Repair invalid pocket/underground tiles before plant-growth calc runs.
    // Non-destructive (void Prefix): vanilla + Geological Landforms BuildFor still execute.
    // Parent tile is also repaired on Map.FinalizeLoading / FinalizeInit above.
    [HarmonyPatch(typeof(MapPlantGrowthRateCalculator), nameof(MapPlantGrowthRateCalculator.BuildFor), new[] { typeof(Map) })]
    public static class Patch_UndergroundPlantGrowthCalculator
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Map map)
        {
            if (map == null || StrataMapUtility.IsWorldGridTile(map.Tile))
            {
                return;
            }

            if (!PocketMapColonyTileUtility.IsStrataPocketOrUnderground(map))
            {
                return;
            }

            PocketMapColonyTileUtility.TryAssign(map);
        }
    }
}

