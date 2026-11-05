using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // A flooded level holds an underground lake. Water seeps out of it onto
    // the level unless a sump pump is running somewhere on the level, faster
    // while it rains hard on the surface. Pawns lying in flood water drown.
    public static class FloodedLevel
    {
        // Picked for B2 and deeper (never the magma layer).
        public const int MinDepth = 2;

        public const float Chance = 0.2f;

        public const int RiseIntervalTicks = 2500;

        public const int DrownIntervalTicks = 250;

        // Cells flooded per rise pulse with no pump running (doubled in heavy rain).
        private const int CellsPerRise = 3;

        // The seep stops spreading past this many flooded cells.
        public const int MaxFloodCells = 500;

        private const float HeavyRainRate = 1f;

        private const float DrownPerPulse = 0.08f;

        private const int EdgeMargin = 16;
        private const float MinDistFromLanding = 22f;

        public static bool Enabled => StrataMod.Settings?.specialLevelsEnabled ?? true;

        internal static void PlaceFeatures(Map map, MapComponent_StrataLevelKind comp)
        {
            IntVec3 landing = MagmaLayer.FindLanding(map);
            if (!TryCarveLake(map, landing, comp))
            {
                return;
            }
            Find.LetterStack.ReceiveLetter(
                "Strata_FloodedLetterTitle".Translate(),
                "Strata_FloodedLetterText".Translate(),
                LetterDefOf.NeutralEvent,
                new LookTargets(landing, map));
        }

        // A winding chain of basins. Each basin stays under the thick-roof
        // support reach, so the lake never brings the ceiling down.
        private static bool TryCarveLake(Map map, IntVec3 landing, MapComponent_StrataLevelKind comp)
        {
            IntVec3 start = IntVec3.Invalid;
            for (int i = 0; i < 80; i++)
            {
                var candidate = new IntVec3(
                    Rand.RangeInclusive(EdgeMargin, map.Size.x - 1 - EdgeMargin), 0,
                    Rand.RangeInclusive(EdgeMargin, map.Size.z - 1 - EdgeMargin));
                if (candidate.DistanceTo(landing) >= MinDistFromLanding)
                {
                    start = candidate;
                    break;
                }
            }
            if (!start.IsValid)
            {
                return false;
            }

            var lake = new HashSet<IntVec3>();
            Vector3 pos = start.ToVector3Shifted();
            float heading = Rand.Range(0f, 360f);
            int basins = Rand.RangeInclusive(4, 6);
            for (int b = 0; b < basins; b++)
            {
                IntVec3 center = pos.ToIntVec3();
                float radius = Rand.Range(4f, 5.5f);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
                {
                    if (cell.InBounds(map) && cell.DistanceTo(landing) >= MinDistFromLanding - 6f
                        && cell.x >= 2 && cell.z >= 2 && cell.x < map.Size.x - 2 && cell.z < map.Size.z - 2)
                    {
                        lake.Add(cell);
                    }
                }
                heading += Rand.Range(-50f, 50f);
                float step = radius * 1.3f;
                pos += new Vector3(Mathf.Cos(heading * Mathf.Deg2Rad) * step, 0f, Mathf.Sin(heading * Mathf.Deg2Rad) * step);
            }
            if (lake.Count == 0)
            {
                return false;
            }

            foreach (IntVec3 cell in lake)
            {
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
            }
            foreach (IntVec3 cell in lake)
            {
                bool shore = false;
                foreach (IntVec3 offset in GenAdj.AdjacentCells)
                {
                    if (!lake.Contains(cell + offset))
                    {
                        shore = true;
                        break;
                    }
                }
                map.terrainGrid.SetTerrain(cell, shore ? TerrainDefOf.WaterShallow : TerrainDefOf.WaterDeep);
                comp.AddLakeCell(cell);
            }
            return true;
        }

        internal static void TickRise(Map map, MapComponent_StrataLevelKind comp)
        {
            FloodMapComponent flood = map.GetComponent<FloodMapComponent>();
            if (flood == null || flood.FloodedCount >= MaxFloodCells || AnyPumpRunning(map))
            {
                return;
            }
            int cells = CellsPerRise * (HeavyRainAbove(map) ? 2 : 1);
            IReadOnlyList<IntVec3> lake = comp.LakeCells;
            if (lake.Count == 0)
            {
                return;
            }
            int placed = 0;
            for (int attempt = 0; attempt < 40 && placed < cells; attempt++)
            {
                IntVec3 from = flood.FloodedCount > 0 && Rand.Bool
                    ? flood.RandomFloodedCell()
                    : lake[Rand.Range(0, lake.Count)];
                if (!from.IsValid)
                {
                    continue;
                }
                IntVec3 next = from + GenAdj.CardinalDirections[Rand.Range(0, 4)];
                if (!next.InBounds(map) || !next.Standable(map) || next.GetTerrain(map).IsWater)
                {
                    continue;
                }
                flood.FloodCell(next);
                placed++;
            }
        }

        internal static void TickDrowning(Map map)
        {
            HediffDef drowning = StrataFloodDefOf.Strata_Drowning;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pawns[i];
                if (pawn.Dead || !(pawn.Downed || pawn.InBed()) || pawn.RaceProps.IsMechanoid
                    || !FloodUtility.IsFlooded(pawn.Position.GetTerrain(map)))
                {
                    continue;
                }
                HealthUtility.AdjustSeverity(pawn, drowning, DrownPerPulse);
            }
        }

        private static bool AnyPumpRunning(Map map)
        {
            List<Thing> pumps = map.listerThings.ThingsOfDef(StrataFloodDefOf.Strata_SumpPump);
            for (int i = 0; i < pumps.Count; i++)
            {
                CompSumpPump pump = pumps[i].TryGetComp<CompSumpPump>();
                if (pump != null && pump.Active)
                {
                    return true;
                }
            }
            return false;
        }

        // Rain on the surface soaks down to the lake (storm mods included).
        private static bool HeavyRainAbove(Map map)
        {
            Map surface = StrataRockUtility.SurfaceMapFor(map);
            WeatherDef weather = surface?.weatherManager?.curWeather;
            return weather != null && weather.rainRate >= HeavyRainRate;
        }

        // The map directly above a flooded level, if any is loaded.
        public static bool HasFloodedLevelBelow(Map map)
        {
            if (map == null)
            {
                return false;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map below = maps[i];
                if (below != map && below.Parent is PocketMapParent pocket && pocket.sourceMap == map
                    && StrataLevelKinds.KindOf(below) == StrataLevelKind.Flooded)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [DefOf]
    public static class StrataFloodDefOf
    {
        public static ThingDef Strata_SumpPump;

        public static HediffDef Strata_Drowning;

        static StrataFloodDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataFloodDefOf));
        }
    }

    // Odyssey fishing: underground water has no fish of its own, so give the
    // underground biome a temperate forest's freshwater fish.
    [StaticConstructorOnStartup]
    public static class StrataUndergroundFish
    {
        static StrataUndergroundFish()
        {
            if (!ModsConfig.OdysseyActive)
            {
                return;
            }
            BiomeDef underground = DefDatabase<BiomeDef>.GetNamedSilentFail(StrataMapUtility.UndergroundBiome);
            BiomeDef source = BiomeDefOf.TemperateForest;
            if (underground == null || source?.fishTypes == null
                || underground.fishTypes?.freshwater_Common?.Count > 0)
            {
                return;
            }
            underground.fishTypes = source.fishTypes;
            underground.maxFishPopulation = source.maxFishPopulation;
        }
    }

    // Homesteader soft compat: a well on the level directly above a flooded
    // level draws from the lake, so each haul brings up twice the water.
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_WellOverFloodedLevel
    {
        private const string DrawWater = "Wellspring_DrawWater";
        private const string DrawWaterDeep = "Wellspring_DrawWaterDeep";

        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> products, RecipeDef recipeDef, IBillGiver billGiver)
        {
            bool boost = SisterModBridges.WellspringLoaded && recipeDef != null
                && (recipeDef.defName == DrawWater || recipeDef.defName == DrawWaterDeep)
                && billGiver is Thing well && FloodedLevel.HasFloodedLevelBelow(well.MapHeld);
            foreach (Thing product in products)
            {
                if (boost)
                {
                    product.stackCount = Mathf.Min(product.stackCount * 2, product.def.stackLimit);
                }
                yield return product;
            }
        }
    }
}
