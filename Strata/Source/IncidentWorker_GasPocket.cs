using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Excavation splits a seam and breaches a pocket of foul deep gas: a burst
    // of the persistent atmosphere gas floods a dug-out room from a rock face.
    // It is the same gas the hidden chambers hold - toxic to breathe,
    // explosive around open flames, and cleared by the same ventilation tools
    // as smoke. Venting keeps pawns safe but wastes the resource.
    public class IncidentWorker_GasPocket : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (StrataMod.Settings?.gasEventsEnabled == false)
            {
                return false;
            }
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && map.GetComponent<AtmosphereMapComponent>() != null
                && TryFindBreach(map, out _, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            AtmosphereMapComponent atmosphere = map.GetComponent<AtmosphereMapComponent>();
            if (atmosphere == null || !TryFindBreach(map, out IntVec3 breach, out IntVec3 inside))
            {
                return false;
            }

            Room room = inside.GetRoom(map);
            if (room == null)
            {
                return false;
            }
            // Deeper levels split richer seams. A burst ignores the ventilated
            // cap - it is pressure, not a steady inflow - but drains through
            // outlets over the following cycles like any other gas.
            float density = Mathf.Min(0.45f + 0.1f * StrataDepth.Of(map) + Rand.Range(0f, 0.25f), 1.2f);
            atmosphere.AddGasBurst(room, StrataGasDefOf.Strata_DeepGas, density, inside);

            for (int i = 0; i < 6; i++)
            {
                FleckMaker.ThrowDustPuffThick(inside.ToVector3Shifted(), map, Rand.Range(1.5f, 3f),
                    new Color(0.5f, 0.7f, 0.4f, 0.8f));
            }

            SendStandardLetter(parms, new TargetInfo(inside, map));
            return true;
        }

        // A natural rock face with a walkable, discovered room cell beside it:
        // where digging plausibly split a seam.
        private static bool TryFindBreach(Map map, out IntVec3 breach, out IntVec3 inside)
        {
            IntVec3 foundBreach = IntVec3.Invalid;
            IntVec3 foundInside = IntVec3.Invalid;
            bool Valid(IntVec3 cell)
            {
                Mineable rock = cell.GetFirstMineable(map);
                if (rock == null || rock.def.building == null || !rock.def.building.isNaturalRock)
                {
                    return false;
                }
                foreach (IntVec3 dir in GenAdj.CardinalDirections)
                {
                    IntVec3 neighbor = cell + dir;
                    if (!neighbor.InBounds(map) || neighbor.Fogged(map) || !neighbor.Standable(map))
                    {
                        continue;
                    }
                    Room room = neighbor.GetRoom(map);
                    if (room == null || room.UsesOutdoorTemperature || room.IsDoorway)
                    {
                        continue;
                    }
                    foundBreach = cell;
                    foundInside = neighbor;
                    return true;
                }
                return false;
            }

            bool ok = CellFinder.TryFindRandomCell(map, Valid, out _);
            breach = foundBreach;
            inside = foundInside;
            return ok;
        }
    }
}
