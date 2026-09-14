using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Deterministic stratum under a cell so a core sample and the eventual
    // generated level agree. Chunked so three nearby holes can disagree.
    public struct StratumTruth
    {
        public float ore;
        public float gas;
        public float infestation;
        public float hardness;
        public bool lostHint;
    }

    public static class StratumSurvey
    {
        public const int ChunkSize = 16;

        public static int TargetDepthBelow(Map map)
        {
            if (map == null)
            {
                return 1;
            }
            if (StrataMapUtility.IsUnderground(map))
            {
                return StrataDepth.Of(map) + 1;
            }
            return 1;
        }

        public static StratumTruth Truth(PlanetTile tile, int depth, IntVec3 cell)
        {
            int seed = Seed(tile, depth, ChunkOf(cell.x), ChunkOf(cell.z));
            Rand.PushState(seed);
            try
            {
                float depth01 = Mathf.Clamp01((depth - 1) / 5f);
                var truth = new StratumTruth
                {
                    ore = Mathf.Clamp01(0.28f + 0.12f * depth + Rand.Range(-0.22f, 0.28f)),
                    gas = Mathf.Clamp01(0.08f + 0.14f * depth + Rand.Range(-0.18f, 0.32f)),
                    infestation = Mathf.Clamp01(0.18f + 0.12f * depth + Rand.Range(-0.2f, 0.3f)),
                    hardness = Mathf.Clamp01(0.35f + 0.08f * depth + Rand.Range(-0.25f, 0.3f)),
                    lostHint = depth >= 4 && Rand.Chance(0.08f + 0.03f * depth01),
                };
                return truth;
            }
            finally
            {
                Rand.PopState();
            }
        }

        public static StratumTruth TruthForSample(Map map, IntVec3 cell)
        {
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            return Truth(tile, TargetDepthBelow(map), cell);
        }

        public static float Precision()
        {
            float band = 0.34f;
            if (IsFinished("Strata_Excavation"))
            {
                band = 0.22f;
            }
            if (IsFinished("Strata_DeepInfrastructure"))
            {
                band = 0.12f;
            }
            return band;
        }

        public static string BandLabel(float value, float band, string lowKey, string midKey, string highKey)
        {
            float lo = Mathf.Clamp01(value - band);
            float hi = Mathf.Clamp01(value + band);
            if (hi < 0.38f)
            {
                return lowKey.Translate();
            }
            if (lo > 0.62f)
            {
                return highKey.Translate();
            }
            if (band > 0.28f)
            {
                return midKey.Translate();
            }
            if (value < 0.45f)
            {
                return lowKey.Translate();
            }
            if (value > 0.55f)
            {
                return highKey.Translate();
            }
            return midKey.Translate();
        }

        public static float ObservedOre(Map map, IntVec3 cell, float radius)
        {
            if (map == null)
            {
                return -1f;
            }
            int host = 0;
            int ore = 0;
            foreach (IntVec3 c in GenRadial.RadialCellsAround(cell, radius, useCenter: true))
            {
                if (!c.InBounds(map))
                {
                    continue;
                }
                Mineable mineable = c.GetFirstMineable(map);
                if (mineable == null)
                {
                    continue;
                }
                host++;
                if (mineable.def.building != null && mineable.def.building.isResourceRock)
                {
                    ore++;
                }
            }
            if (host <= 0)
            {
                return 0f;
            }
            return Mathf.Clamp01(ore / (float)Mathf.Max(8, host / 4));
        }

        internal static int ChunkOf(int v) => v < 0 ? (v - ChunkSize + 1) / ChunkSize : v / ChunkSize;

        internal static int Seed(PlanetTile tile, int depth, int chunkX, int chunkZ)
        {
            int worldSeed = Find.World?.info?.Seed ?? 0;
            int s = Gen.HashCombineInt(worldSeed, tile.tileId);
            s = Gen.HashCombineInt(s, depth);
            s = Gen.HashCombineInt(s, chunkX);
            return Gen.HashCombineInt(s, chunkZ);
        }

        private static bool IsFinished(string defName)
        {
            ResearchProjectDef def = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            return def != null && def.IsFinished;
        }
    }
}
