using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class CrossLevelHaul
{
	private struct VerdictEntry
	{
		public int tick;

		public int mapId;

		public IntVec3 cell;
	}

	private const int VerdictTtlTicks = 600;

	private static readonly Dictionary<int, VerdictEntry> verdictCache = new Dictionary<int, VerdictEntry>();

	public static Map TargetLevelFor(Pawn pawn, Thing t, out Building_ABStairs stairs)
	{
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		stairs = null;
		if (!ABGuard.On(ABGuard.Logistics) || pawn == null || t == null)
		{
			return null;
		}
		Map map = ((Thing)pawn).Map;
		LevelComp levelComp = map.Levels();
		if (levelComp == null || (levelComp.upperMap == null && levelComp.lowerMap == null))
		{
			return null;
		}
		if (!t.Spawned || t.Map != map || ForbidUtility.IsForbidden(t, pawn) || !HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, false))
		{
			return null;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (verdictCache.TryGetValue(t.thingIDNumber, out var value) && ticksGame - value.tick < 600)
		{
			if (value.mapId == -1)
			{
				return null;
			}
			Map val = FindLinked(levelComp, value.mapId);
			if (val != null)
			{
				stairs = CrossLevelWork.NearestUsableStairsCached(pawn, val);
				Building_ABStairs exit = stairs?.CounterpartTowards(val);
				if (exit == null)
				{
					return null;
				}
				StairRouter.Reroute(pawn, val, value.cell, ref stairs, ref exit);
				return val;
			}
			return null;
		}
		if (verdictCache.Count > 2048)
		{
			verdictCache.Clear();
		}
		Map val2 = null;
		IntVec3 destCell = IntVec3.Invalid;
		if (CrossLevelDemand.ExportAllowed(map, t))
		{
			StoragePriority current = StoreUtility.CurrentStoragePriorityOf(t, false);
			if (Check(pawn, t, levelComp.upperMap, current, ref stairs, ref destCell))
			{
				val2 = levelComp.upperMap;
			}
			else if (Check(pawn, t, levelComp.lowerMap, current, ref stairs, ref destCell))
			{
				val2 = levelComp.lowerMap;
			}
			if (val2 == null)
			{
				if (DemandCheck(pawn, t, levelComp.upperMap, ref stairs))
				{
					val2 = levelComp.upperMap;
				}
				else if (DemandCheck(pawn, t, levelComp.lowerMap, ref stairs))
				{
					val2 = levelComp.lowerMap;
				}
			}
		}
		verdictCache[t.thingIDNumber] = new VerdictEntry
		{
			tick = ticksGame,
			mapId = (val2?.uniqueID ?? (-1)),
			cell = destCell
		};
		return val2;
	}

	private static bool DemandCheck(Pawn pawn, Thing t, Map target, ref Building_ABStairs stairs)
	{
		if (target == null || target.Disposed || !CrossLevelDemand.Demands(target, t.def))
		{
			return false;
		}
		Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairsCached(pawn, target);
		if (building_ABStairs?.CounterpartTowards(target) == null)
		{
			return false;
		}
		stairs = building_ABStairs;
		return true;
	}

	private static Map FindLinked(LevelComp comp, int id)
	{
		if (comp.upperMap != null && !comp.upperMap.Disposed && comp.upperMap.uniqueID == id)
		{
			return comp.upperMap;
		}
		if (comp.lowerMap != null && !comp.lowerMap.Disposed && comp.lowerMap.uniqueID == id)
		{
			return comp.lowerMap;
		}
		return null;
	}

	private static bool Check(Pawn pawn, Thing t, Map target, StoragePriority current, ref Building_ABStairs stairs, ref IntVec3 destCell)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return false;
		}
		Building_ABStairs stairs2 = CrossLevelWork.NearestUsableStairsCached(pawn, target);
		Building_ABStairs exit = stairs2?.CounterpartTowards(target);
		if (exit == null)
		{
			return false;
		}
		if (!ABVirtualPosition.TrySwap(pawn, target, ((Thing)exit).Position, out var token))
		{
			return false;
		}
		IntVec3 oldPos = ABVirtualPosition.SwapPositionOnly(t, ((Thing)exit).Position);
		IntVec3 invalid = IntVec3.Invalid;
		bool flag;
		try
		{
			flag = StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, target, current, ((Thing)pawn).Faction, ref invalid, false);
		}
		finally
		{
			ABVirtualPosition.RestorePositionOnly(t, oldPos);
			ABVirtualPosition.Restore(pawn, token);
		}
		if (flag)
		{
			StairRouter.Reroute(pawn, target, invalid, ref stairs2, ref exit);
			stairs = stairs2;
			destCell = invalid;
			return true;
		}
		return false;
	}
}
