using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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

        public static BiomeDef PickProfileBiome(Map map)
        {
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            string defName;
            if (depth <= 2)
            {
                defName = "BMT_EarthenDepths";
            }
            else if (depth == 3)
            {
                defName = Rand.Value < 0.55f ? "BMT_FungalForest" : "BMT_EarthenDepths";
            }
            else if (depth == 4)
            {
                float roll = Rand.Value;
                defName = roll < 0.35f ? "BMT_CrystalCaverns"
                    : roll < 0.7f ? "BMT_FungalForest"
                    : "BMT_EarthenDepths";
            }
            else
            {
                float roll = Rand.Value;
                defName = roll < 0.4f ? "BMT_CrystalCaverns"
                    : roll < 0.75f ? "BMT_FungalForest"
                    : "BMT_EarthenDepths";
            }

            return DefDatabase<BiomeDef>.GetNamedSilentFail(defName);
        }

        public static void LogLayoutChoice(Map map, bool usedBiomes, BiomeDef profile)
        {
            string profileName = profile?.defName ?? "(none)";
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            if (usedBiomes)
            {
                Log.Message("[Strata] Dig cavern layout: Biomes! Caverns profile " + profileName
                    + " (depth " + depth + ").");
            }
            else
            {
                Log.Message("[Strata] Dig cavern layout: native mixed-rock warren"
                    + (profile != null ? " (Biomes! profile " + profileName + " failed)" : "")
                    + ".");
            }
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
