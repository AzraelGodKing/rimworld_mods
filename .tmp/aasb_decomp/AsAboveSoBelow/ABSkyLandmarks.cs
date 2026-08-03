using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class ABSkyLandmarks
{
	internal const int ModeDisabled = 0;

	internal const int ModeRandom = 1;

	internal const int ModeEnabled = 2;

	internal const int ModeForced = 3;

	private static readonly string[] UnsuitableTerms = new string[20]
	{
		"coast", "river", "lake", "island", "ocean", "water", "harbor", "harbour", "bay", "pond",
		"cave", "cove", "reef", "beach", "delta", "fjord", "lagoon", "marsh", "swamp", "archipelago"
	};

	private static List<LandmarkDef> cachedAll;

	private static Dictionary<LandmarkDef, string> displayLabels;

	internal const int RequiredClearSquare = 7;

	internal static bool SystemActive => ModsConfig.OdysseyActive;

	internal static List<LandmarkDef> AllLandmarks()
	{
		if (cachedAll == null)
		{
			cachedAll = new List<LandmarkDef>(DefDatabase<LandmarkDef>.AllDefsListForReading);
			GenCollection.SortBy<LandmarkDef, string>(cachedAll, (Func<LandmarkDef, string>)((LandmarkDef d) => DisplayLabel(d)));
		}
		return cachedAll;
	}

	internal static string DisplayLabel(LandmarkDef def)
	{
		if (displayLabels == null)
		{
			displayLabels = new Dictionary<LandmarkDef, string>();
			List<LandmarkDef> allDefsListForReading = DefDatabase<LandmarkDef>.AllDefsListForReading;
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				string key = ((Def)allDefsListForReading[i]).label ?? ((Def)allDefsListForReading[i]).defName;
				dictionary.TryGetValue(key, out var value);
				dictionary[key] = value + 1;
			}
			for (int j = 0; j < allDefsListForReading.Count; j++)
			{
				LandmarkDef val = allDefsListForReading[j];
				string text = ((Def)val).label ?? ((Def)val).defName;
				string value2 = text;
				if (dictionary[text] > 1)
				{
					string text2 = RequiredMutatorLabel(val);
					value2 = ((!GenText.NullOrEmpty(text2) && text2 != text) ? text2 : (text + " (" + ((Def)val).defName + ")"));
				}
				displayLabels[val] = value2;
			}
		}
		string value3;
		return displayLabels.TryGetValue(def, out value3) ? value3 : (((Def)def).label ?? ((Def)def).defName);
	}

	private static string RequiredMutatorLabel(LandmarkDef def)
	{
		for (int i = 0; i < def.mutatorChances.Count; i++)
		{
			MutatorChance val = def.mutatorChances[i];
			if (val.required && val.mutator != null && !GenText.NullOrEmpty(((Def)val.mutator).label))
			{
				return ((Def)val.mutator).label;
			}
		}
		return null;
	}

	internal static string DescriptionFor(LandmarkDef def)
	{
		if (!GenText.NullOrEmpty(((Def)def).description))
		{
			return ((Def)def).description;
		}
		for (int i = 0; i < def.mutatorChances.Count; i++)
		{
			MutatorChance val = def.mutatorChances[i];
			if (val.required && val.mutator != null && !GenText.NullOrEmpty(((Def)val.mutator).description))
			{
				return ((Def)val.mutator).description;
			}
		}
		return null;
	}

	internal static int DefaultModeFor(LandmarkDef def)
	{
		string text = (((Def)def).defName + " " + def.category).ToLowerInvariant();
		for (int i = 0; i < def.mutatorChances.Count; i++)
		{
			TileMutatorDef mutator = def.mutatorChances[i].mutator;
			if (mutator != null)
			{
				text = text + " " + ((Def)mutator).defName.ToLowerInvariant();
			}
		}
		for (int j = 0; j < UnsuitableTerms.Length; j++)
		{
			if (text.Contains(UnsuitableTerms[j]))
			{
				return 0;
			}
		}
		return 1;
	}

	internal static int ModeFor(ABSettings settings, LandmarkDef def)
	{
		if (settings?.landmarkModes != null && settings.landmarkModes.TryGetValue(((Def)def).defName, out var value))
		{
			return Mathf.Clamp(value, 0, 3);
		}
		return DefaultModeFor(def);
	}

	internal static void SetMode(ABSettings settings, LandmarkDef def, int mode)
	{
		if (settings.landmarkModes == null)
		{
			settings.landmarkModes = new Dictionary<string, int>();
		}
		if (mode == DefaultModeFor(def))
		{
			settings.landmarkModes.Remove(((Def)def).defName);
		}
		else
		{
			settings.landmarkModes[((Def)def).defName] = mode;
		}
	}

	internal static string ModeLabel(int mode)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		return mode switch
		{
			3 => TaggedString.op_Implicit(Translator.Translate("AB_LandmarkForced")), 
			2 => TaggedString.op_Implicit(Translator.Translate("AB_LandmarkEnabled")), 
			0 => TaggedString.op_Implicit(Translator.Translate("AB_LandmarkDisabled")), 
			_ => TaggedString.op_Implicit(Translator.Translate("AB_LandmarkRandom")), 
		};
	}

	internal static bool SuitableFor(LandmarkDef def, Map skyMap)
	{
		Tile tileInfo = skyMap.TileInfo;
		BiomeDef biome = ((tileInfo != null) ? tileInfo.PrimaryBiome : null);
		for (int i = 0; i < def.mutatorChances.Count; i++)
		{
			MutatorChance val = def.mutatorChances[i];
			if (val.mutator == null)
			{
				return false;
			}
			if ((val.required || !(val.chance < 1f)) && !BiomeOk(val.mutator, biome))
			{
				return false;
			}
		}
		return true;
	}

	private static bool BiomeOk(TileMutatorDef mutator, BiomeDef biome)
	{
		if (biome == null)
		{
			return true;
		}
		List<BiomeDef> biomeWhitelist = mutator.biomeWhitelist;
		if (biomeWhitelist != null && biomeWhitelist.Count > 0 && !biomeWhitelist.Contains(biome))
		{
			return false;
		}
		List<BiomeDef> biomeBlacklist = mutator.biomeBlacklist;
		if (biomeBlacklist != null && biomeBlacklist.Count > 0 && biomeBlacklist.Contains(biome))
		{
			return false;
		}
		return true;
	}

	internal static void RollAndApply(Map skyMap, int plateauCellCount, bool[] plateauMask, ABSettings settings)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (!SystemActive || settings == null || !settings.skyLandmarks || !ABGuard.On(ABGuard.LevelGen))
		{
			return;
		}
		try
		{
			if (plateauCellCount < 80)
			{
				ABLog.Dev("Sky landmarks: no open plateau (" + plateauCellCount + " cells), skipped.");
				return;
			}
			if (!HasClearSquare(plateauMask, skyMap.Size.x, skyMap.Size.z, 7))
			{
				ABLog.Dev("Sky landmarks: no clear " + 7 + "x" + 7 + " plateau square, skipped.");
				return;
			}
			int num = Mathf.Clamp(settings.skyLandmarkMax, 1, 3);
			int num2 = 0;
			List<LandmarkDef> list = AllLandmarks();
			for (int i = 0; i < list.Count; i++)
			{
				if (num2 >= num)
				{
					break;
				}
				if (ModeFor(settings, list[i]) == 3)
				{
					Apply(skyMap, list[i]);
					num2++;
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (num2 >= num)
				{
					break;
				}
				if (ModeFor(settings, list[j]) == 2 && SuitableFor(list[j], skyMap))
				{
					Apply(skyMap, list[j]);
					num2++;
				}
			}
			if (num2 < num && Rand.Chance(Mathf.Clamp01(settings.skyLandmarkChance)))
			{
				List<LandmarkDef> list2 = new List<LandmarkDef>();
				for (int k = 0; k < list.Count; k++)
				{
					if (ModeFor(settings, list[k]) == 1 && SuitableFor(list[k], skyMap))
					{
						list2.Add(list[k]);
					}
				}
				LandmarkDef def = default(LandmarkDef);
				if (list2.Count > 0 && GenCollection.TryRandomElementByWeight<LandmarkDef>((IEnumerable<LandmarkDef>)list2, (Func<LandmarkDef, float>)((LandmarkDef d) => Mathf.Max(d.commonality, 0.05f)), ref def))
				{
					Apply(skyMap, def);
					num2++;
				}
			}
			if (num2 > 0)
			{
				ABLandmarkPlacement.BeginScope(skyMap, plateauMask);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.LevelGen, e, "sky landmark roll");
		}
	}

	internal static bool HasClearSquare(bool[] mask, int w, int h, int size)
	{
		if (mask == null || w < size || h < size)
		{
			return false;
		}
		for (int i = 0; i <= h - size; i++)
		{
			for (int j = 0; j <= w - size; j++)
			{
				bool flag = true;
				for (int k = 0; k < size && flag; k++)
				{
					int num = (i + k) * w + j;
					for (int l = 0; l < size; l++)
					{
						if (!mask[num + l])
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void Apply(Map skyMap, LandmarkDef def)
	{
		Tile pocketTileInfo = skyMap.pocketTileInfo;
		if (pocketTileInfo == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < def.mutatorChances.Count; i++)
		{
			MutatorChance val = def.mutatorChances[i];
			if (val.mutator != null && (val.required || Rand.Chance(val.chance)))
			{
				pocketTileInfo.AddMutator(val.mutator);
				num++;
			}
		}
		ABLog.Dev("Sky landmark applied: " + ((Def)def).defName + " (" + num + " mutator(s)).");
	}
}
