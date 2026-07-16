using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Generalized surface entrance for Strata quest sites: clears a patch,
    // spawns the configured stairhead, and optionally rings it with a weathered
    // shell of wall blocks.
    public class GenStep_QuestSiteEntrance : GenStep
    {
        private const int ClearRadius = 5;

        private const int ShellHalfSize = 4;

        public ThingDef stairsDef;

        public ThingDef shellBlocks;

        public float shellFillChance = 0.6f;

        public int rubbleCount = 4;

        public override int SeedPart => 902174864;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (stairsDef == null)
            {
                Log.Error("[Strata] GenStep_QuestSiteEntrance missing stairsDef.");
                return;
            }

            IntVec3 spot = FindEntranceSpot(map, stairsDef);
            ClearArea(map, spot);

            Thing stairs = ThingMaker.MakeThing(stairsDef);
            GenSpawn.Spawn(stairs, spot, map);

            SpawnRuinShell(map, spot, stairsDef);
        }

        private IntVec3 FindEntranceSpot(Map map, ThingDef stairs)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(map.Center, 40f, useCenter: true))
            {
                if (EntranceSpotOk(map, cell, stairs))
                {
                    return cell;
                }
            }
            return map.Center;
        }

        private static bool EntranceSpotOk(Map map, IntVec3 center, ThingDef stairs)
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
                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof != null && !roof.isNatural)
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }
        }

        private void SpawnRuinShell(Map map, IntVec3 center, ThingDef stairs)
        {
            ThingDef blocks = shellBlocks ?? BlocksForMap(map);
            CellRect shell = CellRect.CenteredOn(center, ShellHalfSize);
            CellRect stairsRect = GenAdj.OccupiedRect(center, Rot4.North, stairs.size).ExpandedBy(1);
            foreach (IntVec3 cell in shell.EdgeCells)
            {
                if (!cell.InBounds(map) || stairsRect.Contains(cell) || !Rand.Chance(shellFillChance))
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
            for (int i = 0; i < rubbleCount; i++)
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
