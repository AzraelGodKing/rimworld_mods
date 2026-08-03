using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AsAboveSoBelow;

public class GenStep_ABCavernCarve : GenStep
{
	private const int Margin = 7;

	public override int SeedPart => 762195843;

	public override void Generate(Map map, GenStepParams parms)
	{
		if (!ABGuard.On(ABGuard.LevelGen))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.cavernBasements || !BiomesCavernsCompat.Active)
		{
			return;
		}
		LevelMapGen.Context currentContext = LevelMapGen.CurrentContext;
		if (currentContext != null && currentContext.levelToGenerate >= 0)
		{
			return;
		}
		BiomeDef val = BiomesCavernsCompat.Resolve(settings.cavernBiome);
		if (val == null)
		{
			return;
		}
		try
		{
			Carve(map, val, Mathf.Clamp(settings.cavernOpenness, 0.1f, 0.6f));
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.LevelGen, e, "cavern basement carve");
		}
	}

	private static void Carve(Map map, BiomeDef biome, float openness)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0771: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		//IL_060f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_07da: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
		if (map.pocketTileInfo == null)
		{
			ABLog.Dev("Cavern carve: no pocket tile, basement stays solid rock.");
			return;
		}
		map.pocketTileInfo.PrimaryBiome = biome;
		float num = Mathf.Clamp(ABMod.Settings?.cavernChamberFreq ?? 0.02f, 0.01f, 0.05f);
		ABLog.Dev("Cavern basement: " + ((Def)biome).defName + ", openness " + openness.ToString("0.00") + ", chambers " + num.ToString("0.000") + ".");
		CellRect val = CellRect.WholeMap(map);
		CellRect inner = ((CellRect)(ref val)).ContractedBy(7);
		CellIndices indices = map.cellIndices;
		bool[] carved = new bool[((CellIndices)(ref indices)).NumGridCells];
		List<IntVec3> carvedList = new List<IntVec3>();
		int num2 = Mathf.Max(3, Mathf.RoundToInt((float)((CellRect)(ref inner)).Area / 10000f * (4f + 14f * openness)));
		for (int i = 0; i < num2; i++)
		{
			IntVec3 val2 = ((i == 0 || carvedList.Count == 0) ? ((CellRect)(ref inner)).RandomCell : GenCollection.RandomElement<IntVec3>((IEnumerable<IntVec3>)carvedList));
			Vector3 val3 = ((IntVec3)(ref val2)).ToVector3Shifted();
			float num3 = Rand.Range(0f, 360f);
			int num4 = Rand.RangeInclusive(50, 130);
			for (int j = 0; j < num4; j++)
			{
				num3 += Rand.Range(-22f, 22f);
				Vector3 val4 = val3 + Quaternion.AngleAxis(num3, Vector3.up) * Vector3.forward;
				IntVec3 val5 = IntVec3Utility.ToIntVec3(val4);
				if (!((CellRect)(ref inner)).Contains(val5))
				{
					num3 += 160f + Rand.Range(0f, 40f);
					continue;
				}
				val3 = val4;
				CarveDisc(val5, (Rand.Value < 0.9f) ? 1.4f : 2.1f);
				if (Rand.Value < num)
				{
					CarveDisc(val5, Rand.Range(3f, 4.8f));
				}
			}
		}
		Perlin val6 = new Perlin(0.021, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), (QualityMode)1);
		TerrainGrid terrainGrid = map.terrainGrid;
		List<TerrainPatchMaker> terrainPatchMakers = biome.terrainPatchMakers;
		for (int k = 0; k < carvedList.Count; k++)
		{
			IntVec3 val7 = carvedList[k];
			Building edifice = GridsUtility.GetEdifice(val7, map);
			if (edifice != null)
			{
				BuildingProperties building = ((Thing)edifice).def.building;
				if (building == null || (!building.isNaturalRock && !building.isResourceRock))
				{
					continue;
				}
				((Thing)edifice).Destroy((DestroyMode)0);
			}
			float num5 = (float)(((ModuleBase)val6).GetValue((double)val7.x, 0.0, (double)val7.z) + 1.0) * 0.6f;
			TerrainDef val8 = null;
			if (terrainPatchMakers != null)
			{
				for (int l = 0; l < terrainPatchMakers.Count; l++)
				{
					if (val8 != null)
					{
						break;
					}
					val8 = terrainPatchMakers[l].TerrainAt(val7, map, num5);
				}
			}
			if (val8 == null)
			{
				val8 = TerrainThreshold.TerrainAtValue(biome.terrainsByFertility, num5);
			}
			if (val8 != null)
			{
				terrainGrid.SetTerrain(val7, val8);
			}
		}
		List<ThingDef> list = Find.World.NaturalRockTypesIn(map.Tile).ToListSafe();
		if (list.Count == 0)
		{
			list.Add(ThingDefOf.Sandstone);
		}
		List<Perlin> noises = ABRockGen.MakeNoises(list.Count);
		int num6 = 0;
		for (int m = 0; m < carvedList.Count; m++)
		{
			IntVec3 val9 = carvedList[m];
			if (!RoofCollapseUtility.WithinRangeOfRoofHolder(val9, map, false))
			{
				ThingDef val10 = list[ABRockGen.PickIndex(noises, val9)];
				GenSpawn.Spawn(val10, val9, map, (WipeMode)0);
				terrainGrid.SetTerrain(val9, val10.building?.naturalTerrain ?? TerrainDefOf.Gravel);
				num6++;
			}
		}
		if (num6 > 0)
		{
			ABLog.Dev("Cavern carve: " + num6 + " support pillars added.");
		}
		float num7 = Mathf.Clamp(ABMod.Settings?.cavernFormations ?? 1f, 0f, 2f);
		int num8 = Mathf.FloorToInt(num7);
		if (Rand.Chance(num7 - (float)num8))
		{
			num8++;
		}
		for (int n = 0; n < num8; n++)
		{
			BiomesCavernsCompat.RunForeignGenStep("BMT_ScatterStalagmiteGenerator", map);
		}
		if (((Def)biome).defName == "BMT_CrystalCaverns")
		{
			BiomesCavernsCompat.RunForeignGenStep("BMT_CrystalsGenerator", map);
		}
		List<ThingDef> list2 = biome.AllWildPlants.ToListSafe();
		if (list2.Count > 0)
		{
			float num9 = 0.08f * Mathf.Max(0.5f, biome.plantDensity);
			for (int num10 = 0; num10 < carvedList.Count; num10++)
			{
				IntVec3 val11 = carvedList[num10];
				TerrainDef terrain = GridsUtility.GetTerrain(val11, map);
				if (((BuildableDef)terrain).fertility <= 0.01f || !GenGrid.Standable(val11, map) || GridsUtility.GetPlant(val11, map) != null || GridsUtility.GetEdifice(val11, map) != null || !Rand.Chance(num9 * ((BuildableDef)terrain).fertility))
				{
					continue;
				}
				ThingDef val12 = GenCollection.RandomElementByWeight<ThingDef>((IEnumerable<ThingDef>)list2, (Func<ThingDef, float>)((ThingDef p) => biome.CommonalityOfPlant(p)));
				if (val12?.plant != null && !(val12.plant.fertilityMin > ((BuildableDef)terrain).fertility))
				{
					Thing obj = GenSpawn.Spawn(val12, val11, map, (WipeMode)0);
					Plant val13 = (Plant)(object)((obj is Plant) ? obj : null);
					if (val13 != null)
					{
						val13.Growth = Rand.Range(0.2f, 0.95f);
					}
				}
			}
		}
		List<PawnKindDef> list3 = biome.AllWildAnimals.ToListSafe();
		if (list3.Count <= 0)
		{
			return;
		}
		int num11 = Mathf.Clamp(Mathf.RoundToInt((float)carvedList.Count / 10000f * Mathf.Max(1f, biome.animalDensity) * 1.5f), 2, 10);
		for (int num12 = 0; num12 < num11; num12++)
		{
			IntVec3 val14 = GenCollection.RandomElement<IntVec3>((IEnumerable<IntVec3>)carvedList);
			if (GenGrid.Standable(val14, map))
			{
				PawnKindDef val15 = GenCollection.RandomElementByWeight<PawnKindDef>((IEnumerable<PawnKindDef>)list3, (Func<PawnKindDef, float>)((PawnKindDef val17) => biome.CommonalityOfAnimal(val17)));
				if (val15 != null)
				{
					Pawn val16 = PawnGenerator.GeneratePawn(val15, (Faction)null, (PlanetTile?)null);
					GenSpawn.Spawn((Thing)(object)val16, val14, map, (WipeMode)0);
				}
			}
		}
		void CarveDisc(IntVec3 center, float radius)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			int num13 = GenRadial.NumCellsInRadius(radius);
			for (int num14 = 0; num14 < num13; num14++)
			{
				IntVec3 val17 = center + GenRadial.RadialPattern[num14];
				if (((CellRect)(ref inner)).Contains(val17))
				{
					int num15 = ((CellIndices)(ref indices)).CellToIndex(val17);
					if (!carved[num15])
					{
						carved[num15] = true;
						carvedList.Add(val17);
					}
				}
			}
		}
	}
}
