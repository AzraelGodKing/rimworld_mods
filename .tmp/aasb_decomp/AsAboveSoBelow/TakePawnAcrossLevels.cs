using RimWorld;
using Verse;

namespace AsAboveSoBelow;

internal static class TakePawnAcrossLevels
{
	internal static Building_ABStairs FindStairsTowardBed(Pawn taker, Pawn victim, GuestStatus? guest, out Building_ABStairs exit)
	{
		exit = null;
		LevelComp levelComp = ((Thing)taker).Map.Levels();
		if (levelComp == null || (levelComp.upperMap == null && levelComp.lowerMap == null))
		{
			return null;
		}
		return Toward(taker, victim, levelComp.upperMap, guest, ref exit) ?? Toward(taker, victim, levelComp.lowerMap, guest, ref exit);
	}

	private static Building_ABStairs Toward(Pawn taker, Pawn victim, Map target, GuestStatus? guest, ref Building_ABStairs exitOut)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return null;
		}
		Building_ABStairs stairs = CrossLevelWork.NearestUsableStairs(taker, target, checkReachability: true);
		Building_ABStairs exit = stairs?.CounterpartTowards(target);
		if (exit == null)
		{
			return null;
		}
		if (!ABVirtualPosition.TrySwap(taker, target, ((Thing)exit).Position, out var token))
		{
			return null;
		}
		Building_Bed val = null;
		try
		{
			if (ABVirtualPosition.TrySwap(victim, target, ((Thing)exit).Position, out var token2))
			{
				try
				{
					val = RestUtility.FindBedFor(victim, taker, false, false, guest);
				}
				finally
				{
					ABVirtualPosition.Restore(victim, token2);
				}
			}
		}
		finally
		{
			ABVirtualPosition.Restore(taker, token);
		}
		if (val == null)
		{
			return null;
		}
		StairRouter.Reroute(taker, target, StairRouter.DestHint((Thing)(object)val, target), ref stairs, ref exit);
		exitOut = exit;
		return stairs;
	}
}
