using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Places steam geysers and pressurized deep gas pockets through a carved
    // warren — the signature hazard of a geothermal vent site.
    public class GenStep_GeothermalVentFeatures : GenStep
    {
        public override int SeedPart => 918273646;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers)
                || chambers.Count < 2)
            {
                return;
            }

            int geyserCount = Rand.RangeInclusive(1, 2);
            var used = new HashSet<IntVec3>();
            for (int i = 0; i < geyserCount; i++)
            {
                IntVec3 chamber = PickChamber(chambers, used);
                if (!chamber.IsValid)
                {
                    break;
                }
                used.Add(chamber);
                if (TryFindStandable(map, chamber, out IntVec3 cell))
                {
                    GenSpawn.Spawn(ThingDefOf.SteamGeyser, cell, map);
                    AtmosphereMapComponent.QueueSeed(map, cell, StrataGasDefOf.Strata_Steam,
                        Rand.Range(0.45f, 1.1f));
                }
            }

            int gasPockets = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < gasPockets; i++)
            {
                IntVec3 chamber = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                if (Rand.Chance(0.4f))
                {
                    AtmosphereMapComponent.QueueSeed(map, chamber, StrataGasDefOf.Strata_Methane,
                        Rand.Range(0.8f, 2.4f));
                }
                else
                {
                    AtmosphereMapComponent.QueueSeed(map, chamber, StrataGasDefOf.Strata_DeepGas,
                        Rand.Range(1.2f, 3.5f));
                }
            }
        }

        private static IntVec3 PickChamber(List<IntVec3> chambers, HashSet<IntVec3> used)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                IntVec3 candidate = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }
            return IntVec3.Invalid;
        }

        private static bool TryFindStandable(Map map, IntVec3 near, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(near, map, 5, c => c.Standable(map), out cell);
        }
    }
}
