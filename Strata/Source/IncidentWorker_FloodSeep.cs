using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Groundwater seep floods a patch of the level with shallow moving water.
    public class IncidentWorker_FloodSeep : IncidentWorker
    {
        private const int MinCells = 4;

        private const int MaxCells = 14;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (StrataMod.Settings?.floodEventsEnabled == false)
            {
                return false;
            }
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out IntVec3 root))
            {
                return false;
            }
            FloodMapComponent flood = map.GetComponent<FloodMapComponent>();
            if (flood == null)
            {
                return false;
            }
            int target = Rand.RangeInclusive(MinCells, MaxCells);
            // A Stormproof storm surge above makes an unpumped seep up to twice as big.
            if (!FloodUtility.AnyPumpRunning(map))
            {
                target = Mathf.RoundToInt(target * (1f + FloodUtility.StormSurgeAbove(map)));
            }
            // A bigger blob needs more room than the usual 3.5 cells.
            float radius = target > MaxCells ? 4.5f : 3.5f;
            var blob = new List<IntVec3> { root };
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, radius, useCenter: false))
            {
                if (blob.Count >= target)
                {
                    break;
                }
                if (cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map))
                {
                    blob.Add(cell);
                }
            }
            for (int i = 0; i < blob.Count; i++)
            {
                flood.FloodCell(blob[i]);
            }
            SendStandardLetter(parms, new TargetInfo(root, map));
            return true;
        }
    }
}
