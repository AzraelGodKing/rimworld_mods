using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(CaravanFormingUtility), "AllSendablePawns")]
internal static class Patch_AllSendablePawns_Column
{
	private static void Postfix(Map map, int allowLoadAndEnterTransportersLordForGroupID, List<Pawn> __result)
	{
		if (allowLoadAndEnterTransportersLordForGroupID < 0 && CaravanAcrossLevels.Active(map, out var comp))
		{
			AppendFrom(comp.upperMap, __result);
			AppendFrom(comp.lowerMap, __result);
		}
	}

	private static void AppendFrom(Map level, List<Pawn> list)
	{
		if (level == null || level.Disposed)
		{
			return;
		}
		List<Pawn> list2 = level.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
		for (int i = 0; i < list2.Count; i++)
		{
			Pawn val = list2[i];
			if (CaravanAcrossLevels.ListableForCaravan(val) && !list.Contains(val))
			{
				list.Add(val);
			}
		}
	}
}
