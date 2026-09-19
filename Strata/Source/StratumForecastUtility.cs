using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Deterministic pre-gen forecasts for a (tile, depth, cell) so a core sample
    // and the eventual pocket agree within an uncertainty band.
    public static class StratumForecastUtility
    {
        public struct Truth
        {
            public float oreDensity;
            public float gasRisk;
            public float infestationPressure;
            public float rockHardness;
            public bool lostFloorHint;
            public int levelsBelowWaterTable;
        }

        public struct Report
        {
            public string oreLabel;
            public string gasLabel;
            public string infestationLabel;
            public string rockLabel;
            public string waterLabel;
            public string lostFloorLabel;
            public float bandWidth;
        }

        public static int SeedFor(PlanetTile tile, int depth, IntVec3 cell)
        {
            int tileId = StrataMapUtility.IsWorldGridTile(tile) ? tile.tileId : 0;
            unchecked
            {
                int h = tileId;
                h = (h * 397) ^ depth;
                h = (h * 397) ^ cell.x;
                h = (h * 397) ^ cell.z;
                h = (h * 397) ^ 0x57A7A001;
                return h == 0 ? 1 : h;
            }
        }

        public static int SeedForLevel(PlanetTile tile, int depth)
        {
            int tileId = StrataMapUtility.IsWorldGridTile(tile) ? tile.tileId : 0;
            unchecked
            {
                int h = tileId;
                h = (h * 397) ^ depth;
                h = (h * 397) ^ 0x1EFE1001;
                return h == 0 ? 1 : h;
            }
        }

        public static Truth ComputeTruth(PlanetTile tile, int depth, IntVec3 cell)
        {
            depth = Mathf.Max(1, depth);
            int seed = SeedFor(tile, depth, cell);
            Rand.PushState(seed);
            try
            {
                float depthScale = Mathf.Clamp01((depth - 1) / 4f);
                float ore = Mathf.Clamp01(0.25f + depth * 0.12f + Rand.Range(-0.18f, 0.22f));
                float gas = Mathf.Clamp01(0.08f + depthScale * 0.55f + Rand.Range(-0.15f, 0.2f));
                float bugs = Mathf.Clamp01(0.2f + depth * 0.14f + Rand.Range(-0.12f, 0.18f));
                float rock = Mathf.Clamp01(0.3f + depthScale * 0.45f + Rand.Range(-0.1f, 0.15f));
                bool lost = depth >= 3 && Rand.Chance(0.08f + depthScale * 0.1f);

                int table = WorldComponent_StrataStratum.Get?.WaterTableDepth(tile)
                    ?? EstimateWaterTable(tile);
                int below = Mathf.Max(0, depth - table);

                return new Truth
                {
                    oreDensity = ore,
                    gasRisk = gas,
                    infestationPressure = bugs,
                    rockHardness = rock,
                    lostFloorHint = lost,
                    levelsBelowWaterTable = below,
                };
            }
            finally
            {
                Rand.PopState();
            }
        }

        public static Truth ComputeLevelTruth(PlanetTile tile, int depth)
        {
            return ComputeTruth(tile, depth, new IntVec3(0, 0, 0));
        }

        public static float BandWidth(float researchProgress, float buildingQuality)
        {
            float research = Mathf.Clamp01(researchProgress);
            float quality = Mathf.Clamp(buildingQuality, 0.5f, 1.5f);
            return Mathf.Clamp(0.42f - research * 0.28f - (quality - 1f) * 0.08f, 0.08f, 0.45f);
        }

        public static Report FormatReport(Truth truth, float bandWidth)
        {
            return new Report
            {
                oreLabel = BandLabel(truth.oreDensity, bandWidth, "Strata_Sample_OrePoor", "Strata_Sample_OreModerate", "Strata_Sample_OreRich"),
                gasLabel = BandLabel(truth.gasRisk, bandWidth, "Strata_Sample_GasLow", "Strata_Sample_GasSome", "Strata_Sample_GasHigh"),
                infestationLabel = BandLabel(truth.infestationPressure, bandWidth, "Strata_Sample_BugsQuiet", "Strata_Sample_BugsStirring", "Strata_Sample_BugsHeavy"),
                rockLabel = BandLabel(truth.rockHardness, bandWidth, "Strata_Sample_RockSoft", "Strata_Sample_RockMedium", "Strata_Sample_RockHard"),
                waterLabel = truth.levelsBelowWaterTable <= 0
                    ? "Strata_Sample_WaterAbove".Translate().Resolve()
                    : "Strata_Sample_WaterBelow".Translate(truth.levelsBelowWaterTable).Resolve(),
                lostFloorLabel = truth.lostFloorHint
                    ? "Strata_Sample_LostFloorHint".Translate().Resolve()
                    : null,
                bandWidth = bandWidth,
            };
        }

        private static string BandLabel(float truth, float band, string lowKey, string midKey, string highKey)
        {
            float low = Mathf.Clamp01(truth - band);
            float high = Mathf.Clamp01(truth + band);
            if (high < 0.35f)
            {
                return lowKey.Translate().Resolve();
            }
            if (low > 0.65f)
            {
                return highKey.Translate().Resolve();
            }
            if (truth < 0.4f)
            {
                return lowKey.Translate().Resolve();
            }
            if (truth > 0.6f)
            {
                return highKey.Translate().Resolve();
            }
            return midKey.Translate().Resolve();
        }

        public static int EstimateWaterTable(PlanetTile tile)
        {
            if (!StrataMapUtility.IsWorldGridTile(tile) || Find.WorldGrid == null)
            {
                return 2;
            }
            Tile t = Find.WorldGrid[tile];
            if (t == null)
            {
                return 2;
            }
            float rain = t.rainfall;
            int table = 2;
            if (rain > 1500f)
            {
                table = 1;
            }
            else if (rain < 400f)
            {
                table = 3;
            }
            if (t.temperature < -5f)
            {
                table += 1;
            }
            else if (t.temperature > 30f && rain > 900f)
            {
                table = Mathf.Max(1, table - 1);
            }
            return Mathf.Clamp(table, 0, 5);
        }

        public static float ResearchProgress()
        {
            ResearchProjectDef survey = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_CoreSurvey");
            ResearchProjectDef deep = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Strata_Excavation");
            float p = 0f;
            if (survey != null && survey.IsFinished)
            {
                p += 0.55f;
            }
            if (deep != null && deep.IsFinished)
            {
                p += 0.45f;
            }
            return Mathf.Clamp01(p);
        }
    }
}
