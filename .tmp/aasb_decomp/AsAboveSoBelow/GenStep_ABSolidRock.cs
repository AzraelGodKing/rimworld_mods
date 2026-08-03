using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AsAboveSoBelow;

public class GenStep_ABSolidRock : GenStep
{
	public override int SeedPart => 762195841;

	public override void Generate(Map map, GenStepParams parms)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		List<ThingDef> list = Find.World.NaturalRockTypesIn(map.Tile).ToList();
		if (list.Count == 0)
		{
			list.Add(ThingDefOf.Sandstone);
		}
		List<Perlin> noises = ABRockGen.MakeNoises(list.Count);
		TerrainGrid terrainGrid = map.terrainGrid;
		foreach (IntVec3 allCell in map.AllCells)
		{
			ThingDef val = list[ABRockGen.PickIndex(noises, allCell)];
			TerrainDef val2 = val.building?.naturalTerrain ?? TerrainDefOf.Gravel;
			terrainGrid.SetTerrain(allCell, val2);
			GenSpawn.Spawn(val, allCell, map, (WipeMode)0);
		}
		map.fogGrid.Refog(CellRect.WholeMap(map));
		ABOreGen.ScatterOres(map, null, Mathf.Clamp(ABMod.Settings?.basementOreDensity ?? 6f, 0f, 12f));
	}
}
