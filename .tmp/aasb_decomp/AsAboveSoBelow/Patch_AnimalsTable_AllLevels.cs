using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_AnimalsTable_AllLevels
{
	private static void Postfix(ref IEnumerable<Pawn> __result)
	{
		if (!ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		try
		{
			Map currentMap = Find.CurrentMap;
			LevelComp levelComp = currentMap?.Controller();
			if (levelComp == null || levelComp.MapByLevel.Count <= 1)
			{
				return;
			}
			List<Pawn> list = new List<Pawn>(__result);
			foreach (KeyValuePair<int, Map> item in levelComp.MapByLevel.OrderByDescending((KeyValuePair<int, Map> k) => k.Key))
			{
				Map value = item.Value;
				if (value != null && value != currentMap && !value.Disposed)
				{
					list.AddRange(value.mapPawns.ColonyAnimals);
				}
			}
			__result = list;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "animals table augmentation");
		}
	}
}
