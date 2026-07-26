using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Underground: a room thick with deep gas ignites — by open flame or a
    // spontaneous spark — then fire spreads and the gas burns off.
    public class IncidentWorker_GasFirestorm : IncidentWorker
    {
        private const float MinGasDensity = 0.35f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (StrataMod.Settings?.gasEventsEnabled == false)
            {
                return false;
            }
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && map.GetComponent<AtmosphereMapComponent>() != null
                && TryFindGasRoom(map, out _, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            AtmosphereMapComponent atmosphere = map.GetComponent<AtmosphereMapComponent>();
            if (atmosphere == null || !TryFindGasRoom(map, out IntVec3 cell, out Room room))
            {
                return false;
            }

            Thing flame = AtmosphereMapComponent.FindOpenFlame(room);
            if (flame == null)
            {
                if (!FireUtility.TryStartFireIn(cell, map, 0.55f, null))
                {
                    return false;
                }
                flame = cell.GetFirstThing(map, ThingDefOf.Fire);
            }
            if (flame == null)
            {
                return false;
            }

            float density = atmosphere.DensityInRoom(room, StrataGasDefOf.Strata_DeepGas);
            float radius = Mathf.Clamp(2.2f + density * 3f, 2.8f, 9f);
            GenExplosion.DoExplosion(flame.Position, map, radius, DamageDefOf.Flame, flame);
            int fires = Mathf.Min(Mathf.RoundToInt(density * room.CellCount * 0.04f), 8);
            for (int i = 0; i < fires; i++)
            {
                IntVec3 fireCell = room.Cells.RandomElement();
                if (fireCell.InBounds(map))
                {
                    FireUtility.TryStartFireIn(fireCell, map, Rand.Range(0.25f, 0.7f), flame);
                }
            }
            ClearDeepGas(atmosphere, room);
            SendStandardLetter(parms, new TargetInfo(cell, map));
            return true;
        }

        private static bool TryFindGasRoom(Map map, out IntVec3 cell, out Room room)
        {
            // Replaced AllCells loop (O(map_area)) with AllRooms iteration.
            // Gas is tracked per-room, so we query density directly and only
            // need one non-fogged cell per candidate room for the letter target.
            AtmosphereMapComponent atmosphere = map.GetComponent<AtmosphereMapComponent>();
            cell = IntVec3.Invalid;
            room = null;
            if (atmosphere == null)
            {
                return false;
            }
            var candidates = new List<Pair<IntVec3, Room>>();
            IReadOnlyList<Room> allRooms = map.regionGrid.AllRooms;
            for (int i = 0; i < allRooms.Count; i++)
            {
                Room r = allRooms[i];
                if (r == null || r.IsDoorway || r.UsesOutdoorTemperature)
                {
                    continue;
                }
                if (atmosphere.DensityInRoom(r, StrataGasDefOf.Strata_DeepGas) < MinGasDensity)
                {
                    continue;
                }
                // Pick a non-fogged sample cell for the camera-jump / letter target.
                IntVec3 sample = IntVec3.Invalid;
                Region region = r.FirstRegion;
                if (region != null)
                {
                    IntVec3 candidate = region.AnyCell;
                    if (candidate.IsValid && candidate.InBounds(map) && !candidate.Fogged(map))
                    {
                        sample = candidate;
                    }
                }
                if (!sample.IsValid)
                {
                    continue;
                }
                candidates.Add(new Pair<IntVec3, Room>(sample, r));
            }
            if (candidates.Count == 0)
            {
                return false;
            }
            Pair<IntVec3, Room> pick = candidates.RandomElement();
            cell = pick.First;
            room = pick.Second;
            return true;
        }

        private static void ClearDeepGas(AtmosphereMapComponent atmosphere, Room room)
        {
            foreach (IntVec3 c in room.Cells)
            {
                atmosphere.DebugSetGas(c, StrataGasDefOf.Strata_DeepGas, 0f);
            }
        }
    }
}
