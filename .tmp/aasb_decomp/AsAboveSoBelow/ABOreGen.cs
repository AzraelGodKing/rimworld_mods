using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class ABOreGen
{
	internal static void ScatterOres(Map map, List<IntVec3> candidates, float lumpsPer10kCells)
	{
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (candidates != null && candidates.Count == 0)
			{
				return;
			}
			List<ThingDef> list = new List<ThingDef>();
			List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				ThingDef val = allDefsListForReading[i];
				if (val.building != null && val.building.isResourceRock && val.building.mineableScatterCommonality > 0f)
				{
					list.Add(val);
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			int num = candidates?.Count ?? map.Area;
			int num2 = Mathf.Max(1, Mathf.RoundToInt((float)num / 10000f * lumpsPer10kCells));
			for (int j = 0; j < num2; j++)
			{
				ThingDef val2 = GenCollection.RandomElementByWeight<ThingDef>((IEnumerable<ThingDef>)list, (Func<ThingDef, float>)((ThingDef d) => d.building.mineableScatterCommonality));
				IntVec3 val3 = ((candidates != null) ? GenCollection.RandomElement<IntVec3>((IEnumerable<IntVec3>)candidates) : CellFinder.RandomCell(map));
				int randomInRange = ((IntRange)(ref val2.building.mineableScatterLumpSizeRange)).RandomInRange;
				List<IntVec3> list2 = GridShapeMaker.IrregularLump(val3, map, randomInRange, (Predicate<IntVec3>)null);
				for (int num3 = 0; num3 < list2.Count; num3++)
				{
					IntVec3 val4 = list2[num3];
					Building edifice = GridsUtility.GetEdifice(val4, map);
					if (edifice != null && ((Thing)edifice).def.building != null && ((Thing)edifice).def.building.isNaturalRock && !((Thing)edifice).def.building.isResourceRock)
					{
						GenSpawn.Spawn(val2, val4, map, (WipeMode)0);
					}
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.LevelGen, e, "ore scatter");
		}
	}
}
