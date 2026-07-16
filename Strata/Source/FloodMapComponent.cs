using System.Collections.Generic;
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
    }

    // Tracks seep-flood cells on underground maps; sump pumps drain them.
    public class FloodMapComponent : MapComponent
    {
        private HashSet<IntVec3> floodedCells = new HashSet<IntVec3>();

        public FloodMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref floodedCells, "strataFloodedCells", LookMode.Value);
            floodedCells ??= new HashSet<IntVec3>();
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

        private void ClearCell(IntVec3 cell)
        {
            floodedCells.Remove(cell);
            if (!cell.InBounds(map))
            {
                return;
            }
            if (FloodUtility.IsFlooded(map.terrainGrid.TerrainAt(cell)))
            {
                map.terrainGrid.RemoveTopLayer(cell);
            }
        }

        public bool AnyFlooded => floodedCells.Count > 0;
    }
}
