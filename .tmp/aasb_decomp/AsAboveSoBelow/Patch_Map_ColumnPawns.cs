using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_Map_ColumnPawns
{
	private static void Postfix(Map __instance, ref IEnumerable<Pawn> __result)
	{
		if (!ABGuard.On(ABGuard.World))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings != null && settings.columnWealth)
		{
			LevelComp levelComp = __instance.Levels();
			if (levelComp != null && levelComp.level == 0 && (levelComp.upperMap != null || levelComp.lowerMap != null))
			{
				__result = WithColumn(__result, levelComp.upperMap, levelComp.lowerMap);
			}
		}
	}

	private static IEnumerable<Pawn> WithColumn(IEnumerable<Pawn> surface, Map upper, Map lower)
	{
		foreach (Pawn item in surface)
		{
			yield return item;
		}
		if (upper != null && !upper.Disposed)
		{
			foreach (Pawn item2 in upper.mapPawns.PawnsInFaction(Faction.OfPlayer))
			{
				yield return item2;
			}
		}
		if (lower == null || lower.Disposed)
		{
			yield break;
		}
		foreach (Pawn item3 in lower.mapPawns.PawnsInFaction(Faction.OfPlayer))
		{
			yield return item3;
		}
	}
}
