using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AsAboveSoBelow;

public class GenStep_ABSkyTerrain : GenStep
{
	private const byte KindOutside = 0;

	private const byte KindLedge = 1;

	private const byte KindWall = 2;

	private const byte KindPlateau = 3;

	private const float MeadowCutoffDefault = 0.6f;

	public override int SeedPart => 762195842;

	public override void Generate(Map map, GenStepParams parms)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Expected O, but got Unknown
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0561: Unknown result type (might be due to invalid IL or missing references)
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		Map val = map.GroundMap();
		if (val != null && map.pocketTileInfo != null)
		{
			ABSettings settings = ABMod.Settings;
			if (settings == null || settings.skyBiomeInherit)
			{
				Tile tileInfo = val.TileInfo;
				BiomeDef val2 = ((tileInfo != null) ? tileInfo.PrimaryBiome : null);
				if (val2 != null)
				{
					map.pocketTileInfo.PrimaryBiome = val2;
					ABLog.Dev("Sky level biome inherited from surface: " + ((Def)val2).defName + ".");
				}
			}
		}
		List<ThingDef> list = Find.World.NaturalRockTypesIn(map.Tile).ToList();
		if (list.Count == 0)
		{
			list.Add(ThingDefOf.Sandstone);
		}
		List<Perlin> noises = ABRockGen.MakeNoises(list.Count);
		TerrainGrid terrainGrid = map.terrainGrid;
		CellIndices cellIndices = map.cellIndices;
		int numGridCells = ((CellIndices)(ref cellIndices)).NumGridCells;
		bool[] array = new bool[numGridCells];
		foreach (IntVec3 allCell in map.AllCells)
		{
			RoofDef val3 = null;
			Building val4 = null;
			if (val != null && GenGrid.InBounds(allCell, val))
			{
				val3 = val.roofGrid.RoofAt(allCell);
				val4 = val.edificeGrid[allCell];
			}
			array[((CellIndices)(ref cellIndices)).CellToIndex(allCell)] = (val4 != null && ((Thing)val4).def.mineable) || (val3 != null && val3.isNatural && val3.isThickRoof);
		}
		ABSettings settings2 = ABMod.Settings;
		bool flag = settings2?.naturalPeaks ?? true;
		byte[] array2 = ClassifyMass(map, cellIndices, array, flag);
		if (flag)
		{
			float num = Mathf.Clamp(settings2?.peakOutcropDensity ?? 1f, 0f, 2f);
			if (num > 0.001f)
			{
				AddOutcrops(map, cellIndices, array2, num);
			}
			BreachHiddenValleys(map, cellIndices, array2, Mathf.Clamp(settings2?.peakHiddenValleys ?? 1f, 0f, 1f));
		}
		bool[] array3 = new bool[numGridCells];
		for (int i = 0; i < numGridCells; i++)
		{
			array3[i] = array2[i] == 2;
		}
		List<IntVec3> list2 = new List<IntVec3>();
		List<IntVec3> list3 = new List<IntVec3>();
		List<IntVec3> list4 = new List<IntVec3>();
		Perlin val5 = new Perlin(0.05, 2.0, 0.5, 5, Rand.Range(0, int.MaxValue), (QualityMode)1);
		float num2 = Mathf.Clamp(settings2?.peakSoilFraction ?? 0.15f, 0f, 0.5f);
		foreach (IntVec3 allCell2 in map.AllCells)
		{
			int num3 = ((CellIndices)(ref cellIndices)).CellToIndex(allCell2);
			switch (array2[num3])
			{
			case 1:
				terrainGrid.SetTerrain(allCell2, ABDefOf.AB_MountainTop);
				break;
			case 2:
			{
				ThingDef val7 = list[ABRockGen.PickIndex(noises, allCell2)];
				terrainGrid.SetTerrain(allCell2, ABDefOf.AB_MountainTop);
				GenSpawn.Spawn(val7, allCell2, map, (WipeMode)0);
				list3.Add(allCell2);
				map.roofGrid.SetRoof(allCell2, RoofDefOf.RoofRockThick);
				if (AllWithinRadius(map, cellIndices, array3, allCell2, 2))
				{
					list2.Add(allCell2);
				}
				break;
			}
			case 3:
			{
				float num4 = (float)(((ModuleBase)val5).GetValue((double)allCell2.x, 0.0, (double)allCell2.z) + 1.0) * 0.5f;
				TerrainDef val8;
				if (num4 > 1f - num2)
				{
					val8 = TerrainDefOf.Soil;
				}
				else if (num4 < 0.22f)
				{
					ThingDef val9 = list[ABRockGen.PickIndex(noises, allCell2)];
					val8 = val9.building?.naturalTerrain ?? TerrainDefOf.Gravel;
				}
				else
				{
					val8 = TerrainDefOf.Gravel;
				}
				terrainGrid.SetTerrain(allCell2, val8);
				list4.Add(allCell2);
				break;
			}
			default:
			{
				bool flag2 = val != null && GenGrid.InBounds(allCell2, val);
				RoofDef val6 = (flag2 ? val.roofGrid.RoofAt(allCell2) : null);
				if (flag2 && LevelSync.CoveredBelow(val, allCell2))
				{
					terrainGrid.SetTerrain(allCell2, ABDefOf.AB_RoofSurface);
				}
				else if (val6 != null && val6.isNatural)
				{
					terrainGrid.SetTerrain(allCell2, ABDefOf.AB_MountainTop);
				}
				else
				{
					terrainGrid.SetTerrain(allCell2, ABDefOf.AB_OpenAir);
				}
				break;
			}
			}
		}
		FogGrid fogGrid = map.fogGrid;
		for (int j = 0; j < list2.Count; j++)
		{
			IntVec3 val10 = list2[j];
			fogGrid.Refog(new CellRect(val10.x, val10.z, 1, 1));
		}
		FogEnclosedMeadows(map, cellIndices, array2, fogGrid);
		ABOreGen.ScatterOres(map, list3, Mathf.Clamp(settings2?.skyOreDensity ?? 6f, 0f, 12f));
		SeedTarns(map, cellIndices, array2, list4, settings2);
		SeedPlateauFlora(map, val, list4, settings2);
		bool[] array4 = new bool[numGridCells];
		foreach (IntVec3 allCell3 in map.AllCells)
		{
			int num5 = ((CellIndices)(ref cellIndices)).CellToIndex(allCell3);
			int num6;
			if (array2[num5] == 3)
			{
				TerrainDef obj = terrainGrid.TerrainAt(allCell3);
				num6 = ((obj == null || !obj.IsWater) ? 1 : 0);
			}
			else
			{
				num6 = 0;
			}
			array4[num5] = (byte)num6 != 0;
		}
		ABSkyLandmarks.RollAndApply(map, list4.Count, array4, settings2);
	}

	private static void BreachHiddenValleys(Map map, CellIndices indices, byte[] kind, float keepChance)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		if (keepChance >= 0.999f)
		{
			return;
		}
		int numGridCells = ((CellIndices)(ref indices)).NumGridCells;
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		bool[] array = new bool[numGridCells];
		Queue<IntVec3> queue = new Queue<IntVec3>();
		foreach (IntVec3 allCell in map.AllCells)
		{
			int num = ((CellIndices)(ref indices)).CellToIndex(allCell);
			byte b = kind[num];
			bool flag = b == 1;
			if (!flag && b == 3)
			{
				for (int i = 0; i < adjacentCells.Length; i++)
				{
					if (flag)
					{
						break;
					}
					IntVec3 val = allCell + adjacentCells[i];
					flag = GenGrid.InBounds(val, map) && kind[((CellIndices)(ref indices)).CellToIndex(val)] == 0;
				}
			}
			if (flag)
			{
				array[num] = true;
				queue.Enqueue(allCell);
			}
		}
		while (queue.Count > 0)
		{
			IntVec3 val2 = queue.Dequeue();
			for (int j = 0; j < adjacentCells.Length; j++)
			{
				IntVec3 val3 = val2 + adjacentCells[j];
				if (GenGrid.InBounds(val3, map))
				{
					int num2 = ((CellIndices)(ref indices)).CellToIndex(val3);
					if (!array[num2] && (kind[num2] == 3 || kind[num2] == 1))
					{
						array[num2] = true;
						queue.Enqueue(val3);
					}
				}
			}
		}
		int[] array2 = new int[numGridCells];
		List<List<IntVec3>> list = new List<List<IntVec3>>();
		Stack<IntVec3> stack = new Stack<IntVec3>();
		foreach (IntVec3 allCell2 in map.AllCells)
		{
			int num3 = ((CellIndices)(ref indices)).CellToIndex(allCell2);
			if (kind[num3] != 3 || array[num3] || array2[num3] != 0)
			{
				continue;
			}
			List<IntVec3> list2 = new List<IntVec3>();
			list.Add(list2);
			int num4 = (array2[num3] = list.Count);
			stack.Push(allCell2);
			while (stack.Count > 0)
			{
				IntVec3 val4 = stack.Pop();
				list2.Add(val4);
				for (int k = 0; k < adjacentCells.Length; k++)
				{
					IntVec3 val5 = val4 + adjacentCells[k];
					if (GenGrid.InBounds(val5, map))
					{
						int num5 = ((CellIndices)(ref indices)).CellToIndex(val5);
						if (array2[num5] == 0 && !array[num5] && kind[num5] == 3)
						{
							array2[num5] = num4;
							stack.Push(val5);
						}
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		int num6 = 0;
		for (int l = 0; l < list.Count; l++)
		{
			if (Rand.Chance(keepChance))
			{
				continue;
			}
			List<IntVec3> list3 = list[l];
			bool[] array3 = new bool[numGridCells];
			int[] array4 = new int[numGridCells];
			Queue<IntVec3> queue2 = new Queue<IntVec3>();
			for (int m = 0; m < list3.Count; m++)
			{
				int num7 = ((CellIndices)(ref indices)).CellToIndex(list3[m]);
				array3[num7] = true;
				array4[num7] = -1;
				queue2.Enqueue(list3[m]);
			}
			IntVec3 val6 = IntVec3.Invalid;
			while (queue2.Count > 0 && val6 == IntVec3.Invalid)
			{
				IntVec3 val7 = queue2.Dequeue();
				int num8 = ((CellIndices)(ref indices)).CellToIndex(val7);
				for (int n = 0; n < cardinalDirections.Length; n++)
				{
					IntVec3 val8 = val7 + cardinalDirections[n];
					if (!GenGrid.InBounds(val8, map))
					{
						continue;
					}
					int num9 = ((CellIndices)(ref indices)).CellToIndex(val8);
					if (!array3[num9])
					{
						byte b2 = kind[num9];
						if (b2 == 2)
						{
							array3[num9] = true;
							array4[num9] = num8;
							queue2.Enqueue(val8);
						}
						else if (kind[num8] == 2 && (b2 == 0 || ((b2 == 1 || b2 == 3) && array[num9])))
						{
							val6 = val7;
							break;
						}
					}
				}
			}
			if (!(val6 == IntVec3.Invalid))
			{
				int num10 = ((CellIndices)(ref indices)).CellToIndex(val6);
				while (num10 >= 0 && kind[num10] == 2)
				{
					kind[num10] = 1;
					num10 = array4[num10];
				}
				num6++;
			}
		}
		if (num6 > 0)
		{
			ABLog.Dev("Peak plateau: breached " + num6 + " enclosed valley(s) per settings.");
		}
	}

	private static void SeedTarns(Map map, CellIndices indices, byte[] kind, List<IntVec3> plateauCells, ABSettings settings)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(settings?.peakTarns ?? 1f, 0f, 2f);
		if (num <= 0.001f || plateauCells.Count < 120)
		{
			return;
		}
		float num2 = (float)plateauCells.Count / 1400f * num;
		int num3 = Mathf.FloorToInt(num2);
		if (Rand.Chance(num2 - (float)num3))
		{
			num3++;
		}
		if (num3 <= 0)
		{
			return;
		}
		TerrainGrid terrainGrid = map.terrainGrid;
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		for (int i = 0; i < num3; i++)
		{
			IntVec3 val = GenCollection.RandomElement<IntVec3>((IEnumerable<IntVec3>)plateauCells);
			int num4 = Rand.RangeInclusive(8, 26);
			List<IntVec3> list = GridShapeMaker.IrregularLump(val, map, num4, (Predicate<IntVec3>)null);
			int num5 = 0;
			for (int j = 0; j < list.Count; j++)
			{
				IntVec3 val2 = list[j];
				if (GenGrid.InBounds(val2, map) && kind[((CellIndices)(ref indices)).CellToIndex(val2)] == 3)
				{
					terrainGrid.SetTerrain(val2, TerrainDefOf.WaterShallow);
					hashSet.Add(val2);
					num5++;
				}
			}
			if (num5 < 14)
			{
				continue;
			}
			List<IntVec3> list2 = GridShapeMaker.IrregularLump(val, map, Mathf.Max(4, num5 / 4), (Predicate<IntVec3>)null);
			for (int k = 0; k < list2.Count; k++)
			{
				IntVec3 val3 = list2[k];
				if (hashSet.Contains(val3))
				{
					terrainGrid.SetTerrain(val3, TerrainDefOf.WaterDeep);
				}
			}
		}
		if (hashSet.Count > 0)
		{
			plateauCells.RemoveAll(hashSet.Contains);
			ABLog.Dev("Peak plateau: " + hashSet.Count + " tarn cell(s) pooled.");
		}
	}

	private static byte[] ClassifyMass(Map map, CellIndices indices, bool[] solid, bool naturalistic)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Expected O, but got Unknown
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected O, but got Unknown
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		int numGridCells = ((CellIndices)(ref indices)).NumGridCells;
		byte[] array = new byte[numGridCells];
		int[] array2 = new int[numGridCells];
		Queue<IntVec3> queue = new Queue<IntVec3>();
		foreach (IntVec3 allCell in map.AllCells)
		{
			int num = ((CellIndices)(ref indices)).CellToIndex(allCell);
			if (solid[num])
			{
				array2[num] = int.MaxValue;
				continue;
			}
			array2[num] = 0;
			queue.Enqueue(allCell);
		}
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		while (queue.Count > 0)
		{
			IntVec3 val = queue.Dequeue();
			int num2 = array2[((CellIndices)(ref indices)).CellToIndex(val)] + 1;
			for (int i = 0; i < adjacentCells.Length; i++)
			{
				IntVec3 val2 = val + adjacentCells[i];
				if (GenGrid.InBounds(val2, map))
				{
					int num3 = ((CellIndices)(ref indices)).CellToIndex(val2);
					if (array2[num3] > num2)
					{
						array2[num3] = num2;
						queue.Enqueue(val2);
					}
				}
			}
		}
		if (!naturalistic)
		{
			for (int j = 0; j < numGridCells; j++)
			{
				if (solid[j])
				{
					array[j] = (byte)((array2[j] <= 1) ? 1 : 2);
				}
			}
			return array;
		}
		ABSettings settings = ABMod.Settings;
		float num4 = Mathf.Clamp(settings?.peakMeadowCutoff ?? 0.6f, 0.45f, 0.75f);
		float num5 = Mathf.Clamp(settings?.peakMeadowScale ?? 0.024f, 0.012f, 0.048f);
		int max = Mathf.Clamp(settings?.peakTerraceMax ?? 4, 1, 6);
		Perlin noise = new Perlin(0.035, 2.0, 0.5, 4, Rand.Range(0, int.MaxValue), (QualityMode)1);
		Perlin noise2 = new Perlin((double)num5, 2.0, 0.5, 5, Rand.Range(0, int.MaxValue), (QualityMode)1);
		int[] array3 = new int[numGridCells];
		List<bool> list = new List<bool> { false };
		Stack<IntVec3> stack = new Stack<IntVec3>();
		foreach (IntVec3 allCell2 in map.AllCells)
		{
			int num6 = ((CellIndices)(ref indices)).CellToIndex(allCell2);
			if (!solid[num6] || array3[num6] != 0)
			{
				continue;
			}
			int count = list.Count;
			list.Add(item: false);
			stack.Push(allCell2);
			array3[num6] = count;
			while (stack.Count > 0)
			{
				IntVec3 val3 = stack.Pop();
				int num7 = ((CellIndices)(ref indices)).CellToIndex(val3);
				int num8 = array2[num7];
				byte b;
				if (Noise01(noise2, val3) > num4)
				{
					if (num8 <= 1)
					{
						b = 1;
					}
					else
					{
						b = 3;
						list[count] = true;
					}
				}
				else
				{
					b = (byte)((num8 <= TerraceWidth(noise, val3, max)) ? 1 : 2);
				}
				array[num7] = b;
				for (int k = 0; k < adjacentCells.Length; k++)
				{
					IntVec3 val4 = val3 + adjacentCells[k];
					if (GenGrid.InBounds(val4, map))
					{
						int num9 = ((CellIndices)(ref indices)).CellToIndex(val4);
						if (solid[num9] && array3[num9] == 0)
						{
							array3[num9] = count;
							stack.Push(val4);
						}
					}
				}
			}
		}
		for (int l = 0; l < numGridCells; l++)
		{
			if (solid[l] && !list[array3[l]])
			{
				array[l] = (byte)((array2[l] <= 1) ? 1 : 2);
			}
		}
		return array;
	}

	private static int TerraceWidth(Perlin noise, IntVec3 c, int max)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		float num = Noise01(noise, c);
		if (num < 0.5f || max <= 1)
		{
			return 1;
		}
		int num2 = 1 + Mathf.FloorToInt(Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, num), 1.6f) * (float)max);
		return Mathf.Clamp(num2, 1, max);
	}

	private static float Noise01(Perlin noise, IntVec3 c)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return Mathf.Clamp01((float)(((ModuleBase)noise).GetValue((double)c.x, 0.0, (double)c.z) + 1.0) * 0.5f);
	}

	private static void FogEnclosedMeadows(Map map, CellIndices indices, byte[] kind, FogGrid fog)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		int numGridCells = ((CellIndices)(ref indices)).NumGridCells;
		bool[] array = new bool[numGridCells];
		Queue<IntVec3> queue = new Queue<IntVec3>();
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		foreach (IntVec3 allCell in map.AllCells)
		{
			int num = ((CellIndices)(ref indices)).CellToIndex(allCell);
			byte b = kind[num];
			bool flag = b == 1;
			if (!flag && b == 3)
			{
				for (int i = 0; i < adjacentCells.Length; i++)
				{
					if (flag)
					{
						break;
					}
					IntVec3 val = allCell + adjacentCells[i];
					flag = GenGrid.InBounds(val, map) && kind[((CellIndices)(ref indices)).CellToIndex(val)] == 0;
				}
			}
			if (flag)
			{
				array[num] = true;
				queue.Enqueue(allCell);
			}
		}
		IntVec3[] adjacentCells2 = GenAdj.AdjacentCells;
		while (queue.Count > 0)
		{
			IntVec3 val2 = queue.Dequeue();
			for (int j = 0; j < adjacentCells2.Length; j++)
			{
				IntVec3 val3 = val2 + adjacentCells2[j];
				if (GenGrid.InBounds(val3, map))
				{
					int num2 = ((CellIndices)(ref indices)).CellToIndex(val3);
					if (!array[num2] && (kind[num2] == 3 || kind[num2] == 1))
					{
						array[num2] = true;
						queue.Enqueue(val3);
					}
				}
			}
		}
		int num3 = 0;
		foreach (IntVec3 allCell2 in map.AllCells)
		{
			int num4 = ((CellIndices)(ref indices)).CellToIndex(allCell2);
			if (kind[num4] == 3 && !array[num4])
			{
				fog.Refog(new CellRect(allCell2.x, allCell2.z, 1, 1));
				num3++;
			}
		}
		if (num3 > 0)
		{
			ABLog.Dev("Peak plateau: " + num3 + " hidden-valley cell(s) fogged.");
		}
	}

	private static void AddOutcrops(Map map, CellIndices indices, byte[] kind, float density)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		List<IntVec3> list = new List<IntVec3>();
		foreach (IntVec3 allCell in map.AllCells)
		{
			if (kind[((CellIndices)(ref indices)).CellToIndex(allCell)] == 3)
			{
				list.Add(allCell);
			}
		}
		if (list.Count < 60)
		{
			return;
		}
		int num = Mathf.Max(1, Mathf.RoundToInt((float)list.Count / 900f * density));
		for (int i = 0; i < num; i++)
		{
			IntVec3 val = GenCollection.RandomElement<IntVec3>((IEnumerable<IntVec3>)list);
			int num2 = Rand.RangeInclusive(9, 42);
			List<IntVec3> list2 = GridShapeMaker.IrregularLump(val, map, num2, (Predicate<IntVec3>)null);
			for (int j = 0; j < list2.Count; j++)
			{
				int num3 = ((CellIndices)(ref indices)).CellToIndex(list2[j]);
				if (kind[num3] == 3)
				{
					kind[num3] = 2;
				}
			}
		}
	}

	private static void SeedPlateauFlora(Map map, Map ground, List<IntVec3> plateauCells, ABSettings settings)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		if (plateauCells.Count == 0)
		{
			return;
		}
		float num = Mathf.Clamp(settings?.peakVegetation ?? 1f, 0f, 2f);
		if (num <= 0.01f)
		{
			return;
		}
		BiomeDef biome = ((ground != null) ? ground.Biome : null);
		if (biome == null)
		{
			return;
		}
		List<ThingDef> list = biome.AllWildPlants.ToListSafe();
		if (list.Count == 0)
		{
			return;
		}
		float num2 = 0.09f * Mathf.Max(0.4f, biome.plantDensity) * num;
		for (int i = 0; i < plateauCells.Count; i++)
		{
			IntVec3 val = plateauCells[i];
			TerrainDef terrain = GridsUtility.GetTerrain(val, map);
			if (((BuildableDef)terrain).fertility <= 0.01f || !GenGrid.Standable(val, map) || GridsUtility.GetPlant(val, map) != null || GridsUtility.GetEdifice(val, map) != null || !Rand.Chance(num2 * ((BuildableDef)terrain).fertility))
			{
				continue;
			}
			ThingDef val2 = GenCollection.RandomElementByWeight<ThingDef>((IEnumerable<ThingDef>)list, (Func<ThingDef, float>)((ThingDef p) => biome.CommonalityOfPlant(p)));
			if (val2?.plant != null && !val2.plant.cavePlant && !(val2.plant.fertilityMin > ((BuildableDef)terrain).fertility))
			{
				Thing obj = GenSpawn.Spawn(val2, val, map, (WipeMode)0);
				Plant val3 = (Plant)(object)((obj is Plant) ? obj : null);
				if (val3 != null)
				{
					val3.Growth = Rand.Range(0.25f, 0.9f);
				}
			}
		}
	}

	private unsafe static bool AllWithinRadius(Map map, CellIndices indices, bool[] grid, IntVec3 c, int radius)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		CellRect val = CellRect.CenteredOn(c, radius);
		Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (!GenGrid.InBounds(current, map) || grid[((CellIndices)(ref indices)).CellToIndex(current)])
				{
					continue;
				}
				return false;
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		return true;
	}
}
