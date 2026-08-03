using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public static class CrossLevelWork
{
	private sealed class VersionedCooldown
	{
		private struct Entry
		{
			public int until;

			public int version;
		}

		private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();

		public bool Ready(Pawn pawn, int now)
		{
			if (!entries.TryGetValue(((Thing)pawn).thingIDNumber, out var value))
			{
				return true;
			}
			return now >= value.until || value.version != LevelWorkSummary.WorkVersion;
		}

		public void Charge(Pawn pawn, int untilTick)
		{
			if (entries.Count > 512)
			{
				entries.Clear();
			}
			entries[((Thing)pawn).thingIDNumber] = new Entry
			{
				until = untilTick,
				version = LevelWorkSummary.WorkVersion
			};
		}
	}

	private struct StairsMemoEntry
	{
		public int tick;

		public Building_ABStairs stairs;
	}

	private const int MigrationCooldownTicks = 1200;

	private const int BetterWorkCooldownTicks = 900;

	private const int MaxProbesPerTick = 2;

	private const int MaxCellsPerGiverProbe = 300;

	private const int EmergencyMigrationCooldownTicks = 600;

	internal static bool VirtualScanActive;

	private static readonly ABPawnCooldown migrationCooldown = new ABPawnCooldown();

	private static readonly ABPawnCooldown emergencyCooldown = new ABPawnCooldown();

	private static readonly VersionedCooldown betterWorkCooldown = new VersionedCooldown();

	private static int probeBudgetTick = -1;

	private static int probeBudgetUsed;

	private static readonly Dictionary<long, StairsMemoEntry> stairsMemo = new Dictionary<long, StairsMemoEntry>();

	private static bool TryClaimProbeBudget(int now)
	{
		if (now != probeBudgetTick)
		{
			probeBudgetTick = now;
			probeBudgetUsed = 0;
		}
		if (probeBudgetUsed >= 2)
		{
			return false;
		}
		probeBudgetUsed++;
		return true;
	}

	public static bool LowPowerWorker(Pawn pawn)
	{
		if (pawn.RaceProps == null || pawn.RaceProps.Humanlike || pawn.needs == null)
		{
			return false;
		}
		Need_Rest rest = pawn.needs.rest;
		if (rest != null && ((Need)rest).CurLevelPercentage < 0.45f)
		{
			return true;
		}
		Need_MechEnergy energy = pawn.needs.energy;
		return energy != null && ((Need)energy).CurLevelPercentage < 0.35f;
	}

	public static ThinkResult? TryMigrateForWork(JobGiver_Work giver, Pawn pawn)
	{
		if (!((Thing)pawn).Map.TryLinkedLevels(out var comp))
		{
			return null;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (!migrationCooldown.Ready(pawn, ticksGame))
		{
			return null;
		}
		migrationCooldown.ChargeUntil(pawn, ticksGame + 1200);
		return (TryTowards(giver, pawn, comp.upperMap) ?? TryTowards(giver, pawn, comp.lowerMap)) ?? TryReturnHome(giver, pawn, comp);
	}

	public static ThinkResult? TryMigrateForBetterWork(JobGiver_Work giver, Pawn pawn, ThinkResult local)
	{
		Job job = ((ThinkResult)(ref local)).Job;
		if (job == null)
		{
			return null;
		}
		WorkGiverDef workGiverDef = job.workGiverDef;
		if (workGiverDef == null)
		{
			return null;
		}
		Pawn_WorkSettings workSettings = pawn.workSettings;
		List<WorkGiver> list = ((workSettings != null) ? workSettings.WorkGiversInOrderNormal : null);
		if (list == null || list.Count == 0)
		{
			return null;
		}
		int num = -1;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].def == workGiverDef)
			{
				num = i;
				break;
			}
		}
		if (num <= 0)
		{
			return null;
		}
		if (!((Thing)pawn).Map.TryLinkedLevels(out var comp))
		{
			return null;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (!betterWorkCooldown.Ready(pawn, ticksGame))
		{
			return null;
		}
		if (!TryClaimProbeBudget(ticksGame))
		{
			return null;
		}
		betterWorkCooldown.Charge(pawn, ticksGame + 900);
		return TryTowardsBetter(giver, pawn, comp.upperMap, list, num) ?? TryTowardsBetter(giver, pawn, comp.lowerMap, list, num);
	}

	private static ThinkResult? TryTowardsBetter(JobGiver_Work giver, Pawn pawn, Map target, List<WorkGiver> order, int stop)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return null;
		}
		if (!LevelWorkSummary.AnyPlausibleBefore(target, order, stop))
		{
			return null;
		}
		if (!TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			return null;
		}
		if (!ProbeBetterWorkAt(pawn, target, ((Thing)exit).Position, order, stop, out var workDest))
		{
			return null;
		}
		StairRouter.Reroute(pawn, target, workDest, ref stairs, ref exit);
		migrationCooldown.ChargeUntil(pawn, Find.TickManager.TicksGame + 1200);
		ABLog.Dev("Migrate " + ((Entity)pawn).LabelShort + " -> level " + target.Level() + ": higher-priority work found, taking stairs.");
		return new ThinkResult(MakeStairsJob(stairs, exit), (ThinkNode)(object)giver, (JobTag?)(JobTag)0, false);
	}

	private static bool ProbeBetterWorkAt(Pawn pawn, Map target, IntVec3 entryCell, List<WorkGiver> order, int stop, out IntVec3 workDest)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		workDest = IntVec3.Invalid;
		if (!ABVirtualPosition.TrySwap(pawn, target, entryCell, out var token))
		{
			return false;
		}
		bool flag = false;
		VirtualScanActive = true;
		try
		{
			for (int i = 0; i < stop; i++)
			{
				if (flag)
				{
					break;
				}
				WorkGiver val = order[i];
				WorkGiverDef def = val.def;
				if (def?.workType != null && !LevelWorkSummary.IsOwnCrossLevelGiver(def) && LevelWorkSummary.Plausible(target, def.workType) && val.MissingRequiredCapacity(pawn) == null && !val.ShouldSkip(pawn, false))
				{
					Job val2 = val.NonScanJob(pawn);
					if (val2 != null)
					{
						workDest = ((((LocalTargetInfo)(ref val2.targetA)).IsValid && ((LocalTargetInfo)(ref val2.targetA)).HasThing) ? ((LocalTargetInfo)(ref val2.targetA)).Thing.PositionHeld : (((LocalTargetInfo)(ref val2.targetA)).IsValid ? ((LocalTargetInfo)(ref val2.targetA)).Cell : IntVec3.Invalid));
						flag = true;
						break;
					}
					WorkGiver_Scanner val3 = (WorkGiver_Scanner)(object)((val is WorkGiver_Scanner) ? val : null);
					if (val3 != null)
					{
						flag = ProbeGiver(val3, pawn, out workDest);
					}
				}
			}
		}
		finally
		{
			ABVirtualPosition.Restore(pawn, token);
			VirtualScanActive = false;
		}
		return flag;
	}

	private static bool ProbeGiver(WorkGiver_Scanner scanner, Pawn pawn, out IntVec3 workDest)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		workDest = IntVec3.Invalid;
		if (((WorkGiver)scanner).def.scanThings)
		{
			IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
			Thing val;
			if (scanner.AllowUnreachable)
			{
				IEnumerable<Thing> enumerable2 = enumerable ?? ((Thing)pawn).Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
				val = GenClosest.ClosestThing_Global(((Thing)pawn).Position, (IEnumerable)enumerable2, 99999f, (Predicate<Thing>)Validator, (Func<Thing, float>)null, false);
			}
			else
			{
				val = GenClosest.ClosestThingReachable(((Thing)pawn).Position, ((Thing)pawn).Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), (TraverseMode)0, false, false, false, true), 9999f, (Predicate<Thing>)Validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null, (RegionType)14, false, false);
			}
			if (val != null)
			{
				workDest = val.PositionHeld;
				return true;
			}
		}
		if (((WorkGiver)scanner).def.scanCells)
		{
			int num = 0;
			Danger val2 = scanner.MaxPathDanger(pawn);
			foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
			{
				if (++num > 300)
				{
					break;
				}
				if (ForbidUtility.IsForbidden(item, pawn) || !scanner.HasJobOnCell(pawn, item, false) || (!scanner.AllowUnreachable && !ReachabilityUtility.CanReach(pawn, LocalTargetInfo.op_Implicit(item), scanner.PathEndMode, val2, false, false, (TraverseMode)0)))
				{
					continue;
				}
				workDest = item;
				return true;
			}
		}
		return false;
		bool Validator(Thing th)
		{
			return !ForbidUtility.IsForbidden(th, pawn) && scanner.HasJobOnThing(pawn, th, false);
		}
	}

	public static ThinkResult? TryMigrateForEmergencyWork(JobGiver_Work giver, Pawn pawn)
	{
		if (!((Thing)pawn).Map.TryLinkedLevels(out var comp))
		{
			return null;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (!emergencyCooldown.Ready(pawn, ticksGame))
		{
			return null;
		}
		bool flag = EmergencyWorkPlausible(pawn, comp.upperMap);
		bool flag2 = EmergencyWorkPlausible(pawn, comp.lowerMap);
		if (!flag && !flag2)
		{
			return null;
		}
		emergencyCooldown.ChargeUntil(pawn, ticksGame + 600);
		ThinkResult? result = (flag ? TryTowards(giver, pawn, comp.upperMap) : ((ThinkResult?)null));
		if (!result.HasValue && flag2)
		{
			result = TryTowards(giver, pawn, comp.lowerMap);
		}
		return result;
	}

	private static bool EmergencyWorkPlausible(Pawn pawn, Map target)
	{
		if (target == null || target.Disposed)
		{
			return false;
		}
		if (target.listerThings.ThingsOfDef(ThingDefOf.Fire).Count > 0)
		{
			return true;
		}
		if (AnyEmergencyPatient(target.mapPawns.SpawnedPawnsInFaction(((Thing)pawn).Faction)))
		{
			return true;
		}
		return AnyEmergencyPatient(target.mapPawns.PrisonersOfColonySpawned);
	}

	private static bool AnyEmergencyPatient(List<Pawn> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			Pawn val = list[i];
			if (val.Downed && !RestUtility.InBed(val))
			{
				return true;
			}
			if (val.health != null && val.health.HasHediffsNeedingTendByPlayer(false))
			{
				return true;
			}
		}
		return false;
	}

	private static ThinkResult? TryReturnHome(JobGiver_Work giver, Pawn pawn, LevelComp comp)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.idleReturnHome || comp.level == 0)
		{
			return null;
		}
		Map target = ((comp.level > 0) ? comp.lowerMap : comp.upperMap);
		if (!TryStairsJobToward(pawn, target, out var job))
		{
			return null;
		}
		return new ThinkResult(job, (ThinkNode)(object)giver, (JobTag?)(JobTag)0, false);
	}

	private static ThinkResult? TryTowards(JobGiver_Work giver, Pawn pawn, Map target)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return null;
		}
		if (!TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			ABLog.Dev("Migrate " + ((Entity)pawn).LabelShort + " -> level " + target.Level() + ": no reachable linked stairs on this level.");
			return null;
		}
		if (!WorkTargetAt(giver, pawn, target, ((Thing)exit).Position, out var workDest))
		{
			ABLog.Dev("Migrate " + ((Entity)pawn).LabelShort + " -> level " + target.Level() + ": no work reachable from the stairwell exit at " + ((object)((Thing)exit).Position/*cast due to .constrained prefix*/).ToString() + " (is the work area connected to that exit?).");
			return null;
		}
		StairRouter.Reroute(pawn, target, workDest, ref stairs, ref exit);
		ABLog.Dev("Migrate " + ((Entity)pawn).LabelShort + " -> level " + target.Level() + ": work found, taking stairs.");
		return new ThinkResult(MakeStairsJob(stairs, exit), (ThinkNode)(object)giver, (JobTag?)(JobTag)0, false);
	}

	public static bool TryResolveStairs(Pawn pawn, Map target, out Building_ABStairs stairs, out Building_ABStairs exit)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return TryResolveStairs(pawn, target, IntVec3.Invalid, out stairs, out exit);
	}

	public static bool TryResolveStairs(Pawn pawn, Map target, IntVec3 dest, out Building_ABStairs stairs, out Building_ABStairs exit)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		stairs = null;
		exit = null;
		if (target == null || target.Disposed)
		{
			return false;
		}
		if (((IntVec3)(ref dest)).IsValid && StairRouter.TryBestToward(pawn, target, dest, out stairs, out exit))
		{
			return true;
		}
		stairs = NearestUsableStairs(pawn, target, checkReachability: true);
		exit = stairs?.CounterpartTowards(target);
		return exit != null;
	}

	public static Job MakeStairsJob(Building_ABStairs stairs, Building_ABStairs exit)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Job val = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)stairs));
		val.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
		return val;
	}

	public static bool TryStairsJobToward(Pawn pawn, Map target, out Job job)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return TryStairsJobToward(pawn, target, IntVec3.Invalid, out job);
	}

	public static bool TryStairsJobToward(Pawn pawn, Map target, IntVec3 dest, out Job job)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		job = null;
		if (!TryResolveStairs(pawn, target, dest, out var stairs, out var exit))
		{
			return false;
		}
		job = MakeStairsJob(stairs, exit);
		return true;
	}

	public static Building_ABStairs NearestUsableStairsCached(Pawn pawn, Map target)
	{
		if (pawn == null || target == null)
		{
			return null;
		}
		long key = ((long)((Thing)pawn).thingIDNumber << 32) | (uint)target.uniqueID;
		int ticksGame = Find.TickManager.TicksGame;
		if (stairsMemo.TryGetValue(key, out var value) && value.tick == ticksGame)
		{
			Building_ABStairs stairs = value.stairs;
			return (stairs != null && ((Thing)stairs).Spawned && stairs.CounterpartTowards(target) != null) ? stairs : null;
		}
		if (stairsMemo.Count > 1024)
		{
			stairsMemo.Clear();
		}
		Building_ABStairs building_ABStairs = NearestUsableStairs(pawn, target, checkReachability: true);
		stairsMemo[key] = new StairsMemoEntry
		{
			tick = ticksGame,
			stairs = building_ABStairs
		};
		return building_ABStairs;
	}

	public static Building_ABStairs NearestUsableStairs(Pawn pawn, Map target, bool checkReachability)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		List<Building_ABStairs> list = ((Thing)pawn).Map.Levels()?.Stairs;
		if (list == null)
		{
			return null;
		}
		Building_ABStairs result = null;
		float num = float.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			Building_ABStairs building_ABStairs = list[i];
			if (building_ABStairs == null || !((Thing)building_ABStairs).Spawned || (building_ABStairs.Ext != null && building_ABStairs.Ext.utilityOnly))
			{
				continue;
			}
			Building_ABStairs building_ABStairs2 = building_ABStairs.CounterpartTowards(target);
			if (building_ABStairs2 != null)
			{
				IntVec3 val = ((Thing)building_ABStairs).Position - ((Thing)pawn).Position;
				float num2 = ((IntVec3)(ref val)).LengthHorizontalSquared;
				if (!(num2 >= num) && (!checkReachability || ReachabilityUtility.CanReach(pawn, LocalTargetInfo.op_Implicit((Thing)(object)building_ABStairs), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0)))
				{
					result = building_ABStairs;
					num = num2;
				}
			}
		}
		return result;
	}

	private static bool WorkTargetAt(JobGiver_Work giver, Pawn pawn, Map target, IntVec3 entryCell, out IntVec3 workDest)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		workDest = IntVec3.Invalid;
		if (!ABVirtualPosition.TrySwap(pawn, target, entryCell, out var token))
		{
			return false;
		}
		bool flag = false;
		VirtualScanActive = true;
		try
		{
			ThinkResult val = ((ThinkNode)giver).TryIssueJobPackage(pawn, default(JobIssueParams));
			flag = ((ThinkResult)(ref val)).Job != null;
			if (flag)
			{
				workDest = StairRouter.DestHint(((ThinkResult)(ref val)).Job, target);
			}
		}
		finally
		{
			ABVirtualPosition.Restore(pawn, token);
			VirtualScanActive = false;
		}
		return flag;
	}
}
