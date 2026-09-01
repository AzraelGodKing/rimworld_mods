using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Seeds the solid rock of a fresh level with hidden features found by
    // mining, like ore: small chambers sealed in the stone. Geothermal
    // chambers hold a steam geyser (the vanilla geothermal generator just
    // works, and its heat feeds the stairwell temperature exchange);
    // gas chambers hold pressurized deep gas, sometimes with a deep vent that
    // keeps seeping forever - a hazard by torchlight, a power source behind a
    // gas well. Chance and count scale with depth.
    public class GenStep_HiddenChambers : GenStep
    {
        private const int EdgeMargin = 12;
        private const float MinDistFromLanding = 24f;
        private const float MinDistBetween = 15f;
        private const int PlacementTries = 60;

        public override int SeedPart => 918273645;

        public override void Generate(Map map, GenStepParams parms)
        {
            int depth = StrataDepth.CountLevelsBelowSurface(map);
            var placed = new List<IntVec3>();

            int geoCount = 0;
            if (depth > 1 && Rand.Chance(Mathf.Min(0.4f + 0.15f * (depth - 1), 0.85f)))
            {
                geoCount = 1 + (Rand.Chance(0.25f * (depth - 1)) ? 1 : 0);
            }
            for (int i = 0; i < geoCount; i++)
            {
                if (TryFindChamberSpot(map, placed, out IntVec3 spot))
                {
                    CarveChamber(map, spot, Rand.Range(3.6f, 4.2f));
                    GenSpawn.Spawn(ThingDefOf.SteamGeyser, spot, map);
                    AtmosphereMapComponent.QueueSeed(map, spot, StrataGasDefOf.Strata_Steam,
                        Rand.Range(0.4f, 0.9f));
                    placed.Add(spot);
                }
            }

            int gasCount = 0;
            if (depth > 1 && Rand.Chance(Mathf.Min(0.5f + 0.15f * (depth - 1), 0.95f)))
            {
                gasCount = Rand.RangeInclusive(1, Mathf.Min(1 + depth, 4));
            }
            for (int i = 0; i < gasCount; i++)
            {
                if (!TryFindChamberSpot(map, placed, out IntVec3 spot))
                {
                    continue;
                }
                CarveChamber(map, spot, Rand.Range(2.4f, 3.4f));
                // Deeper pockets are pressurized well past one room-atmosphere:
                // a breach floods more tunnel than the chamber's own volume.
                float density = Mathf.Min(2.5f + 0.6f * depth + Rand.Value, 6f);
                // Mix foul deep gas with occasional buoyant methane pockets.
                if (Rand.Chance(0.35f))
                {
                    AtmosphereMapComponent.QueueSeed(map, spot, StrataGasDefOf.Strata_Methane,
                        density * Rand.Range(0.45f, 0.85f));
                }
                else
                {
                    AtmosphereMapComponent.QueueSeed(map, spot, StrataGasDefOf.Strata_DeepGas, density);
                    if (Rand.Chance(0.6f))
                    {
                        GenSpawn.Spawn(StrataThingDefOf.Strata_DeepGasVent, spot, map);
                    }
                }
                placed.Add(spot);
            }
        }

        private static bool TryFindChamberSpot(Map map, List<IntVec3> placed, out IntVec3 spot)
        {
            IntVec3 landing = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            for (int i = 0; i < PlacementTries; i++)
            {
                var candidate = new IntVec3(
                    Rand.RangeInclusive(EdgeMargin, map.Size.x - 1 - EdgeMargin), 0,
                    Rand.RangeInclusive(EdgeMargin, map.Size.z - 1 - EdgeMargin));
                if (candidate.DistanceTo(landing) < MinDistFromLanding)
                {
                    continue;
                }
                bool tooClose = false;
                foreach (IntVec3 other in placed)
                {
                    if (candidate.DistanceTo(other) < MinDistBetween)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                {
                    continue;
                }
                spot = candidate;
                return true;
            }
            spot = IntVec3.Invalid;
            return false;
        }

        // Small enough that every carved cell stays within vanilla's thick-roof
        // support distance of the surrounding rock - no collapse on discovery.
        private static void CarveChamber(Map map, IntVec3 center, float radius)
        {
            ChamberCarveUtility.CarveCircle(map, center, radius);
        }
    }

    // Fogs the freshly generated solid-rock level, then unfogs a small arrival
    // chamber. Native warrens stay dark until a colonist walks there. Hidden
    // chambers stay fogged until a miner breaks through (vanilla unfogs on
    // mineable despawn).
    public class GenStep_StrataFog : GenStep
    {
        private const float ArrivalUnfogRadius = 8f;

        public override int SeedPart => 133731882;

        public override void Generate(Map map, GenStepParams parms)
        {
            map.fogGrid.Refog(CellRect.WholeMap(map));
            IntVec3 root = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            var chamber = new HashSet<IntVec3>();
            map.floodFiller.FloodFill(
                root,
                c => c.InBounds(map) && c.Walkable(map) && c.DistanceTo(root) <= ArrivalUnfogRadius,
                c => chamber.Add(c));
            foreach (IntVec3 c in chamber)
            {
                map.fogGrid.Unfog(c);
                foreach (IntVec3 adj in GenAdj.CardinalDirections)
                {
                    IntVec3 n = c + adj;
                    if (n.InBounds(map))
                    {
                        map.fogGrid.Unfog(n);
                    }
                }
            }
        }
    }
}
