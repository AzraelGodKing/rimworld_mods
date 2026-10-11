using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Azrael
{
    /// <summary>
    /// Biases the default and "Random" starting site toward large hills or mountains.
    /// The player can still pick any tile; removing the part restores vanilla picking.
    /// </summary>
    public class ScenPart_PreferMountainStart : ScenPart
    {
        public override string Summary(Scenario scen)
        {
            return "Azrael_ScenPart_PreferMountainSummary".Translate();
        }

        internal static bool ActiveInScenario()
        {
            Scenario scen = Find.Scenario;
            if (scen == null)
            {
                return false;
            }
            foreach (ScenPart part in scen.AllParts)
            {
                if (part is ScenPart_PreferMountainStart)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool TryFindMountainTile(out PlanetTile tile)
        {
            tile = PlanetTile.Invalid;
            PlanetLayer layer = Find.WorldGrid?.Surface;
            if (layer == null)
            {
                return false;
            }

            var candidates = new List<int>();
            for (int i = 0; i < layer.TilesCount; i++)
            {
                Tile t = layer[i];
                if (t == null || (t.hilliness != Hilliness.LargeHills && t.hilliness != Hilliness.Mountainous))
                {
                    continue;
                }
                BiomeDef biome = t.PrimaryBiome;
                if (biome == null || !biome.canBuildBase || !biome.implemented || !biome.canAutoChoose)
                {
                    continue;
                }
                candidates.Add(i);
            }

            for (int attempt = 0; attempt < 60 && candidates.Count > 0; attempt++)
            {
                int pick = Rand.Range(0, candidates.Count);
                PlanetTile candidate = layer[candidates[pick]].tile;
                candidates.RemoveAt(pick);
                if (TileFinder.IsValidTileForNewSettlement(candidate))
                {
                    tile = candidate;
                    return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(TileFinder), nameof(TileFinder.RandomStartingTile))]
    internal static class Patch_TileFinder_RandomStartingTile
    {
        private static void Postfix(ref PlanetTile __result)
        {
            if (!ScenPart_PreferMountainStart.ActiveInScenario())
            {
                return;
            }
            if (ScenPart_PreferMountainStart.TryFindMountainTile(out PlanetTile tile))
            {
                __result = tile;
            }
        }
    }
}
