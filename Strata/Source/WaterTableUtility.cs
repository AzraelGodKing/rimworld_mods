using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Per-tile water table depth (B-level). Levels above stay dry; levels
    // below seep into the existing flood/sump system.
    public class WorldComponent_StrataWaterTable : WorldComponent
    {
        public Dictionary<int, int> baseDepthByTile = new Dictionary<int, int>();

        public WorldComponent_StrataWaterTable(World world) : base(world)
        {
        }

        public static WorldComponent_StrataWaterTable Get =>
            Find.World?.GetComponent<WorldComponent_StrataWaterTable>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref baseDepthByTile, "strataWaterTableBase", LookMode.Value, LookMode.Value);
            baseDepthByTile ??= new Dictionary<int, int>();
        }
    }

    public static class WaterTableUtility
    {
        public static bool Enabled =>
            StrataMod.Settings != null && StrataMod.Settings.waterTableEnabled;

        public static bool SeepageActive(Map map)
        {
            return Enabled
                && map != null
                && StrataMapUtility.IsUnderground(map)
                && LevelsBelowTable(map) > 0;
        }

        public static int BaseTableDepth(PlanetTile tile)
        {
            if (!tile.Valid)
            {
                return 2;
            }
            return ComputeBase(tile);
        }

        public static int EffectiveTableDepth(PlanetTile tile, Map surfaceHint = null)
        {
            int depth = BaseTableDepth(tile);
            depth += SeasonalShift(tile, surfaceHint);
            return Mathf.Clamp(depth, 1, 6);
        }

        public static int EffectiveTableDepth(Map map)
        {
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            Map surface = ColonyBedUtility.FindSurfacePlayerHome(map);
            return EffectiveTableDepth(tile, surface);
        }

        public static int LevelsBelowTable(Map map)
        {
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return 0;
            }
            int depth = StrataDepth.Of(map);
            if (depth <= 1)
            {
                // B1 is the shallow starter level — never a seep target.
                return 0;
            }
            return Mathf.Max(0, depth - EffectiveTableDepth(map));
        }

        public static string InspectRelative(Map map)
        {
            if (!Enabled || map == null || !StrataMapUtility.IsUnderground(map))
            {
                return null;
            }
            int depth = StrataDepth.Of(map);
            if (depth <= 1)
            {
                return "Strata_WaterTable_Shallow".Translate();
            }
            int table = EffectiveTableDepth(map);
            int delta = depth - table;
            if (delta > 0)
            {
                return "Strata_WaterTable_Below".Translate(delta);
            }
            if (delta < 0)
            {
                return "Strata_WaterTable_Above".Translate(-delta);
            }
            return "Strata_WaterTable_At".Translate();
        }

        public static string CompactLabel(Map map)
        {
            if (!Enabled || map == null || !StrataMapUtility.IsUnderground(map))
            {
                return null;
            }
            int depth = StrataDepth.Of(map);
            if (depth <= 1)
            {
                return "Strata_WaterTable_CompactShallow".Translate();
            }
            int table = EffectiveTableDepth(map);
            int delta = depth - table;
            if (delta > 0)
            {
                return "Strata_WaterTable_CompactBelow".Translate(delta);
            }
            if (delta < 0)
            {
                return "Strata_WaterTable_CompactAbove".Translate(-delta);
            }
            return "Strata_WaterTable_CompactAt".Translate();
        }

        public static string AppendInspect(string text, Map map)
        {
            string line = InspectRelative(map);
            if (line.NullOrEmpty())
            {
                return text;
            }
            return text.NullOrEmpty() ? line : text + "\n" + line;
        }

        public static bool CoveredByPoweredSump(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return false;
            }
            List<Thing> pumps = map.listerThings?.ThingsOfDef(StrataThingDefOf.Strata_SumpPump);
            if (pumps == null || pumps.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < pumps.Count; i++)
            {
                Thing pump = pumps[i];
                CompSumpPump sump = pump.TryGetComp<CompSumpPump>();
                if (sump == null || !sump.Active)
                {
                    continue;
                }
                if (cell.InHorDistOf(pump.Position, sump.Props.clearRadius))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AnyUncoveredFlood(Map map)
        {
            FloodMapComponent flood = map?.GetComponent<FloodMapComponent>();
            return flood != null && flood.AnyFloodedCell(c => !CoveredByPoweredSump(map, c));
        }

        private static int ComputeBase(PlanetTile tile)
        {
            Tile worldTile = Find.WorldGrid?[tile];
            float rainfall = worldTile?.rainfall ?? 1000f;
            float rain01 = Mathf.Clamp01(rainfall / 2400f);
            int depth = 1 + Mathf.RoundToInt((1f - rain01) * 2f);
            BiomeDef biome = worldTile?.PrimaryBiome;
            string name = biome?.defName ?? string.Empty;
            if (name.Contains("Swamp") || name.Contains("Marsh") || name.Contains("TropicalRainforest"))
            {
                depth -= 1;
            }
            else if (name.Contains("Desert") || name.Contains("Arid") || name.Contains("Badlands"))
            {
                depth += 1;
            }
            return Mathf.Clamp(depth, 1, 6);
        }

        private static int SeasonalShift(PlanetTile tile, Map surfaceHint)
        {
            Map surface = surfaceHint;
            if (surface == null || !StrataMapUtility.IsWorldGridTile(surface.Tile) || surface.Tile != tile)
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map candidate = maps[i];
                    if (StrataMapUtility.IsSurfacePlayerHome(candidate)
                        && StrataMapUtility.IsWorldGridTile(candidate.Tile)
                        && candidate.Tile == tile)
                    {
                        surface = candidate;
                        break;
                    }
                }
            }
            if (surface == null)
            {
                return 0;
            }
            int shift = 0;
            if (surface.weatherManager != null && surface.weatherManager.RainRate > 0.45f)
            {
                shift -= 1;
            }
            GameConditionManager conditions = surface.gameConditionManager;
            if (conditions != null)
            {
                if (ConditionNamed(conditions, "Drought") || ConditionNamed(conditions, "DroughtInitial"))
                {
                    shift += 1;
                }
                if (ConditionNamed(conditions, "Flashstorm") || ConditionNamed(conditions, "VolcanicWinter"))
                {
                    shift -= 1;
                }
            }
            return Mathf.Clamp(shift, -1, 1);
        }

        private static bool ConditionNamed(GameConditionManager manager, string defName)
        {
            GameConditionDef def = DefDatabase<GameConditionDef>.GetNamedSilentFail(defName);
            return def != null && manager.ConditionIsActive(def);
        }
    }
}
