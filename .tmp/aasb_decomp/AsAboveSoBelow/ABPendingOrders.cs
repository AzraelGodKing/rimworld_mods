using System;
using System.Collections.Generic;
using Verse;

namespace AsAboveSoBelow;

internal static class ABPendingOrders
{
	private struct Entry
	{
		public int tick;

		public Map map;

		public Action action;
	}

	private const int ExpiryTicks = 5000;

	private static readonly Dictionary<int, Entry> pending = new Dictionary<int, Entry>();

	public static void Set(Pawn pawn, Map targetMap, Action action)
	{
		if (pawn != null && targetMap != null && action != null)
		{
			if (pending.Count > 64)
			{
				pending.Clear();
			}
			pending[((Thing)pawn).thingIDNumber] = new Entry
			{
				tick = Find.TickManager.TicksGame,
				map = targetMap,
				action = action
			};
		}
	}

	public static void TryRun(Pawn pawn, Map arrivedOn)
	{
		if (pawn == null || !pending.TryGetValue(((Thing)pawn).thingIDNumber, out var value))
		{
			return;
		}
		if (Find.TickManager.TicksGame - value.tick > 5000)
		{
			pending.Remove(((Thing)pawn).thingIDNumber);
		}
		else if (value.map == arrivedOn)
		{
			pending.Remove(((Thing)pawn).thingIDNumber);
			try
			{
				value.action();
			}
			catch (Exception e)
			{
				ABGuard.Disable(ABGuard.Movement, e, "pending order replay");
			}
		}
	}
}
