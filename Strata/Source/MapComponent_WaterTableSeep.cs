using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Continuous groundwater on levels below the water table. Reuses flood
    // cells; a powered sump holds only the cells inside its radius.
    public class MapComponent_WaterTableSeep : MapComponent
    {
        private const int Interval = 250;
        private const int MaxFlooded = 280;
        private const float MaxFloodFraction = 0.28f;

        private float accumulator;
        private bool warned;

        public MapComponent_WaterTableSeep(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref accumulator, "strataSeepAccum");
            Scribe_Values.Look(ref warned, "strataSeepWarned");
        }

        public override void MapComponentTick()
        {
            if (!map.IsHashIntervalTick(Interval) || !WaterTableUtility.SeepageActive(map))
            {
                return;
            }

            FloodMapComponent flood = map.GetComponent<FloodMapComponent>();
            if (flood == null)
            {
                return;
            }
            if (flood.FloodedCount <= 0)
            {
                warned = false;
            }
            int cap = FloodCap();
            if (flood.FloodedCount >= cap)
            {
                return;
            }

            int below = WaterTableUtility.LevelsBelowTable(map);
            float slider = StrataMod.Settings?.waterTableSeepRate ?? 1f;
            float rain01 = Rainfall01();
            float cellsPerHour = below * Mathf.Lerp(0.45f, 1.35f, rain01) * Mathf.Max(0.15f, slider);
            accumulator += Interval * (cellsPerHour / 2500f);
            int toPlace = Mathf.FloorToInt(accumulator);
            if (toPlace <= 0)
            {
                return;
            }
            accumulator -= toPlace;
            int placed = 0;
            for (int i = 0; i < toPlace && flood.FloodedCount < cap; i++)
            {
                if (!TryFindSeepCell(out IntVec3 cell))
                {
                    break;
                }
                flood.FloodCell(cell);
                placed++;
            }
            if (placed > 0 && !warned)
            {
                warned = true;
                Find.LetterStack.ReceiveLetter(
                    "Strata_WaterTable_LetterLabel".Translate(),
                    "Strata_WaterTable_LetterText".Translate(map.Parent?.LabelCap ?? map.ToString()),
                    LetterDefOf.NegativeEvent,
                    new TargetInfo(map.Center, map));
            }
        }

        private int FloodCap()
        {
            int standable = Mathf.Max(40, map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Count + 80);
            int byArea = Mathf.RoundToInt(map.Area * MaxFloodFraction * 0.04f);
            return Mathf.Clamp(Mathf.Max(standable / 3, byArea), 24, MaxFlooded);
        }

        private float Rainfall01()
        {
            PlanetTile tile = StrataMapUtility.ResolveColonyPlanetTile(map);
            if (!tile.Valid || Find.WorldGrid == null)
            {
                return 0.5f;
            }
            Tile worldTile = Find.WorldGrid[tile];
            return Mathf.Clamp01((worldTile?.rainfall ?? 1000f) / 2400f);
        }

        private bool TryFindSeepCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            for (int i = 0; i < 40; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                if (!candidate.Standable(map) || candidate.Fogged(map))
                {
                    continue;
                }
                if (FloodUtility.IsFlooded(map.terrainGrid.TerrainAt(candidate)))
                {
                    continue;
                }
                if (HasPortal(candidate) || WaterTableUtility.CoveredByPoweredSump(map, candidate))
                {
                    continue;
                }
                cell = candidate;
                return true;
            }
            return false;
        }

        private bool HasPortal(IntVec3 cell)
        {
            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is MapPortal)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
