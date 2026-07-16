using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // A section of thick rock ceiling gives way over an excavated space,
    // crushing whatever is beneath. Only fires on Strata underground levels and
    // only where there is open floor still capped by heavy rock roof.
    // Shoring pillars protect nearby cells and reduce how often cave-ins occur.
    public class IncidentWorker_CaveIn : IncidentWorker
    {
        private const int MinBlobCells = 3;

        private const int MaxBlobCells = 9;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (parms.target is not Map map || !StrataMapUtility.IsUnderground(map))
            {
                return false;
            }
            ShoringMapComponent shoring = map.GetComponent<ShoringMapComponent>();
            if (shoring != null && shoring.ActivePillarCount > 0 && Rand.Chance(0.35f))
            {
                return false;
            }
            return TryFindCollapseCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindCollapseCell(map, out IntVec3 root))
            {
                return false;
            }

            var blob = new List<IntVec3> { root };
            int target = Rand.RangeInclusive(MinBlobCells, MaxBlobCells);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(root, 2.2f, useCenter: false))
            {
                if (blob.Count >= target)
                {
                    break;
                }
                if (cell.InBounds(map) && IsCollapsible(cell, map))
                {
                    blob.Add(cell);
                }
            }

            RoofCollapserImmediate.DropRoofInCells(blob, map);
            SendStandardLetter(parms, new TargetInfo(root, map));
            return true;
        }

        private static bool TryFindCollapseCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCell(map, c => IsCollapsible(c, map), out cell);
        }

        private static bool IsCollapsible(IntVec3 c, Map map)
        {
            if (!c.Standable(map)
                || map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick
                || c.GetEdifice(map) != null
                || c.Fogged(map))
            {
                return false;
            }
            ShoringMapComponent shoring = map.GetComponent<ShoringMapComponent>();
            return shoring == null || !shoring.CellIsProtected(c);
        }
    }
}
