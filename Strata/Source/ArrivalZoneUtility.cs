using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Keeps stairwell landings walkable after Biomes! cavern terrain gen. Biomes
    // can paint lava/magma pools through the vertically aligned arrival cell;
    // this clears mineables and replaces hazardous terrain in a disc around it.
    public static class ArrivalZoneUtility
    {
        // Thick-roof support limit for mineable carve (see GenStep_SolidRock).
        public const float ChamberRadius = 6.5f;

        // Wider disc for lava/magma/water — keeps Deep Lava from hugging the shaft.
        public const float HazardTerrainRadius = 14f;

        public static void PrepareLandingZone(Map map, IntVec3 spot, bool clearRoof)
        {
            CarveChamber(map, spot, ChamberRadius, clearRoof);
            SanitizeHazardousTerrain(map, spot, HazardTerrainRadius);
        }

        public static void PrepareLandingCell(Map map, IntVec3 spot)
        {
            CarveChamber(map, spot, 4.5f, clearRoof: false);
            SanitizeHazardousTerrain(map, spot, HazardTerrainRadius);
        }

        private static void CarveChamber(Map map, IntVec3 spot, float radius, bool clearRoof)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(spot, radius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                cell.GetFirstMineable(map)?.Destroy(DestroyMode.Vanish);
                if (clearRoof)
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }
        }

        public static void SanitizeHazardousTerrain(Map map, IntVec3 spot, float radius)
        {
            TerrainDef safeFloor = SafeFloorForMap(map);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(spot, radius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                TerrainDef terrain = cell.GetTerrain(map);
                if (!IsHazardousTerrain(terrain))
                {
                    continue;
                }
                map.terrainGrid.SetTerrain(cell, safeFloor);
            }
        }

        internal static bool IsHazardousTerrain(TerrainDef terrain)
        {
            if (terrain == null)
            {
                return false;
            }
            if (terrain.tags != null)
            {
                for (int i = 0; i < terrain.tags.Count; i++)
                {
                    if (terrain.tags[i] == "Lava")
                    {
                        return true;
                    }
                }
            }
            if (terrain.IsWater)
            {
                return true;
            }
            if (terrain.passability == Traversability.Impassable
                && terrain.defName != null
                && (terrain.defName.Contains("Lava") || terrain.defName == "BMT_Magma"))
            {
                return true;
            }
            return false;
        }

        private static TerrainDef SafeFloorForMap(Map map)
        {
            ThingDef rock = RockForMap(map);
            return rock.building?.naturalTerrain ?? TerrainDefOf.Gravel;
        }

        private static ThingDef RockForMap(Map map)
        {
            Map surface = map;
            int guard = 0;
            while (surface.Parent is PocketMapParent pocket && pocket.sourceMap != null && guard++ < 32)
            {
                surface = pocket.sourceMap;
            }

            List<ThingDef> rocks = Find.World.NaturalRockTypesIn(surface.Tile).ToList();
            if (!rocks.NullOrEmpty())
            {
                return rocks.RandomElement();
            }
            return ThingDefOf.Granite;
        }
    }
}
