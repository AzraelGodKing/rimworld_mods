using System.Collections.Generic;
using AzraelCommon;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public static class FloodUtility
    {
        private static TerrainDef floodTerrain;

        public static TerrainDef FloodTerrain =>
            floodTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("WaterMovingShallow")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("ShallowWater")
                ?? TerrainDefOf.WaterShallow;

        public static bool IsFlooded(TerrainDef terrain)
        {
            TerrainDef flood = FloodTerrain;
            return terrain != null && flood != null && terrain == flood;
        }

        // A running sump pump keeps the level dry: no seep growth, no storm surge.
        public static bool AnyPumpRunning(Map map)
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

        // Stormproof's storm surge on the surface above (0 to 1): rain, lightning
        // weather and ion storms push groundwater down. 0 without Stormproof.
        public static float StormSurgeAbove(Map map) =>
            StormproofClient.StormSurgeIntensity(StrataRockUtility.SurfaceMapFor(map));
    }

    // Tracks seep-flood cells on underground maps; sump pumps drain them.
    public class FloodMapComponent : MapComponent
    {
        private HashSet<IntVec3> floodedCells = new HashSet<IntVec3>();
        private Dictionary<IntVec3, string> originalTerrain = new Dictionary<IntVec3, string>();

        public FloodMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref floodedCells, "strataFloodedCells", LookMode.Value);
            Scribe_Collections.Look(ref originalTerrain, "strataFloodOriginalTerrain", LookMode.Value, LookMode.Value);
            floodedCells ??= new HashSet<IntVec3>();
            originalTerrain ??= new Dictionary<IntVec3, string>();
        }

        public void FloodCell(IntVec3 cell)
        {
            if (!cell.InBounds(map) || !cell.Standable(map))
            {
                return;
            }
            TerrainDef flood = FloodUtility.FloodTerrain;
            if (flood == null)
            {
                return;
            }
            TerrainDef current = map.terrainGrid.TerrainAt(cell);
            if (current != null && !FloodUtility.IsFlooded(current) && !originalTerrain.ContainsKey(cell))
            {
                originalTerrain[cell] = current.defName;
            }
            map.terrainGrid.SetTerrain(cell, flood);
            floodedCells.Add(cell);
        }

        public void ClearFloodsInRadius(IntVec3 center, float radius)
        {
            List<IntVec3> toClear = null;
            foreach (IntVec3 cell in floodedCells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                if (cell.InHorDistOf(center, radius))
                {
                    toClear ??= new List<IntVec3>();
                    toClear.Add(cell);
                }
            }
            if (toClear == null)
            {
                return;
            }
            for (int i = 0; i < toClear.Count; i++)
            {
                ClearCell(toClear[i]);
            }
        }

        public bool AnyFloodedInRadius(IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in floodedCells)
            {
                if (cell.InBounds(map) && cell.InHorDistOf(center, radius))
                {
                    return true;
                }
            }
            return false;
        }

        private void ClearCell(IntVec3 cell)
        {
            floodedCells.Remove(cell);
            if (!cell.InBounds(map))
            {
                originalTerrain.Remove(cell);
                return;
            }

            if (originalTerrain.TryGetValue(cell, out string defName))
            {
                originalTerrain.Remove(cell);
                TerrainDef orig = DefDatabase<TerrainDef>.GetNamedSilentFail(defName);
                if (orig != null)
                {
                    map.terrainGrid.SetTerrain(cell, orig);
                    return;
                }
            }

            if (FloodUtility.IsFlooded(map.terrainGrid.TerrainAt(cell)))
            {
                map.terrainGrid.RemoveTopLayer(cell);
            }
        }

        public bool AnyFlooded => floodedCells.Count > 0;

        public int FloodedCount => floodedCells.Count;

        public IntVec3 RandomFloodedCell()
        {
            if (floodedCells.Count == 0)
            {
                return IntVec3.Invalid;
            }
            int pick = Rand.Range(0, floodedCells.Count);
            foreach (IntVec3 cell in floodedCells)
            {
                if (pick-- == 0)
                {
                    return cell;
                }
            }
            return IntVec3.Invalid;
        }
    }
}
