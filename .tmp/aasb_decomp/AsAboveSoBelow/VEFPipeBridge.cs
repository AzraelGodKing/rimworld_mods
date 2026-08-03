using System.Collections.Generic;
using PipeSystem;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public static class VEFPipeBridge
{
	private const float Damping = 0.5f;

	private const float MinTransfer = 0.5f;

	private const float MinDirect = 0.01f;

	public static void BridgePair(Building_ABStairs a, Building_ABStairs b)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		PipeNetManager component = ((Thing)a).Map.GetComponent<PipeNetManager>();
		PipeNetManager component2 = ((Thing)b).Map.GetComponent<PipeNetManager>();
		if (component == null || component2 == null)
		{
			return;
		}
		List<PipeNet> pipeNets = component.pipeNets;
		List<PipeNet> pipeNets2 = component2.pipeNets;
		for (int i = 0; i < pipeNets.Count; i++)
		{
			PipeNet val = pipeNets[i];
			if (val?.networkGrid == null || !Touches(val, ((Thing)a).Position, ((Thing)a).Map))
			{
				continue;
			}
			PipeNet val2 = null;
			for (int j = 0; j < pipeNets2.Count; j++)
			{
				PipeNet val3 = pipeNets2[j];
				if (val3?.networkGrid != null && val3.def == val.def && Touches(val3, ((Thing)b).Position, ((Thing)b).Map))
				{
					val2 = val3;
					break;
				}
			}
			if (val2 != null && val2 != val)
			{
				DirectFeed(val, val2);
				DirectFeed(val2, val);
				EqualizeStorages(val, val2);
			}
		}
	}

	private static bool Touches(object netObj, IntVec3 c, Map map)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		PipeNet val = (PipeNet)netObj;
		if (val.networkGrid[c])
		{
			return true;
		}
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		for (int i = 0; i < cardinalDirections.Length; i++)
		{
			IntVec3 val2 = c + cardinalDirections[i];
			if (GenGrid.InBounds(val2, map) && val.networkGrid[val2])
			{
				return true;
			}
		}
		return false;
	}

	private static void DirectFeed(object fromObj, object toObj)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipeNet val = (PipeNet)fromObj;
		PipeNet val2 = (PipeNet)toObj;
		float num = 0f;
		List<CompResourceTrader> receivers = val2.receivers;
		for (int i = 0; i < receivers.Count; i++)
		{
			CompResourceTrader val3 = receivers[i];
			if (!val3.ResourceOn && val3.CanBeOn())
			{
				num += val3.Consumption;
			}
		}
		float num2 = val2.Consumption + num - val2.Production - val2.ExtraProductionThisTick;
		if (!(num2 <= 0.01f))
		{
			float num3 = Mathf.Max(0f, val.Production - val.Consumption - val.ExtraConsumptionThisTick) + val.Stored;
			float num4 = Mathf.Min(num2, num3);
			if (!(num4 <= 0.01f))
			{
				val.ExtraConsumptionThisTick += num4;
				val2.ExtraProductionThisTick += num4;
			}
		}
	}

	private static void EqualizeStorages(object netAObj, object netBObj)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipeNet val = (PipeNet)netAObj;
		PipeNet val2 = (PipeNet)netBObj;
		float num = val.CurrentStored();
		float num2 = val2.CurrentStored();
		float num3 = num + val.AvailableCapacity;
		float num4 = num2 + val2.AvailableCapacity;
		if (num3 <= 0f || num4 <= 0f)
		{
			return;
		}
		float num5 = (num * num4 - num2 * num3) / (num3 + num4) * 0.5f;
		PipeNet val3;
		PipeNet val4;
		float num6;
		if (num5 > 0.5f)
		{
			val3 = val;
			val4 = val2;
			num6 = num5;
		}
		else
		{
			if (!(num5 < -0.5f))
			{
				return;
			}
			val3 = val2;
			val4 = val;
			num6 = 0f - num5;
		}
		float num7 = default(float);
		val3.DrawAmongStorage(num6, ref num7, (List<CompResourceStorage>)null, false);
		if (!(num7 <= 0f))
		{
			float num8 = default(float);
			val4.DistributeAmongStorage(num7, ref num8, (List<CompResourceStorage>)null, false);
			float num9 = num7 - num8;
			if (num9 > 0.001f)
			{
				float num10 = default(float);
				val3.DistributeAmongStorage(num9, ref num10, (List<CompResourceStorage>)null, false);
			}
		}
	}
}
