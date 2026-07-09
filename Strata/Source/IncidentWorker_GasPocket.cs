using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Excavation breaches a pocket of foul underground gas, flooding a stretch
    // of the level with toxic vapour. A reason to keep a route to fresh air.
    public class IncidentWorker_GasPocket : IncidentWorker
    {
        private const float Radius = 4.5f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
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

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, Radius, useCenter: true))
            {
                if (!cell.InBounds(map) || cell.Filled(map))
                {
                    continue;
                }
                float falloff = 1f - root.DistanceTo(cell) / (Radius + 1f);
                int amount = Mathf.RoundToInt(Mathf.Clamp01(falloff) * GasGrid.MaxGasPerCell);
                if (amount > 0)
                {
                    map.gasGrid.AddGas(cell, GasType.ToxGas, amount);
                }
            }

            SendStandardLetter(parms, new TargetInfo(root, map));
            return true;
        }
    }
}
