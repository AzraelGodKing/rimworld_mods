using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

internal static class StairTransfer
{
	private const float FollowPullRadius = 12f;

	public static Building_ABStairs ResolveNextDest(Building_ABStairs entry, Building_ABStairs final)
	{
		if (entry == null)
		{
			return null;
		}
		if (final != null && !((Thing)final).Destroyed && ((Thing)final).Spawned && ((Thing)final).Map != ((Thing)entry).Map)
		{
			Map map = ((Thing)entry).Map;
			LevelComp levelComp = map.Levels();
			int num = Math.Sign(((Thing)final).Map.Level() - map.Level());
			Map val = ((num <= 0) ? levelComp?.lowerMap : levelComp?.upperMap);
			return (val != null) ? entry.CounterpartTowards(val) : null;
		}
		return entry.Counterpart ?? entry.SecondCounterpart;
	}

	public static void Transfer(Pawn p, Building_ABStairs stairs, CarriedIntent intent = CarriedIntent.Auto, Building_ABStairs explicitDest = null, Building_ABStairs rideFinal = null)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		Building_ABStairs building_ABStairs = explicitDest ?? stairs?.Counterpart ?? stairs?.SecondCounterpart;
		if (p == null || building_ABStairs == null || !((Thing)building_ABStairs).Spawned || ABGiddyUpCompat.BlockForMount(p))
		{
			return;
		}
		Map map = ((Thing)stairs).Map;
		IntVec3 position = ((Thing)stairs).Position;
		bool flag = false;
		try
		{
			ABSettings settings = ABMod.Settings;
			flag = settings != null && settings.cameraFollowStairs && ((Thing)p).Faction == Faction.OfPlayer && !p.RaceProps.Animal && (object)Find.Selector.SingleSelectedThing == p && map == Find.CurrentMap;
		}
		catch
		{
			flag = false;
		}
		Thing val = null;
		try
		{
			Map map2 = ((Thing)building_ABStairs).Map;
			IntVec3 position2 = ((Thing)building_ABStairs).Position;
			bool drafted = p.Drafted;
			Pawn_CarryTracker carryTracker = p.carryTracker;
			val = ((carryTracker != null) ? carryTracker.CarriedThing : null);
			if (val != null)
			{
				((ThingOwner)p.carryTracker.innerContainer).Remove(val);
			}
			HostileDescend.NoteLeaving(p);
			((Entity)p).DeSpawn((DestroyMode)0);
			IntVec3 val2 = (GenGrid.Standable(position2, map2) ? position2 : CellFinder.StandableCellNear(position2, map2, 4f, (Predicate<IntVec3>)null));
			if (!((IntVec3)(ref val2)).IsValid)
			{
				val2 = position2;
			}
			GenSpawn.Spawn((Thing)(object)p, val2, map2, (WipeMode)0);
			if (val != null && !val.Destroyed)
			{
				if (p.carryTracker == null || !p.carryTracker.TryStartCarry(val))
				{
					GenPlace.TryPlaceThing(val, val2, map2, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
				}
				val = null;
			}
			if (drafted && p.drafter != null)
			{
				p.drafter.Drafted = true;
			}
			HostileDescend.NoteArrived(p, map2);
			CrossLevelAnimals.NoteArrived(p);
			ABApi.NotifyPawnTransferred(p, map, map2);
			if (flag && ((Thing)p).Spawned && !p.Dead)
			{
				LevelCamera.FollowPawn(p);
			}
			FinishCarriedDelivery(p, intent);
			PullFollowers(p, stairs, map, building_ABStairs);
			ContinueRide(p, building_ABStairs, rideFinal, map2);
			ABPendingOrders.TryRun(p, map2);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "stair transfer");
			if (!((Thing)p).Spawned && !((Thing)p).Destroyed && !p.Dead)
			{
				GenSpawn.Spawn((Thing)(object)p, position, map, (WipeMode)0);
			}
			if (val != null && !val.Destroyed && val.holdingOwner == null && !val.Spawned)
			{
				GenPlace.TryPlaceThing(val, position, map, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
			}
		}
	}

	private static void ContinueRide(Pawn p, Building_ABStairs arrivalCar, Building_ABStairs rideFinal, Map arrivedOn)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (rideFinal == null || ((Thing)rideFinal).Destroyed || !((Thing)rideFinal).Spawned || ((Thing)rideFinal).Map == arrivedOn || p == null || !((Thing)p).Spawned || p.Dead)
			{
				return;
			}
			Building_ABStairs building_ABStairs = ResolveNextDest(arrivalCar, rideFinal);
			if (building_ABStairs != null)
			{
				Job val = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)arrivalCar));
				val.targetC = LocalTargetInfo.op_Implicit((Thing)(object)rideFinal);
				Pawn_JobTracker jobs = p.jobs;
				if (jobs != null)
				{
					jobs.StartJob(val, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "elevator ride continuation");
		}
	}

	private static void PullFollowers(Pawn master, Building_ABStairs entry, Map sourceMap, Building_ABStairs dest)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (master == null || !master.IsColonistPlayerControlled || sourceMap == null || sourceMap.Disposed || entry == null || !((Thing)entry).Spawned)
			{
				return;
			}
			List<Pawn> list = sourceMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			for (int i = 0; i < list.Count; i++)
			{
				Pawn val = list[i];
				if (!val.RaceProps.Animal || val.Downed || val.Dead)
				{
					continue;
				}
				Pawn_PlayerSettings playerSettings = val.playerSettings;
				if (playerSettings == null || playerSettings.Master != master || (!playerSettings.followDrafted && !playerSettings.followFieldwork) || AnimalPenUtility.NeedsToBeManagedByRope(val))
				{
					continue;
				}
				IntVec3 val2 = ((Thing)val).Position - ((Thing)entry).Position;
				if (!((float)((IntVec3)(ref val2)).LengthHorizontalSquared > 144f) && val.CurJobDef != ABDefOf.AB_UseStairs && !ABGiddyUpCompat.IsCarryingRider(val) && ReachabilityUtility.CanReach(val, LocalTargetInfo.op_Implicit((Thing)(object)entry), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
				{
					Job val3 = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)entry));
					val3.targetC = LocalTargetInfo.op_Implicit((Thing)(object)dest);
					Pawn_JobTracker jobs = val.jobs;
					if (jobs != null)
					{
						jobs.StartJob(val3, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
					}
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "pet follow through stairs");
		}
	}

	private static void FinishCarriedDelivery(Pawn p, CarriedIntent intent)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Pawn_CarryTracker carryTracker = p.carryTracker;
			Thing val = ((carryTracker != null) ? carryTracker.CarriedThing : null);
			if (val == null || !p.IsColonistPlayerControlled || p.Drafted || LordUtility.GetLord(p) != null)
			{
				return;
			}
			Pawn val2 = (Pawn)(object)((val is Pawn) ? val : null);
			Thing val3 = default(Thing);
			if (val2 != null)
			{
				if (!p.carryTracker.TryDropCarriedThing(((Thing)p).Position, (ThingPlaceMode)1, ref val3, (Action<Thing, int>)null) || !((Thing)val2).Spawned || val2.Dead)
				{
					return;
				}
				Building_Bed val4 = null;
				JobDef val5 = null;
				if (intent == CarriedIntent.Capture)
				{
					val4 = RestUtility.FindBedFor(val2, p, false, false, (GuestStatus?)(GuestStatus)1);
					val5 = JobDefOf.Capture;
				}
				else if (intent == CarriedIntent.Imprison)
				{
					val4 = RestUtility.FindBedFor(val2, p, false, false, (GuestStatus?)(GuestStatus)1);
					val5 = (val2.Downed ? JobDefOf.TakeWoundedPrisonerToBed : JobDefOf.EscortPrisonerToBed);
				}
				else if (intent == CarriedIntent.Rescue || (val2.Downed && ((Thing)val2).Faction == ((Thing)p).Faction))
				{
					val4 = RestUtility.FindBedFor(val2, p, false, false, (GuestStatus?)null);
					val5 = JobDefOf.Rescue;
				}
				if (val4 == null || val5 == null)
				{
					return;
				}
				Job val6 = JobMaker.MakeJob(val5, LocalTargetInfo.op_Implicit((Thing)(object)val2), LocalTargetInfo.op_Implicit((Thing)(object)val4));
				val6.count = 1;
				Pawn_JobTracker jobs = p.jobs;
				if (jobs != null)
				{
					JobQueue jobQueue = jobs.jobQueue;
					if (jobQueue != null)
					{
						jobQueue.EnqueueFirst(val6, (JobTag?)(JobTag)0);
					}
				}
				return;
			}
			Job val7 = HaulAIUtility.HaulToStorageJob(p, val, false);
			if (val7 != null)
			{
				Pawn_JobTracker jobs2 = p.jobs;
				if (jobs2 != null)
				{
					JobQueue jobQueue2 = jobs2.jobQueue;
					if (jobQueue2 != null)
					{
						jobQueue2.EnqueueFirst(val7, (JobTag?)(JobTag)0);
					}
				}
			}
			else
			{
				p.carryTracker.TryDropCarriedThing(((Thing)p).Position, (ThingPlaceMode)1, ref val3, (Action<Thing, int>)null);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "carried delivery finish");
		}
	}
}
