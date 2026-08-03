using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(TradeUtility), "LaunchThingsOfType")]
internal static class Patch_LaunchThingsOfType_Column
{
	private static bool inColumnLaunch;

	private static bool Prefix(ThingDef resDef, int debt, Map map, TradeShip trader)
	{
		if (inColumnLaunch || !ColumnTrade.Active(map, out var comp))
		{
			return true;
		}
		try
		{
			inColumnLaunch = true;
			Map[] array = (Map[])(object)new Map[3] { map, comp.upperMap, comp.lowerMap };
			foreach (Map val in array)
			{
				if (val != null && !val.Disposed && debt > 0)
				{
					int num = CountAtBeacons(val, resDef);
					if (num > 0)
					{
						int num2 = Math.Min(debt, num);
						TradeUtility.LaunchThingsOfType(resDef, num2, val, trader);
						debt -= num2;
					}
				}
			}
			if (debt > 0)
			{
				Log.Error("Could not find any " + ((object)resDef)?.ToString() + " to transfer to trader (column-wide).");
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.World, e, "column trade launch");
			return true;
		}
		finally
		{
			inColumnLaunch = false;
		}
		return false;
	}

	private static int CountAtBeacons(Map map, ThingDef resDef)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (Building_OrbitalTradeBeacon item in Building_OrbitalTradeBeacon.AllPowered(map))
		{
			foreach (IntVec3 tradeableCell in item.TradeableCells)
			{
				List<Thing> list = map.thingGrid.ThingsListAtFast(tradeableCell);
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].def == resDef)
					{
						num += list[i].stackCount;
					}
				}
			}
		}
		return num;
	}
}
