using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Mining breaks through into a side cavern: fungus, gas, or abandoned loot.
    public class IncidentWorker_CaveBreakthrough : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsUnderground(map)
                && map.mapPawns.FreeColonistsSpawnedCount > 0
                && TryFindRockFace(map, out _, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindRockFace(map, out IntVec3 breach, out IntVec3 inside))
            {
                return false;
            }
            IntVec3 center = breach + (inside - breach);
            ChamberCarveUtility.CarveCircle(map, center, Rand.Range(2.6f, 3.8f));
            SeedChamber(map, center);
            for (int i = 0; i < 5; i++)
            {
                FleckMaker.ThrowDustPuffThick(inside.ToVector3Shifted(), map, Rand.Range(1.2f, 2.4f),
                    new Color(0.45f, 0.38f, 0.32f, 0.85f));
            }
            SendStandardLetter(parms, new TargetInfo(inside, map));
            return true;
        }

        private static void SeedChamber(Map map, IntVec3 center)
        {
            float roll = Rand.Value;
            if (roll < 0.34f)
            {
                if (Faction.OfInsects != null && Rand.Chance(0.65f))
                {
                    GenSpawn.Spawn(ThingDefOf.Hive, center, map);
                }
                else
                {
                    ThingDef filth = ThingDefOf.Filth_Slime;
                    if (filth != null)
                    {
                        FilthMaker.TryMakeFilth(center, map, filth);
                    }
                }
                return;
            }
            if (roll < 0.67f && StrataMod.Settings?.gasEventsEnabled != false)
            {
                float density = Mathf.Min(1.2f + 0.25f * StrataDepth.Of(map), 3f);
                AtmosphereMapComponent.QueueSeed(map, center, StrataGasDefOf.Strata_DeepGas, density);
                if (Rand.Chance(0.35f))
                {
                    GenSpawn.Spawn(StrataThingDefOf.Strata_DeepGasVent, center, map);
                }
                return;
            }
            ScatterLoot(map, center);
        }

        private static void ScatterLoot(Map map, IntVec3 center)
        {
            ThingDef loot = Rand.Value < 0.5f ? ThingDefOf.Silver : ThingDefOf.Steel;
            int stacks = Rand.RangeInclusive(2, 5);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2.5f, useCenter: true))
            {
                if (stacks-- <= 0)
                {
                    break;
                }
                if (cell.InBounds(map) && cell.Standable(map))
                {
                    GenSpawn.Spawn(loot, cell, map, WipeMode.Vanish).stackCount = Rand.RangeInclusive(15, 40);
                }
            }
        }

        private static bool TryFindRockFace(Map map, out IntVec3 breach, out IntVec3 inside)
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
