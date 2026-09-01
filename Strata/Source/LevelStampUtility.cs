using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Copy a finished floor's structure onto another linked level as blueprints.
    public static class LevelStampUtility
    {
        public static string LabelFor(Map map)
        {
            if (map == null)
            {
                return "Level";
            }
            string custom = StrataLevelLabels.Get?.GetLabel(map);
            if (!custom.NullOrEmpty())
            {
                return custom;
            }
            int altitude = StrataDepth.Altitude(map);
            if (altitude == 0)
            {
                string name = map.Parent?.LabelCap;
                return name.NullOrEmpty() ? "Strata_LevelSurface".Translate().ToString() : name;
            }
            if (altitude > 0)
            {
                return "Strata_LevelAbove".Translate(altitude);
            }
            return "Strata_LevelBelow".Translate(altitude);
        }

        public static List<Map> LinkedMaps(Map source)
        {
            var maps = new List<Map>();
            if (source == null)
            {
                return maps;
            }
            maps.Add(source);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(source))
            {
                if (link.map != null && !maps.Contains(link.map))
                {
                    maps.Add(link.map);
                }
            }
            return maps;
        }

        public static IntVec3 Transform(IntVec3 cell, Rot4 rot, IntVec3 offset, IntVec3 srcSize)
        {
            int x = cell.x;
            int z = cell.z;
            int w = srcSize.x;
            int h = srcSize.z;
            if (rot == Rot4.East)
            {
                int nx = z;
                z = w - 1 - x;
                x = nx;
            }
            else if (rot == Rot4.South)
            {
                x = w - 1 - x;
                z = h - 1 - z;
            }
            else if (rot == Rot4.West)
            {
                int nx = h - 1 - z;
                z = x;
                x = nx;
            }
            return new IntVec3(x + offset.x, 0, z + offset.z);
        }

        public static Rot4 RotateBuilding(Rot4 buildingRot, Rot4 stampRot)
        {
            return new Rot4((buildingRot.AsInt + stampRot.AsInt) & 3);
        }

        public static bool IsStampableStructure(Building building)
        {
            if (building == null || building.def == null)
            {
                return false;
            }
            ThingDef def = building.def;
            if (building is MapPortal
                || typeof(MapPortal).IsAssignableFrom(def.thingClass)
                || def.IsFrame
                || def.IsBlueprint
                || def.mineable
                || (def.building != null && def.building.isNaturalRock))
            {
                return false;
            }
            if (def.designationCategory != null && def.designationCategory.defName == "Structure")
            {
                return true;
            }
            return building.TryGetComp<CompShoring>() != null;
        }

        public static bool IsStampableFloor(TerrainDef terrain)
        {
            return terrain != null
                && terrain.layerable
                && terrain.BuildableByPlayer
                && !terrain.natural;
        }

        public static StampResult Stamp(
            Map source,
            Map dest,
            Rot4 rot,
            IntVec3 offset,
            bool includeFloors,
            bool includeStockpiles)
        {
            var result = new StampResult();
            if (source == null || dest == null || source == dest)
            {
                return result;
            }

            IntVec3 srcSize = source.Size;
            var seen = new HashSet<Building>();
            foreach (IntVec3 srcCell in source.AllCells)
            {
                IntVec3 destCell = Transform(srcCell, rot, offset, srcSize);
                if (!destCell.InBounds(dest))
                {
                    result.skipped++;
                    continue;
                }

                Building edifice = srcCell.GetEdifice(source);
                if (edifice != null && seen.Add(edifice) && IsStampableStructure(edifice))
                {
                    IntVec3 destPos = Transform(edifice.Position, rot, offset, srcSize);
                    Rot4 destRot = edifice.def.rotatable
                        ? RotateBuilding(edifice.Rotation, rot)
                        : Rot4.North;
                    if (!TryPlaceBlueprint(edifice.def, destPos, dest, destRot, edifice.Stuff, result))
                    {
                        result.skipped++;
                    }
                }

                if (includeFloors)
                {
                    TerrainDef terrain = source.terrainGrid.TerrainAt(srcCell);
                    if (IsStampableFloor(terrain))
                    {
                        if (!TryPlaceBlueprint(terrain, destCell, dest, Rot4.North, null, result))
                        {
                            result.skipped++;
                        }
                    }
                }
            }

            if (includeStockpiles)
            {
                StampStockpiles(source, dest, rot, offset, srcSize, result);
            }

            return result;
        }

        private static bool TryPlaceBlueprint(
            BuildableDef def,
            IntVec3 cell,
            Map dest,
            Rot4 rot,
            ThingDef stuff,
            StampResult result)
        {
            if (def == null || !cell.InBounds(dest))
            {
                return false;
            }
            AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(def, cell, rot, dest);
            if (!report.Accepted)
            {
                return false;
            }
            GenConstruct.PlaceBlueprintForBuild(
                def, cell, dest, rot, Faction.OfPlayer, stuff, null, null, true);
            result.placed++;
            return true;
        }

        private static void StampStockpiles(
            Map source,
            Map dest,
            Rot4 rot,
            IntVec3 offset,
            IntVec3 srcSize,
            StampResult result)
        {
            List<Zone> zones = source.zoneManager.AllZones;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is not Zone_Stockpile srcZone)
                {
                    continue;
                }
                var destZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, dest.zoneManager);
                destZone.label = srcZone.label;
                if (srcZone.settings != null && destZone.settings != null)
                {
                    destZone.settings.Priority = srcZone.settings.Priority;
                    destZone.settings.filter.CopyAllowancesFrom(srcZone.settings.filter);
                }
                int added = 0;
                foreach (IntVec3 srcCell in srcZone.Cells)
                {
                    IntVec3 destCell = Transform(srcCell, rot, offset, srcSize);
                    if (!destCell.InBounds(dest) || destCell.GetZone(dest) != null)
                    {
                        continue;
                    }
                    destZone.AddCell(destCell);
                    added++;
                }
                if (added == 0)
                {
                    dest.zoneManager.DeregisterZone(destZone);
                    result.skipped++;
                }
                else
                {
                    result.zones++;
                }
            }
        }

        public struct StampResult
        {
            public int placed;
            public int skipped;
            public int zones;
        }
    }
}
