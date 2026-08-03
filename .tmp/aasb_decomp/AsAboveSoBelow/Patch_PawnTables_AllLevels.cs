using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_PawnTables_AllLevels
{
	private static void Postfix(MainTabWindow_PawnTable __instance, ref IEnumerable<Pawn> __result)
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
			bool flag = __instance is MainTabWindow_Schedule;
			List<Pawn> list = new List<Pawn>(__result);
			foreach (KeyValuePair<int, Map> item in levelComp.MapByLevel.OrderByDescending((KeyValuePair<int, Map> k) => k.Key))
			{
				Map value = item.Value;
				if (value != null && value != currentMap && !value.Disposed)
				{
					list.AddRange(value.mapPawns.FreeColonists);
					if (flag)
					{
						list.AddRange(value.mapPawns.SpawnedColonySubhumansPlayerControlled);
					}
				}
			}
			__result = list;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "pawn table augmentation");
		}
	}
}
