using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Per-tile water table and cached core samples for the sensing suite.
    public class WorldComponent_StrataStratum : WorldComponent
    {
        private Dictionary<int, int> waterTableByTile = new Dictionary<int, int>();
        private Dictionary<string, SampleRecord> samples = new Dictionary<string, SampleRecord>();
        private int seasonalNudgeTick = -999999;

        public WorldComponent_StrataStratum(World world) : base(world)
        {
        }

        public static WorldComponent_StrataStratum Get =>
            Find.World?.GetComponent<WorldComponent_StrataStratum>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref waterTableByTile, "strataWaterTableByTile", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref samples, "strataCoreSamples", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref seasonalNudgeTick, "strataWaterTableNudgeTick", -999999);
            waterTableByTile ??= new Dictionary<int, int>();
            samples ??= new Dictionary<string, SampleRecord>();
        }

        public override void WorldComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick % 60000 != 0)
            {
                return;
            }
            NudgeTablesFromWeather();
        }

        public int WaterTableDepth(PlanetTile tile)
        {
            if (!StrataMapUtility.IsWorldGridTile(tile))
            {
                return 2;
            }
            int id = tile.tileId;
            if (waterTableByTile.TryGetValue(id, out int depth))
            {
                return depth;
            }
            depth = StratumForecastUtility.EstimateWaterTable(tile);
            waterTableByTile[id] = depth;
            return depth;
        }

        public int LevelsBelowTable(Map map)
        {
            if (map == null)
            {
                return 0;
            }
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            int depth = 0;
            if (StrataMapUtility.IsUnderground(map))
            {
                depth = Mathf.Max(0, StrataDepth.Of(map));
            }
            return Mathf.Max(0, depth - WaterTableDepth(tile));
        }

        public void RememberSample(PlanetTile tile, int depth, IntVec3 cell, StratumForecastUtility.Truth truth, float band)
        {
            if (!StrataMapUtility.IsWorldGridTile(tile))
            {
                return;
            }
            samples[SampleKey(tile.tileId, depth, cell)] = new SampleRecord
            {
                tileId = tile.tileId,
                depth = depth,
                cell = cell,
                ore = truth.oreDensity,
                gas = truth.gasRisk,
                bugs = truth.infestationPressure,
                rock = truth.rockHardness,
                lostHint = truth.lostFloorHint,
                belowTable = truth.levelsBelowWaterTable,
                band = band,
                sampledTick = Find.TickManager.TicksGame,
            };
        }

        public bool TryGetSample(PlanetTile tile, int depth, IntVec3 cell, out SampleRecord record)
        {
            record = null;
            if (!StrataMapUtility.IsWorldGridTile(tile))
            {
                return false;
            }
            return samples.TryGetValue(SampleKey(tile.tileId, depth, cell), out record);
        }

        private static string SampleKey(int tileId, int depth, IntVec3 cell) =>
            tileId + ":" + depth + ":" + cell.x + "," + cell.z;

        private void NudgeTablesFromWeather()
        {
            if (Find.TickManager.TicksGame - seasonalNudgeTick < 60000)
            {
                return;
            }
            seasonalNudgeTick = Find.TickManager.TicksGame;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null)
                {
                    continue;
                }
                PlanetTile planetTile = StrataMapUtility.ResolveColonyPlanetTile(map);
                if (!StrataMapUtility.IsWorldGridTile(planetTile))
                {
                    continue;
                }
                int id = planetTile.tileId;
                int table = WaterTableDepth(planetTile);
                int delta = 0;
                WeatherDef weather = map.weatherManager?.curWeather;
                if (weather != null)
                {
                    string name = weather.defName ?? string.Empty;
                    if (name.IndexOf("Rain", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Storm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        delta = -1;
                    }
                    else if (name.IndexOf("Dry", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Drought", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        delta = 1;
                    }
                }
                if (SisterModBridges.StormproofLoaded && map.GameConditionManager != null)
                {
                    foreach (GameCondition cond in map.GameConditionManager.ActiveConditions)
                    {
                        string cn = cond?.def?.defName ?? string.Empty;
                        if (cn.IndexOf("Drought", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            delta = 1;
                            break;
                        }
                    }
                }
                if (delta != 0)
                {
                    waterTableByTile[id] = Mathf.Clamp(table + delta, 0, 5);
                }
            }
        }

        public class SampleRecord : IExposable
        {
            public int tileId;
            public int depth;
            public IntVec3 cell;
            public float ore;
            public float gas;
            public float bugs;
            public float rock;
            public bool lostHint;
            public int belowTable;
            public float band;
            public int sampledTick;

            public void ExposeData()
            {
                Scribe_Values.Look(ref tileId, "tileId");
                Scribe_Values.Look(ref depth, "depth");
                Scribe_Values.Look(ref cell, "cell");
                Scribe_Values.Look(ref ore, "ore");
                Scribe_Values.Look(ref gas, "gas");
                Scribe_Values.Look(ref bugs, "bugs");
                Scribe_Values.Look(ref rock, "rock");
                Scribe_Values.Look(ref lostHint, "lostHint");
                Scribe_Values.Look(ref belowTable, "belowTable");
                Scribe_Values.Look(ref band, "band");
                Scribe_Values.Look(ref sampledTick, "sampledTick");
            }
        }
    }
}
