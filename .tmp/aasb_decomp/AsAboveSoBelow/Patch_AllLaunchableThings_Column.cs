using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(TradeUtility), "AllLaunchableThingsForTrade")]
internal static class Patch_AllLaunchableThings_Column
{
	private static void Postfix(Map map, ITrader trader, ref IEnumerable<Thing> __result)
	{
		if (ColumnTrade.Active(map, out var comp))
		{
			IEnumerable<Thing> enumerable = __result;
			if (comp.upperMap != null && !comp.upperMap.Disposed)
			{
				enumerable = enumerable.Concat(TradeUtility.AllLaunchableThingsForTrade(comp.upperMap, trader));
			}
			if (comp.lowerMap != null && !comp.lowerMap.Disposed)
			{
				enumerable = enumerable.Concat(TradeUtility.AllLaunchableThingsForTrade(comp.lowerMap, trader));
			}
			__result = enumerable;
		}
	}
}
