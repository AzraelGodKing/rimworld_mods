using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

public static class BiomesCavernsCompat
{
	public const string PackageId = "BiomesTeam.BiomesCaverns";

	public const string RandomChoice = "Random";

	private static bool resolved;

	private static bool activeCached;

	private static readonly string[] BiomeNames = new string[3] { "BMT_FungalForest", "BMT_CrystalCaverns", "BMT_EarthenDepths" };

	public static bool Active
	{
		get
		{
			if (!resolved)
			{
				resolved = true;
				activeCached = ModsConfig.IsActive("BiomesTeam.BiomesCaverns") || ModsConfig.IsActive("BiomesTeam.BiomesCaverns_steam");
			}
			return activeCached;
		}
	}

	private static float WeightOf(string defName)
	{
		if (defName == "BMT_FungalForest")
		{
			return 50f;
		}
		if (defName == "BMT_CrystalCaverns")
		{
			return 35f;
		}
		return 15f;
	}

	public static List<BiomeDef> CavernBiomes()
	{
		List<BiomeDef> list = new List<BiomeDef>();
		if (!Active)
		{
			return list;
		}
		for (int i = 0; i < BiomeNames.Length; i++)
		{
			BiomeDef namedSilentFail = DefDatabase<BiomeDef>.GetNamedSilentFail(BiomeNames[i]);
			if (namedSilentFail != null)
			{
				list.Add(namedSilentFail);
			}
		}
		return list;
	}

	public static BiomeDef Resolve(string choice)
	{
		List<BiomeDef> list = CavernBiomes();
		if (list.Count == 0)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(choice) && choice != "Random")
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (((Def)list[i]).defName == choice)
				{
					return list[i];
				}
			}
		}
		return GenCollection.RandomElementByWeight<BiomeDef>((IEnumerable<BiomeDef>)list, (Func<BiomeDef, float>)((BiomeDef b) => WeightOf(((Def)b).defName)));
	}

	public static void RunForeignGenStep(string defName, Map map)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GenStepDef namedSilentFail = DefDatabase<GenStepDef>.GetNamedSilentFail(defName);
			if (namedSilentFail?.genStep != null)
			{
				namedSilentFail.genStep.Generate(map, default(GenStepParams));
			}
		}
		catch (Exception ex)
		{
			ABLog.Dev("Foreign genstep " + defName + " failed (ignored): " + ex.Message);
		}
	}
}
