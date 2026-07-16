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
            AtmosphereMapComponent atmosphere = map.GetComponent<AtmosphereMapComponent>();
            cell = IntVec3.Invalid;
            room = null;
            if (atmosphere == null)
            {
                return false;
            }
            var candidates = new List<Pair<IntVec3, Room>>();
            var seenRooms = new HashSet<Room>();
            foreach (IntVec3 c in map.AllCells)
            {
                if (c.Fogged(map))
                {
                    continue;
                }
                Room r = c.GetRoom(map);
                if (r == null || r.IsDoorway || r.UsesOutdoorTemperature || !seenRooms.Add(r))
                {
                    continue;
                }
                if (atmosphere.DensityInRoom(r, StrataGasDefOf.Strata_DeepGas) >= MinGasDensity)
                {
                    candidates.Add(new Pair<IntVec3, Room>(c, r));
                }
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
