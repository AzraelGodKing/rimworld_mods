using System.Collections.Generic;
using Rimefeller;
using Verse;

namespace AsAboveSoBelow;

public static class RimefellerBridge
{
	private const float Damping = 0.5f;

	private const float MinTransfer = 0.5f;

	public static void BridgePair(Building_ABStairs a, Building_ABStairs b)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		PipelineNet val = (PipelineNet)NetOf(a);
		PipelineNet val2 = (PipelineNet)NetOf(b);
		if (val != null && val2 != null && val != val2)
		{
			EqualizeOil(val, val2);
			EqualizeFuel(val, val2);
		}
	}

	private static object NetOf(Building_ABStairs link)
	{
		foreach (CompStorageTank comp in ((ThingWithComps)link).GetComps<CompStorageTank>())
		{
			PipelineNet pipeNet = ((CompPipe)comp).pipeNet;
			if (pipeNet != null)
			{
				return pipeNet;
			}
		}
		return null;
	}

	private static void EqualizeOil(object netAObj, object netBObj)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipelineNet val = (PipelineNet)netAObj;
		PipelineNet val2 = (PipelineNet)netBObj;
		SumTanks(val.OilStorage, out var stored, out var cap);
		SumTanks(val2.OilStorage, out var stored2, out var cap2);
		if (!(cap <= 0f) && !(cap2 <= 0f))
		{
			float num = (stored * cap2 - stored2 * cap) / (cap + cap2) * 0.5f;
			if (num > 0.5f)
			{
				MoveOil(val, val2, num);
			}
			else if (num < -0.5f)
			{
				MoveOil(val2, val, 0f - num);
			}
		}
	}

	private static void MoveOil(object fromObj, object toObj, float amount)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipelineNet val = (PipelineNet)fromObj;
		PipelineNet val2 = (PipelineNet)toObj;
		if (val.PullOil((double)amount))
		{
			double num = val2.PushCrude((double)amount);
			if (num > 0.001)
			{
				val.PushCrude(num);
			}
		}
	}

	private static void EqualizeFuel(object netAObj, object netBObj)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipelineNet val = (PipelineNet)netAObj;
		PipelineNet val2 = (PipelineNet)netBObj;
		SumTanks(val.FuelStorage, out var stored, out var cap);
		SumTanks(val2.FuelStorage, out var stored2, out var cap2);
		if (!(cap <= 0f) && !(cap2 <= 0f))
		{
			float num = (stored * cap2 - stored2 * cap) / (cap + cap2) * 0.5f;
			if (num > 0.5f)
			{
				MoveFuel(val, val2, num);
			}
			else if (num < -0.5f)
			{
				MoveFuel(val2, val, 0f - num);
			}
		}
	}

	private static void MoveFuel(object fromObj, object toObj, float amount)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		PipelineNet val = (PipelineNet)fromObj;
		PipelineNet val2 = (PipelineNet)toObj;
		if (val.PullFuel((double)amount))
		{
			float num = val2.PushFuel(amount);
			if (num > 0.001f)
			{
				val.PushFuel(num);
			}
		}
	}

	private static void SumTanks(object tanksObj, out float stored, out float cap)
	{
		stored = 0f;
		cap = 0f;
		List<CompStorageTank> list = (List<CompStorageTank>)tanksObj;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			CompStorageTank val = list[i];
			if (val != null && !val.DrainTank)
			{
				stored += (float)val.Storage;
				cap += (float)val.Props.StorageCap;
			}
		}
	}
}
