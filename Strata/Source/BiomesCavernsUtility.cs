using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Optional bridge to Biomes! Caverns / Core / Framework. No hard dependency:
    // when those mods are absent, Strata keeps its solid-rock fill.
    public static class BiomesCavernsUtility
    {
        private const string PackageCaverns = "BiomesTeam.BiomesCaverns";
        private const string PackageCore = "BiomesTeam.BiomesCore";
        private const string PackageFramework = "BiomesTeam.CoreFramework";

        private static readonly string[] CavernBiomeDefNames =
        {
            "BMT_EarthenDepths",
            "BMT_FungalForest",
            "BMT_CrystalCaverns",
        };

        private static readonly string[] CavernRocksTypeNames =
        {
            "BiomesCore.MapGeneration.GenStep_CavernRocksFromGrid",
            "BiomesCore.GenSteps.GenStep_CavernRocksFromGrid",
        };

        private static Type cavernRocksType;
        private static bool bindLogged;

        public static bool IsActive =>
            ModsConfig.IsActive(PackageCaverns)
            && (ModsConfig.IsActive(PackageCore) || ModsConfig.IsActive(PackageFramework));

        public static bool ShouldGenerateCavernLayout(Map map)
        {
            // When Biomes! Caverns (+ Core/Framework) is loaded, it always owns
            // dig map generation over Strata's native caves.
            if (!IsActive || map == null)
            {
                return false;
            }
            return StrataDepth.CountLevelsBelowSurface(map) >= 1;
        }

        // Optional extras (plants/fauna/stalagmites) — gated by the Mod Options toggle.
        public static bool ShouldScatterCavernFeatures(Map map)
        {
            if (!ShouldGenerateCavernLayout(map)
                || StrataMod.Settings?.biomesCavernsCompatEnabled == false
                || !IsStrataCavernBiome(map.Biome))
            {
                return false;
            }
            return true;
        }

        public static bool IsStrataCavernBiome(BiomeDef biome)
        {
            if (biome?.defName == null)
            {
                return false;
            }
            for (int i = 0; i < CavernBiomeDefNames.Length; i++)
            {
                if (biome.defName == CavernBiomeDefNames[i])
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGenerateCavernLayout(Map map, GenStepParams parms, BiomeDef profile)
        {
            if (map == null || profile == null || !TryGetPocketTile(map, out Tile tile))
            {
                return false;
            }

            BiomeDef previous = tile.PrimaryBiome;
            tile.PrimaryBiome = profile;

            bool regionsWereEnabled = map.regionAndRoomUpdater.Enabled;
            map.regionAndRoomUpdater.Enabled = false;
            try
            {
                new GenStep_ElevationFertility().Generate(map, parms);
                RunCavernRocksFromGrid(map, parms);
                new GenStep_Terrain().Generate(map, parms);
                return true;
            }
            catch (Exception ex)
            {
                tile.PrimaryBiome = previous;
                LogBindOnce("Cavern layout failed: " + ex.Message);
                return false;
            }
            finally
            {
                map.regionAndRoomUpdater.Enabled = regionsWereEnabled;
            }
        }

        public static void ScatterCavernFeatures(Map map, GenStepParams parms)
        {
            if (!ShouldScatterCavernFeatures(map))
            {
                return;
            }

            try
            {
                new GenStep_Plants().Generate(map, parms);
                new GenStep_Animals().Generate(map, parms);
                RunDefGenStep("BMT_ScatterStalagmiteGenerator", map, parms);
                RunDefGenStep("BMT_CrystalsGenerator", map, parms);
            }
            catch (Exception ex)
            {
                LogBindOnce("Cavern feature scatter failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Pick a Biomes! Caverns profile the way their world workers do, mapped onto
        /// Strata floors. [Biomes! Caverns](https://steamcommunity.com/sharedfiles/filedetails/?id=2969748433)
        /// has three full cavern biomes plus a Shallow Cave overlay (not a BiomeDef):
        /// fungal forest wants wet rock, crystalline caverns want cold, earthen depths
        /// are magma near the mantle (their workers start around cave-system depth 2–3).
        /// B1–B2 used to force earthen depths, so most colonies never saw the others.
        /// </summary>
        public static BiomeDef PickProfileBiome(Map map)
        {
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            if (depth < 1)
            {
                depth = 1;
            }

            TryReadSurfaceClimate(map, out float temp, out float rain);
            float wet = Mathf.InverseLerp(400f, 1400f, rain);
            float cold = Mathf.InverseLerp(12f, -8f, temp);
            float hot = Mathf.InverseLerp(18f, 45f, temp);

            float fungal;
            float crystal;
            float earthen;
            if (depth <= 1)
            {
                // Near-surface stand-in for Shallow Cave: climate first, magma rare.
                fungal = 0.40f + 0.45f * wet;
                crystal = 0.20f + 0.45f * cold;
                earthen = 0.08f + 0.22f * hot;
            }
            else if (depth == 2)
            {
                fungal = 0.30f + 0.35f * wet;
                crystal = 0.22f + 0.35f * cold;
                earthen = 0.20f + 0.30f * hot;
            }
            else if (depth == 3)
            {
                fungal = 0.22f + 0.28f * wet;
                crystal = 0.22f + 0.28f * cold;
                earthen = 0.38f + 0.35f * hot;
            }
            else
            {
                fungal = 0.12f + 0.22f * wet;
                crystal = 0.18f + 0.25f * cold;
                earthen = 0.50f + 0.40f * hot;
            }

            string defName = RollCavernProfile(fungal, crystal, earthen);
            BiomeDef def = DefDatabase<BiomeDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                return def;
            }

            for (int i = 0; i < CavernBiomeDefNames.Length; i++)
            {
                def = DefDatabase<BiomeDef>.GetNamedSilentFail(CavernBiomeDefNames[i]);
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        private static string RollCavernProfile(float fungal, float crystal, float earthen)
        {
            float total = fungal + crystal + earthen;
            if (total <= 0f)
            {
                return "BMT_FungalForest";
            }

            float roll = Rand.Value * total;
            if (roll < fungal)
            {
                return "BMT_FungalForest";
            }

            if (roll < fungal + crystal)
            {
                return "BMT_CrystalCaverns";
            }

            return "BMT_EarthenDepths";
        }

        private static void TryReadSurfaceClimate(Map map, out float temperature, out float rainfall)
        {
            temperature = 20f;
            rainfall = 1000f;
            PlanetTile planetTile = StrataMapUtility.ResolveColonyPlanetTile(map);
            if (!StrataMapUtility.IsWorldGridTile(planetTile) || Find.WorldGrid == null)
            {
                return;
            }

            Tile tile = Find.WorldGrid[planetTile];
            if (tile == null)
            {
                return;
            }

            temperature = tile.temperature;
            rainfall = tile.rainfall;
        }

        public static void LogLayoutChoice(Map map, bool usedBiomes, BiomeDef profile)
        {
            string profileName = profile?.defName ?? "(none)";
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            if (usedBiomes)
            {
                StrataLog.Verbose("[Strata] Dig cavern layout: Biomes! Caverns profile " + profileName
                    + " (depth " + depth + ").");
            }
            else
            {
                StrataLog.Verbose("[Strata] Dig cavern layout: native mixed-rock warren"
                    + (profile != null ? " (Biomes! profile " + profileName + " failed)" : "")
                    + ".");
            }
        }

        /// <summary>
        /// Biomes elevation/rocks on a mountain (or landform) PlanetTile can leave
        /// a contour-shaped hollow with almost no mineable rock — dig-under-mountain
        /// then looks empty. Real cavern layouts are still mostly rock.
        /// Also catches maps that pass a global rock % but leave the mountain
        /// silhouette (thick roof / natural rock upstairs) empty.
        /// </summary>
        public static bool CavernLayoutTooHollow(Map map)
        {
            if (map == null)
            {
                return true;
            }

            const float minMineableFraction = 0.28f;
            int stride = map.Size.x >= 200 ? 2 : 1;

            if (TrySampleMineableFraction(map, stride, mountainAlignedOnly: false,
                    out int cells, out float fraction)
                && (cells <= 0 || fraction < minMineableFraction))
            {
                StrataLog.Verbose("[Strata] Biomes! dig layout too sparse ("
                    + (fraction * 100f).ToString("F1") + "% mineable sampled) — "
                    + "falling back to Strata rock fill (common under mountains / landforms).");
                return true;
            }

            // Edge rock can push the global sample over the threshold while the
            // mountain footprint the player dug under stays empty.
            if (TrySampleMineableFraction(map, stride, mountainAlignedOnly: true,
                    out int mountainCells, out float mountainFraction)
                && mountainCells >= 40
                && mountainFraction < minMineableFraction)
            {
                StrataLog.Verbose("[Strata] Biomes! dig layout empty under mountain footprint ("
                    + (mountainFraction * 100f).ToString("F1") + "% mineable in "
                    + mountainCells + " mountain-aligned samples) — "
                    + "falling back to Strata rock fill.");
                return true;
            }

            return false;
        }

        private static bool TrySampleMineableFraction(
            Map map,
            int stride,
            bool mountainAlignedOnly,
            out int cells,
            out float fraction)
        {
            cells = 0;
            fraction = 0f;
            Map source = mountainAlignedOnly ? SourceMapForDig(map) : null;
            if (mountainAlignedOnly && source == null)
            {
                return false;
            }

            int mineables = 0;
            for (int z = 0; z < map.Size.z; z += stride)
            {
                for (int x = 0; x < map.Size.x; x += stride)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (mountainAlignedOnly)
                    {
                        IntVec3 below = source.Size == map.Size
                            ? cell
                            : StrataMapUtility.ProportionalCell(cell, map, source);
                        if (!below.InBounds(source) || !StrataRockUtility.CellIsMountainMass(source, below))
                        {
                            continue;
                        }
                    }

                    cells++;
                    if (cell.GetFirstMineable(map) != null)
                    {
                        mineables++;
                    }
                }
            }

            if (cells > 0)
            {
                fraction = (float)mineables / cells;
            }
            return true;
        }

        internal static Map SourceMapForDig(Map dig)
        {
            if (PocketMapUtility.currentlyGeneratingPortal?.Map != null)
            {
                return PocketMapUtility.currentlyGeneratingPortal.Map;
            }
            if (dig?.Parent is PocketMapParent pocket)
            {
                return pocket.sourceMap;
            }
            return null;
        }

        private static void RunCavernRocksFromGrid(Map map, GenStepParams parms)
        {
            if (!TryBindCavernRocks(out GenStep step))
            {
                new GenStep_RocksFromGrid().Generate(map, parms);
                return;
            }
            step.Generate(map, parms);
        }

        private static bool TryBindCavernRocks(out GenStep step)
        {
            step = null;
            if (cavernRocksType == null)
            {
                cavernRocksType = FindCavernRocksType();
                if (cavernRocksType == null)
                {
                    LogBindOnce("GenStep_CavernRocksFromGrid type not found.");
                    return false;
                }
            }
            try
            {
                step = (GenStep)Activator.CreateInstance(cavernRocksType);
                return true;
            }
            catch (Exception ex)
            {
                LogBindOnce("Could not create GenStep_CavernRocksFromGrid: " + ex.Message);
                return false;
            }
        }

        private static Type FindCavernRocksType()
        {
            for (int i = 0; i < CavernRocksTypeNames.Length; i++)
            {
                Type type = AccessTools.TypeByName(CavernRocksTypeNames[i]);
                if (type != null)
                {
                    return type;
                }
            }

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name == null || (!name.Contains("Biomes") && !name.Contains("BMT")))
                {
                    continue;
                }
                try
                {
                    Type[] types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i].Name == "GenStep_CavernRocksFromGrid")
                        {
                            return types[i];
                        }
                    }
                }
                catch
                {
                    // ReflectionTypeLoadException on some assemblies — skip.
                }
            }

            GenStepDef def = DefDatabase<GenStepDef>.GetNamedSilentFail("BMT_CavernRocksFromGrid");
            return def?.genStep?.GetType();
        }

        private static void RunDefGenStep(string defName, Map map, GenStepParams parms)
        {
            GenStepDef def = DefDatabase<GenStepDef>.GetNamedSilentFail(defName);
            def?.genStep?.Generate(map, parms);
        }

        private static bool TryGetPocketTile(Map map, out Tile tile)
        {
            tile = null;
            if (map == null || !map.IsPocketMap || map.pocketTileInfo == null)
            {
                LogBindOnce("Pocket-map tile info unavailable — cavern layout biome swap disabled.");
                return false;
            }
            tile = map.pocketTileInfo;
            return true;
        }

        private static void LogBindOnce(string message)
        {
            if (bindLogged)
            {
                return;
            }
            bindLogged = true;
            Log.Warning("[Strata] Biomes! Caverns compat: " + message);
        }
    }
}
