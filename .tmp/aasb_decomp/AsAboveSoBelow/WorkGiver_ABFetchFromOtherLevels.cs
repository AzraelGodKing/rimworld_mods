using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

public class WorkGiver_ABFetchFromOtherLevels : WorkGiver
{
	private const int EmptyScanCooldownTicks = 450;

	private const int MaxItemsPerScan = 25;

	private static readonly ABPawnCooldown emptyScanCooldown = new ABPawnCooldown();

	public override Job NonScanJob(Pawn pawn)
	{
		if (!ABGuard.On(ABGuard.Logistics) || CrossLevelWork.VirtualScanActive)
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelHauling)
		{
			return null;
		}
		if (pawn != null && ((Thing)pawn).Spawned && !pawn.Drafted && LordUtility.GetLord(pawn) == null)
		{
			Pawn_CarryTracker carryTracker = pawn.carryTracker;
			if (((carryTracker != null) ? carryTracker.CarriedThing : null) == null)
			{
				if (CrossLevelWork.LowPowerWorker(pawn))
				{
					return null;
				}
				if (!((Thing)pawn).Map.TryLinkedLevels(out var comp))
				{
					return null;
				}
				int ticksGame = Find.TickManager.TicksGame;
				if (!emptyScanCooldown.Ready(pawn, ticksGame))
				{
					return null;
				}
				try
				{
					Job val = TryFetchTowards(pawn, comp.upperMap) ?? TryFetchTowards(pawn, comp.lowerMap);
					if (val != null)
					{
						return val;
					}
				}
				catch (Exception e)
				{
					ABGuard.Disable(ABGuard.Logistics, e, "cross level fetch");
					return null;
				}
				emptyScanCooldown.ChargeUntil(pawn, ticksGame + 450);
				return null;
			}
		}
		return null;
	}

	private static Job TryFetchTowards(Pawn pawn, Map target)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return null;
		}
		Map demandMap = ((Thing)pawn).Map;
		ICollection<Thing> collection = target.listerHaulables.ThingsPotentiallyNeedingHauling();
		bool flag = collection.Count > 0;
		bool flag2 = ABMod.Settings.crossLevelSupply && CrossLevelDemand.HasFetchableDemand(demandMap, target, pawn);
		if (!flag && !flag2)
		{
			return null;
		}
		if (!CrossLevelWork.TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			return null;
		}
		if (!ABVirtualPosition.TrySwap(pawn, target, ((Thing)exit).Position, out var token))
		{
			return null;
		}
		bool flag3 = false;
		bool flag4 = false;
		IntVec3 dest = IntVec3.Invalid;
		CrossLevelWork.VirtualScanActive = true;
		try
		{
			if (flag)
			{
				int num = 0;
				IntVec3 val = default(IntVec3);
				IHaulDestination val2 = default(IHaulDestination);
				foreach (Thing item in collection)
				{
					if (++num > 25)
					{
						break;
					}
					if (item != null && item.Spawned && item.Map == target && !ForbidUtility.IsForbidden(item, pawn) && HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, item, false))
					{
						if (StoreUtility.TryFindBestBetterStorageFor(item, pawn, target, StoreUtility.CurrentStoragePriorityOf(item, false), ((Thing)pawn).Faction, ref val, ref val2, false))
						{
							flag3 = true;
							dest = item.PositionHeld;
							break;
						}
						if (CrossLevelHaul.TargetLevelFor(pawn, item, out var _) != null)
						{
							flag3 = true;
							dest = item.PositionHeld;
							break;
						}
					}
				}
			}
			if (!flag3 && flag2)
			{
				Thing val3 = CrossLevelDemand.FindFetchableDemand(demandMap, target, pawn, requireReachable: true);
				flag4 = val3 != null;
				flag3 = flag4;
				if (val3 != null)
				{
					dest = StairRouter.DestHint(val3, target);
				}
			}
		}
		finally
		{
			ABVirtualPosition.Restore(pawn, token);
			CrossLevelWork.VirtualScanActive = false;
		}
		if (!flag3)
		{
			return null;
		}
		StairRouter.Reroute(pawn, target, dest, ref stairs, ref exit);
		if (flag4)
		{
			ABPendingOrders.Set(pawn, target, delegate
			{
				IssueDemandHaulBack(pawn, target, demandMap);
			});
		}
		return CrossLevelWork.MakeStairsJob(stairs, exit);
	}

	private static void IssueDemandHaulBack(Pawn pawn, Map sourceMap, Map demandMap)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (pawn == null || !((Thing)pawn).Spawned || pawn.Dead || ((Thing)pawn).Map != sourceMap || pawn.Drafted || LordUtility.GetLord(pawn) != null)
			{
				return;
			}
			Pawn_CarryTracker carryTracker = pawn.carryTracker;
			if (((carryTracker != null) ? carryTracker.CarriedThing : null) != null)
			{
				return;
			}
			Thing val = CrossLevelDemand.FindFetchableDemand(demandMap, sourceMap, pawn, requireReachable: true);
			if (val != null && CrossLevelWork.TryResolveStairs(pawn, demandMap, out var stairs, out var exit))
			{
				Job val2 = JobMaker.MakeJob(ABDefOf.AB_HaulAcrossLevels, LocalTargetInfo.op_Implicit(val), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
				val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
				val2.count = Mathf.Min(val.stackCount, pawn.carryTracker.MaxStackSpaceEver(val.def));
				Pawn_JobTracker jobs = pawn.jobs;
				if (jobs != null)
				{
					jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)0, false);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "demand haul back");
		}
	}
}
