using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class StairRouter
{
	private const float CellsPerClimbTick = 1f / 13f;

	public static bool TryBestToward(Pawn pawn, Map target, IntVec3 dest, out Building_ABStairs stairs, out Building_ABStairs exit)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		stairs = null;
		exit = null;
		if (pawn == null || ((Thing)pawn).Map == null || target == null || target.Disposed)
		{
			return false;
		}
		List<Building_ABStairs> list = ((Thing)pawn).Map.Levels()?.Stairs;
		if (list == null)
		{
			return false;
		}
		bool flag = ((IntVec3)(ref dest)).IsValid && GenGrid.InBounds(dest, target);
		Building_ABStairs building_ABStairs = null;
		float num = float.MaxValue;
		Building_ABStairs building_ABStairs2 = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			Building_ABStairs building_ABStairs3 = list[i];
			if (building_ABStairs3 == null || !((Thing)building_ABStairs3).Spawned || (building_ABStairs3.Ext != null && building_ABStairs3.Ext.utilityOnly))
			{
				continue;
			}
			Building_ABStairs building_ABStairs4 = building_ABStairs3.CounterpartTowards(target);
			if (building_ABStairs4 == null)
			{
				continue;
			}
			IntVec3 val = ((Thing)building_ABStairs3).Position - ((Thing)pawn).Position;
			float num3 = ((IntVec3)(ref val)).LengthHorizontal + ClimbCost(building_ABStairs3, pawn);
			if (flag)
			{
				float num4 = num3;
				val = ((Thing)building_ABStairs4).Position - dest;
				num3 = num4 + ((IntVec3)(ref val)).LengthHorizontal;
			}
			if ((!(num3 >= num2) || !(num3 >= num)) && ReachabilityUtility.CanReach(pawn, LocalTargetInfo.op_Implicit((Thing)(object)building_ABStairs3), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
			{
				if (num3 < num2)
				{
					building_ABStairs2 = building_ABStairs3;
					num2 = num3;
				}
				if (flag && num3 < num && ExitReaches(target, building_ABStairs4, dest))
				{
					building_ABStairs = building_ABStairs3;
					num = num3;
				}
			}
		}
		stairs = building_ABStairs ?? building_ABStairs2;
		exit = stairs?.CounterpartTowards(target);
		return exit != null;
	}

	public static void Reroute(Pawn pawn, Map target, IntVec3 dest, ref Building_ABStairs stairs, ref Building_ABStairs exit)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (((IntVec3)(ref dest)).IsValid && TryBestToward(pawn, target, dest, out var stairs2, out var exit2))
		{
			stairs = stairs2;
			exit = exit2;
		}
	}

	public static IntVec3 DestHint(Job job, Map target)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (job == null || target == null)
		{
			return IntVec3.Invalid;
		}
		LocalTargetInfo targetA = job.targetA;
		if (!((LocalTargetInfo)(ref targetA)).IsValid)
		{
			return IntVec3.Invalid;
		}
		if (((LocalTargetInfo)(ref targetA)).HasThing)
		{
			Thing thing = ((LocalTargetInfo)(ref targetA)).Thing;
			return (thing != null && thing.MapHeld == target) ? thing.PositionHeld : IntVec3.Invalid;
		}
		return GenGrid.InBounds(((LocalTargetInfo)(ref targetA)).Cell, target) ? ((LocalTargetInfo)(ref targetA)).Cell : IntVec3.Invalid;
	}

	public static IntVec3 DestHint(Thing found, Map target)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		return (found != null && found.MapHeld == target) ? found.PositionHeld : IntVec3.Invalid;
	}

	private static float ClimbCost(Building_ABStairs s, Pawn pawn)
	{
		return (float)s.ClimbTicksFor(pawn) * (1f / 13f);
	}

	private static bool ExitReaches(Map target, Building_ABStairs exit, IntVec3 dest)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return target.reachability.CanReach(((Thing)exit).Position, LocalTargetInfo.op_Implicit(dest), (PathEndMode)2, TraverseParms.For((TraverseMode)1, (Danger)3, false, false, false, true, false));
	}
}
