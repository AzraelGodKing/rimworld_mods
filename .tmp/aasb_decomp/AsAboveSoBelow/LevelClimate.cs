using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public static class LevelClimate
{
	private const float ExchangeRatePerDegree = 25f;

	private const float MaxExchange = 800f;

	public static void TickGroundPairs(LevelComp groundComp)
	{
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelTemperature)
		{
			return;
		}
		List<Building_ABStairs> stairs = groundComp.Stairs;
		for (int i = 0; i < stairs.Count; i++)
		{
			Building_ABStairs building_ABStairs = stairs[i];
			if (building_ABStairs != null && ((Thing)building_ABStairs).Spawned && (building_ABStairs.Ext == null || !building_ABStairs.Ext.utilityOnly))
			{
				ExchangeLink(building_ABStairs, building_ABStairs.Counterpart);
				ExchangeLink(building_ABStairs, building_ABStairs.SecondCounterpart);
			}
		}
	}

	private static void ExchangeLink(Building_ABStairs a, Building_ABStairs b)
	{
		if (b != null && ((Thing)b).Spawned && ((Thing)b).Map != null && !((Thing)b).Map.Disposed)
		{
			ExchangePair(a, b);
		}
	}

	private static void ExchangePair(Building_ABStairs a, Building_ABStairs b)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		float temperatureForCell = GenTemperature.GetTemperatureForCell(((Thing)a).Position, ((Thing)a).Map);
		float temperatureForCell2 = GenTemperature.GetTemperatureForCell(((Thing)b).Position, ((Thing)b).Map);
		float num = temperatureForCell - temperatureForCell2;
		if (!(Mathf.Abs(num) < 0.5f))
		{
			float num2 = Mathf.Clamp(num * 25f, -800f, 800f);
			GenTemperature.PushHeat(((Thing)b).Position, ((Thing)b).Map, num2);
			GenTemperature.PushHeat(((Thing)a).Position, ((Thing)a).Map, 0f - num2);
		}
	}
}
