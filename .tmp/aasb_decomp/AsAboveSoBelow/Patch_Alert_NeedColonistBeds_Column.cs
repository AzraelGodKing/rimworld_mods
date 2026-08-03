using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Alert_NeedColonistBeds), "NeedColonistBeds")]
internal static class Patch_Alert_NeedColonistBeds_Column
{
	private static void Postfix(Map map, ref bool __result)
	{
		if (!ABGuard.On(ABGuard.Ui) || map == null)
		{
			return;
		}
		try
		{
			LevelComp levelComp = map.Levels();
			if (levelComp == null)
			{
				return;
			}
			if (levelComp.level != 0)
			{
				__result = false;
				return;
			}
			LevelComp levelComp2 = map.Controller();
			if (levelComp2 == null || levelComp2.MapByLevel.Count <= 1)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			int num3 = default(int);
			int num4 = default(int);
			int num5 = default(int);
			foreach (KeyValuePair<int, Map> item in levelComp2.MapByLevel)
			{
				Map value = item.Value;
				if (value != null && !value.Disposed)
				{
					Alert_NeedColonistBeds.AvailableColonistBeds(value, false, ref num3, ref num4, ref num5);
					num += num3;
					num2 += num4;
				}
			}
			__result = num < 0 || num2 < 0;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "colonist beds alert aggregation");
		}
	}
}
