using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Applies biome flavor to a pre-carved warren using chamber centers stored
    // by GenStep_CarveWarren or GenStep_CarveVault.
    public class GenStep_WarrenTheme : GenStep
    {
        public string theme = "default";

        private const float ChamberRadius = 5f;

        public override int SeedPart => 847291305;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar(GenStep_CarveWarren.ChambersVar, out List<IntVec3> chambers)
                || chambers.NullOrEmpty())
            {
                return;
            }

            switch (theme?.ToLowerInvariant())
            {
                case "fungal":
                    ApplyFungal(map, chambers);
                    break;
                case "flooded":
                    ApplyFlooded(map, chambers);
                    break;
                case "frozen":
                    ApplyFrozen(map, chambers);
                    break;
                case "volcanic":
                    ApplyVolcanic(map, chambers);
                    break;
            }
        }

        private static void ApplyFungal(Map map, List<IntVec3> chambers)
        {
            ThingDef moss = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Moss");
            ThingDef grass = ThingDefOf.Plant_Grass;
            foreach (IntVec3 center in chambers)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ChamberRadius, useCenter: true))
                {
                    if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetPlant(map) != null)
                    {
                        continue;
                    }
                    if (Rand.Chance(0.22f))
                    {
                        ThingDef plant = moss != null && Rand.Chance(0.55f) ? moss : grass;
                        if (plant != null)
                        {
                            Plant plantThing = (Plant)GenSpawn.Spawn(plant, cell, map);
                            plantThing.Growth = Rand.Range(0.35f, 0.95f);
                        }
                    }
                }
            }
            if (chambers.Count > 1 && Rand.Chance(0.35f))
            {
                IntVec3 gasChamber = chambers[Rand.RangeInclusive(1, chambers.Count - 1)];
                AtmosphereMapComponent.QueueSeed(map, gasChamber, StrataGasDefOf.Strata_DeepGas,
                    Rand.Range(0.25f, 0.75f));
            }
            // Fungal warrens always carry a spore haze in deeper chambers.
            for (int i = 1; i < chambers.Count; i++)
            {
                if (Rand.Chance(0.55f))
                {
                    AtmosphereMapComponent.QueueSeed(map, chambers[i], StrataGasDefOf.Strata_Spores,
                        Rand.Range(0.2f, 0.65f));
                }
            }
        }

        private static void ApplyFlooded(Map map, List<IntVec3> chambers)
        {
            TerrainDef shallow = TerrainDef.Named("WaterShallow");
            TerrainDef mud = TerrainDef.Named("Mud");
            // Stale low pits pool black damp after long flooding.
            for (int i = 1; i < chambers.Count; i++)
            {
                if (Rand.Chance(0.5f))
                {
                    AtmosphereMapComponent.QueueSeed(map, chambers[i], StrataGasDefOf.Strata_BlackDamp,
                        Rand.Range(0.25f, 0.7f));
                }
            }
            if (shallow == null && mud == null)
            {
                return;
            }
            foreach (IntVec3 center in chambers)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ChamberRadius, useCenter: true))
                {
                    if (!cell.InBounds(map) || cell.GetFirstMineable(map) != null)
                    {
                        continue;
                    }
                    float dist = cell.DistanceTo(center);
                    if (shallow != null && dist <= ChamberRadius * 0.55f && Rand.Chance(0.65f))
                    {
                        map.terrainGrid.SetTerrain(cell, shallow);
                    }
                    else if (mud != null && dist <= ChamberRadius && Rand.Chance(0.35f))
                    {
                        map.terrainGrid.SetTerrain(cell, mud);
                    }
                }
            }
        }

        private static void ApplyFrozen(Map map, List<IntVec3> chambers)
        {
            TerrainDef ice = DefDatabase<TerrainDef>.GetNamedSilentFail("Ice")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("PackedIce");
            TerrainDef snow = DefDatabase<TerrainDef>.GetNamedSilentFail("SnowDeep")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("SoftSnow");
            if (ice == null && snow == null)
            {
                return;
            }
            foreach (IntVec3 center in chambers)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ChamberRadius, useCenter: true))
                {
                    if (!cell.InBounds(map) || cell.GetFirstMineable(map) != null)
                    {
                        continue;
                    }
                    if (ice != null && Rand.Chance(0.55f))
                    {
                        map.terrainGrid.SetTerrain(cell, ice);
                    }
                    else if (snow != null && Rand.Chance(0.25f))
                    {
                        map.terrainGrid.SetTerrain(cell, snow);
                    }
                }
            }
        }

        private static void ApplyVolcanic(Map map, List<IntVec3> chambers)
        {
            for (int i = 1; i < chambers.Count; i++)
            {
                if (Rand.Chance(0.45f))
                {
                    AtmosphereMapComponent.QueueSeed(map, chambers[i], StrataGasDefOf.Strata_DeepGas,
                        Rand.Range(0.8f, 2.2f));
                }
                if (Rand.Chance(0.4f))
                {
                    AtmosphereMapComponent.QueueSeed(map, chambers[i], StrataGasDefOf.Strata_Steam,
                        Rand.Range(0.3f, 0.9f));
                }
            }
        }
    }
}
