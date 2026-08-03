using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class ABApi
{
	public const int ApiVersion = 1;

	private static readonly HashSet<string> extraExitDuties = new HashSet<string>();

	public static event Action<Map> LevelCreated;

	public static event Action<Map> LevelRemoved;

	public static event Action<Pawn, Map, Map> PawnTransferred;

	public static int GetLevel(Map map)
	{
		return (map?.Levels()?.level).GetValueOrDefault();
	}

	public static bool IsLevelMap(Map map)
	{
		LevelComp levelComp = map?.Levels();
		return levelComp != null && levelComp.level != 0 && levelComp.groundMap != null && !levelComp.groundMap.Disposed;
	}

	public static Map GetGroundMap(Map map)
	{
		LevelComp levelComp = map?.Levels();
		if (levelComp == null)
		{
			return null;
		}
		Map val = ((levelComp.level == 0) ? map : levelComp.groundMap);
		return (val != null && !val.Disposed) ? val : null;
	}

	public static Map GetUpperMap(Map map)
	{
		Map val = map?.Levels()?.upperMap;
		return (val != null && !val.Disposed) ? val : null;
	}

	public static Map GetLowerMap(Map map)
	{
		Map val = map?.Levels()?.lowerMap;
		return (val != null && !val.Disposed) ? val : null;
	}

	public static IEnumerable<Map> GetColumnMaps(Map map)
	{
		Map ground = GetGroundMap(map) ?? map;
		if (ground != null)
		{
			yield return ground;
			Map upper = GetUpperMap(ground);
			if (upper != null)
			{
				yield return upper;
			}
			Map lower = GetLowerMap(ground);
			if (lower != null)
			{
				yield return lower;
			}
		}
	}

	public static Building GetStairsToward(Pawn pawn, Map target)
	{
		if (pawn == null || !((Thing)pawn).Spawned || target == null || target.Disposed)
		{
			return null;
		}
		return (Building)(object)CrossLevelWork.NearestUsableStairs(pawn, target, checkReachability: true);
	}

	public static Building GetStairsToward(Pawn pawn, Map target, IntVec3 dest)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (pawn == null || !((Thing)pawn).Spawned || target == null || target.Disposed)
		{
			return null;
		}
		if (((IntVec3)(ref dest)).IsValid && StairRouter.TryBestToward(pawn, target, dest, out var stairs, out var _))
		{
			return (Building)(object)stairs;
		}
		return (Building)(object)CrossLevelWork.NearestUsableStairs(pawn, target, checkReachability: true);
	}

	public static bool TrySendPawnToward(Pawn pawn, Map target, bool forced = false)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return TrySendPawnToward(pawn, target, IntVec3.Invalid, forced);
	}

	public static bool TrySendPawnToward(Pawn pawn, Map target, IntVec3 dest, bool forced = false)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (pawn == null || !((Thing)pawn).Spawned || pawn.Dead || pawn.Downed || target == null || target.Disposed || ((Thing)pawn).Map == target)
		{
			return false;
		}
		if (!CrossLevelWork.TryStairsJobToward(pawn, target, dest, out var job))
		{
			return false;
		}
		if (forced)
		{
			Pawn_JobTracker jobs = pawn.jobs;
			if (jobs != null)
			{
				jobs.StartJob(job, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
			}
		}
		else
		{
			Pawn_JobTracker jobs2 = pawn.jobs;
			if (jobs2 != null)
			{
				jobs2.TryTakeOrderedJob(job, (JobTag?)(JobTag)0, false);
			}
		}
		return true;
	}

	public static void RegisterExitDuty(string dutyDefName)
	{
		if (!GenText.NullOrEmpty(dutyDefName))
		{
			extraExitDuties.Add(dutyDefName);
		}
	}

	internal static bool IsRegisteredExitDuty(DutyDef duty)
	{
		return duty != null && extraExitDuties.Count > 0 && extraExitDuties.Contains(((Def)duty).defName);
	}

	public static void RegisterNeedJobGiver(string jobGiverFullTypeName, bool allowInMentalState = false)
	{
		NeedMigration.Register(jobGiverFullTypeName, allowInMentalState);
	}

	internal static void NotifyLevelCreated(Map map)
	{
		try
		{
			ABApi.LevelCreated?.Invoke(map);
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] A LevelCreated subscriber threw: " + ex);
		}
	}

	internal static void NotifyLevelRemoved(Map map)
	{
		try
		{
			ABApi.LevelRemoved?.Invoke(map);
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] A LevelRemoved subscriber threw: " + ex);
		}
	}

	internal static void NotifyPawnTransferred(Pawn pawn, Map from, Map to)
	{
		try
		{
			ABApi.PawnTransferred?.Invoke(pawn, from, to);
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] A PawnTransferred subscriber threw: " + ex);
		}
	}
}
