using System.Collections.Generic;
using DubsBadHygiene;
using Verse;

namespace AsAboveSoBelow;

public static class DBHWaterBridge
{
	private const float Damping = 0.5f;

	private const float MinTransfer = 0.05f;

	public static void BridgePair(Building_ABStairs a, Building_ABStairs b)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		HygienePipeMapComp component = ((Thing)a).Map.GetComponent<HygienePipeMapComp>();
		HygienePipeMapComp component2 = ((Thing)b).Map.GetComponent<HygienePipeMapComp>();
		if (component == null || component2 == null)
		{
			return;
		}
		PlumbingNet[] pipeNets = component.PipeNets;
		PlumbingNet[] pipeNets2 = component2.PipeNets;
		ContaminationLevel val5 = default(ContaminationLevel);
		foreach (PlumbingNet val in pipeNets)
		{
			if (!val.cells.Contains(((Thing)a).Position))
			{
				continue;
			}
			PlumbingNet val2 = null;
			for (int j = 0; j < pipeNets2.Length; j++)
			{
				if (pipeNets2[j].NetType == val.NetType && pipeNets2[j].cells.Contains(((Thing)b).Position))
				{
					val2 = pipeNets2[j];
					break;
				}
			}
			if (val2 == null || val2 == val)
			{
				continue;
			}
			float num = 0f;
			List<CompWaterStorage> waterTowers = val.WaterTowers;
			for (int k = 0; k < waterTowers.Count; k++)
			{
				num += waterTowers[k].WaterStorage + waterTowers[k].space;
			}
			float num2 = 0f;
			List<CompWaterStorage> waterTowers2 = val2.WaterTowers;
			for (int l = 0; l < waterTowers2.Count; l++)
			{
				num2 += waterTowers2[l].WaterStorage + waterTowers2[l].space;
			}
			if (num <= 0f || num2 <= 0f)
			{
				continue;
			}
			float num3 = (val.WaterStorage * num2 - val2.WaterStorage * num) / (num + num2) * 0.5f;
			PlumbingNet val3;
			PlumbingNet val4;
			float num4;
			if (num3 > 0.05f)
			{
				val3 = val;
				val4 = val2;
				num4 = num3;
			}
			else
			{
				if (!(num3 < -0.05f))
				{
					continue;
				}
				val3 = val2;
				val4 = val;
				num4 = 0f - num3;
			}
			if (val3.PullWater(num4, ref val5))
			{
				float num5 = val4.PushWater(num4);
				if (num5 > 0f)
				{
					val3.PushWater(num5);
				}
			}
		}
	}
}
