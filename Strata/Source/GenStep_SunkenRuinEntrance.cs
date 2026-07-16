using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Runs on the surface map of a sunken ruin site (linked via the site part):
    // clears a patch near the middle of the map and raises the ancient
    // stairhead inside a broken ring of old stone wall.
    public class GenStep_SunkenRuinEntrance : GenStep
    {
        private const int ClearRadius = 5;

        private const int ShellHalfSize = 4;

        public override int SeedPart => 902174863;

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 spot = FindEntranceSpot(map);
            ClearArea(map, spot);

            Thing stairs = ThingMaker.MakeThing(SunkenRuinDefOf.Strata_RuinStairsDown);
            GenSpawn.Spawn(stairs, spot, map);

            SpawnRuinShell(map, spot);
        }

        private static IntVec3 FindEntranceSpot(Map map)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(map.Center, 40f, useCenter: true))
            {
                if (EntranceSpotOk(map, cell))
                {
                    return cell;
                }
            }
            return map.Center;
        }

        private static bool EntranceSpotOk(Map map, IntVec3 center)
        {
            if (!center.InBounds(map) || center.DistanceToEdge(map) < ShellHalfSize + 6)
            {
                return false;
            }
            foreach (IntVec3 cell in CellRect.CenteredOn(center, ShellHalfSize))
            {
                if (!cell.InBounds(map))
                {
                    return false;
                }
                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain.IsWater || terrain.passability == Traversability.Impassable)
                {
                    return false;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing is Pawn || thing is MapPortal)
                    {
                        return false;
                    }
                    // Natural rock, plants and debris get cleared; anything
                    // else standing here (geysers, generated structures) means
                    // this is somebody else's spot.
                    if (thing.def.IsEdifice() && thing.def.building?.isNaturalRock != true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void ClearArea(Map map, IntVec3 center)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ClearRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (!(thing is Pawn) && thing.def.destroyable)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
                // Old constructed roofs come down; a mountain overhang stays.
                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof != null && !roof.isNatural)
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }
        }

        // A weathered ring of stone-block wall with plenty of gaps, plus a few
        // chunks of rubble, so the stairhead reads as the floor of a collapsed
        // building rather than a bare staircase in a field.
        private static void SpawnRuinShell(Map map, IntVec3 center)
        {
            ThingDef blocks = BlocksForMap(map);
            CellRect shell = CellRect.CenteredOn(center, ShellHalfSize);
            CellRect stairsRect = GenAdj.OccupiedRect(center, Rot4.North, SunkenRuinDefOf.Strata_RuinStairsDown.size).ExpandedBy(1);
            foreach (IntVec3 cell in shell.EdgeCells)
            {
                if (!cell.InBounds(map) || stairsRect.Contains(cell) || !Rand.Chance(0.6f))
                {
                    continue;
                }
                if (cell.GetEdifice(map) == null)
                {
                    Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, blocks);
                    GenSpawn.Spawn(wall, cell, map);
                    wall.HitPoints = Rand.RangeInclusive(wall.MaxHitPoints / 5, wall.MaxHitPoints / 2);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                IntVec3 cell = shell.RandomCell;
                if (cell.InBounds(map) && cell.Standable(map) && !stairsRect.Contains(cell))
                {
                    GenSpawn.Spawn(ThingDefOf.ChunkSlagSteel, cell, map);
                }
            }
        }

        private static ThingDef BlocksForMap(Map map)
        {
            foreach (ThingDef rock in Find.World.NaturalRockTypesIn(map.Tile))
            {
                ThingDef chunk = rock.building?.mineableThing;
                if (chunk?.butcherProducts != null && chunk.butcherProducts.Count > 0)
                {
                    return chunk.butcherProducts[0].thingDef;
                }
            }
            return ThingDefOf.BlocksGranite;
        }
    }
}
