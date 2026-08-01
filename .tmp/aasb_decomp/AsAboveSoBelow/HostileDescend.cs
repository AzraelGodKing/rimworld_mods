using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

internal static class HostileDescend
{
	private const int MaxReachChecksPerPawn = 8;

	public static void ScanPocketMap(LevelComp comp)
	{
		Map map = ((MapComponent)comp).map;
		Map val = ((comp.level > 0) ? comp.lowerMap : comp.upperMap);
		if (val == null || val.Disposed)
		{
			return;
		}
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		for (int num = allPawnsSpawned.Count - 1; num >= 0; num--)
		{
			if (num < allPawnsSpawned.Count)
			{
				Pawn val2 = allPawnsSpawned[num];
				if (val2 != null && ((Thing)val2).Faction != Faction.OfPlayer && !val2.Dead && !val2.Downed && ((Thing)val2).Spawned)
				{
					bool flag = GenHostility.HostileTo((Thing)(object)val2, Faction.OfPlayer);
					if (!(flag ? (!ShouldDescendHostile(val2)) : (!ShouldDescendFriendly(val2))))
					{
						if (flag && CrossLevelAutoEngage.TryEngageInsteadOfDescend(val2))
						{
							ABLog.Dev("Hostile " + ((Entity)val2).LabelShort + " engages across the gap instead of descending.");
						}
						else
						{
							Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairs(val2, val, checkReachability: true);
							Building_ABStairs building_ABStairs2 = building_ABStairs?.CounterpartTowards(val);
							if (building_ABStairs2 != null)
							{
								Job val3 = CrossLevelWork.MakeStairsJob(building_ABStairs, building_ABStairs2);
								Pawn_JobTracker jobs = val2.jobs;
								if (jobs != null)
								{
									jobs.StartJob(val3, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
								}
								ABLog.Dev((flag ? "Hostile " : "NPC ") + ((Entity)val2).LabelShort + " descending from level " + comp.level + " via " + ((Thing)building_ABStairs).ThingID + ".");
							}
						}
					}
				}
			}
		}
	}

	public static void ScanGroundHostiles(LevelComp comp)
	{
		Map map = ((MapComponent)comp).map;
		if (map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer).Count == 0)
		{
			return;
		}
		Map val = null;
		if (HasPlayerPresence(comp.upperMap))
		{
			val = comp.upperMap;
		}
		else if (HasPlayerPresence(comp.lowerMap))
		{
			val = comp.lowerMap;
		}
		if (val == null)
		{
			return;
		}
		int num = 0;
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		int num2 = allPawnsSpawned.Count - 1;
		while (num2 >= 0 && num < 8)
		{
			if (num2 < allPawnsSpawned.Count)
			{
				Pawn val2 = allPawnsSpawned[num2];
				if (val2 != null && ((Thing)val2).Faction != Faction.OfPlayer && !val2.Dead && !val2.Downed && ((Thing)val2).Spawned && GenHostility.HostileTo((Thing)(object)val2, Faction.OfPlayer) && ShouldDescendHostile(val2))
				{
					if (CrossLevelAutoEngage.TryEngageInsteadOfDescend(val2))
					{
						num++;
					}
					else
					{
						Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairs(val2, val, checkReachability: true);
						Building_ABStairs building_ABStairs2 = building_ABStairs?.CounterpartTowards(val);
						if (building_ABStairs2 != null)
						{
							Job val3 = CrossLevelWork.MakeStairsJob(building_ABStairs, building_ABStairs2);
							Pawn_JobTracker jobs = val2.jobs;
							if (jobs != null)
							{
								jobs.StartJob(val3, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
							}
							num++;
							ABLog.Dev("Hostile " + ((Entity)val2).LabelShort + " ascending toward the colony via " + ((Thing)building_ABStairs).ThingID + ".");
						}
					}
				}
			}
			num2--;
		}
	}

	private static bool HasPlayerPresence(Map m)
	{
		if (m == null || m.Disposed)
		{
			return false;
		}
		return m.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Count > 0 || m.listerBuildings.allBuildingsColonist.Count > 0;
	}

	internal static bool ShouldDescendHostile(Pawn p)
	{
		if ((!p.RaceProps.Humanlike && !p.RaceProps.IsMechanoid && (!p.RaceProps.Animal || ((Thing)p).Faction == null)) || p.RaceProps.Insect || ((Thing)p).Faction == Faction.OfInsects)
		{
			return false;
		}
		if (ABVehicleCompat.IsVehicle(p))
		{
			return false;
		}
		if (p.InMentalState || p.CurJobDef == ABDefOf.AB_UseStairs || ABGiddyUpCompat.BlockForMount(p))
		{
			return false;
		}
		return IsIdle(p) && !HasReachableTarget(p);
	}

	private static bool WantsToLeave(Pawn p)
	{
		Lord lord = LordUtility.GetLord(p);
		if (lord == null)
		{
			return IsIdle(p);
		}
		DutyDef val = p.mindState?.duty?.def;
		if (val == null)
		{
			return false;
		}
		return val == DutyDefOf.TravelOrLeave || val == DutyDefOf.TravelOrWait || val == DutyDefOf.Kidnap || val == DutyDefOf.Steal || val == DutyDefOf.TakeWoundedGuest || val == DutyDefOf.PrisonerEscape || ABApi.IsRegisteredExitDuty(val);
	}

	private static bool ShouldDescendFriendly(Pawn p)
	{
		if (p.InMentalState || p.CurJobDef == ABDefOf.AB_UseStairs || ABGiddyUpCompat.BlockForMount(p))
		{
			return false;
		}
		if (ABVehicleCompat.IsVehicle(p))
		{
			return false;
		}
		if (!p.RaceProps.Humanlike && !p.RaceProps.Animal)
		{
			return false;
		}
		if (((Thing)p).Faction == null && p.RaceProps.Animal)
		{
			return IsIdle(p) && CrossLevelAnimals.WildAnimalWantsOff(p);
		}
		return WantsToLeave(p);
	}

	internal static bool IsIdle(Pawn p)
	{
		JobDef curJobDef = p.CurJobDef;
		if (curJobDef == null || curJobDef == JobDefOf.Wait_Wander || curJobDef == JobDefOf.GotoWander || curJobDef == JobDefOf.Wait)
		{
			return true;
		}
		if ((curJobDef == JobDefOf.AttackMelee || curJobDef == JobDefOf.AttackStatic) && p.CurJob != null && ((LocalTargetInfo)(ref p.CurJob.targetA)).Thing is Building_ABStairs)
		{
			return true;
		}
		return false;
	}

	internal static bool HasReachableTarget(Pawn p)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (((Thing)p).Faction == null)
		{
			List<Pawn> list = ((Thing)p).Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			for (int i = 0; i < list.Count; i++)
			{
				Pawn val = list[i];
				if (!val.Dead && !val.Downed && ReachabilityUtility.CanReach(p, LocalTargetInfo.op_Implicit((Thing)(object)val), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
				{
					return true;
				}
			}
			return false;
		}
		HashSet<IAttackTarget> hashSet = ((Thing)p).Map.attackTargetsCache.TargetsHostileToFaction(((Thing)p).Faction);
		int num = 0;
		foreach (IAttackTarget item in hashSet)
		{
			Thing thing = item.Thing;
			if (thing != null && !thing.Destroyed && thing.Spawned && !item.ThreatDisabled((IAttackTargetSearcher)(object)p))
			{
				if (++num > 8)
				{
					return true;
				}
				if (ReachabilityUtility.CanReach(p, LocalTargetInfo.op_Implicit(thing), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void NoteLeaving(Pawn p)
	{
		try
		{
			if (p != null && ((Thing)p).Faction != null && ((Thing)p).Faction != Faction.OfPlayer)
			{
				Lord lord = LordUtility.GetLord(p);
				if (lord != null)
				{
					lord.Notify_PawnLost(p, (PawnLostCondition)6, (DamageInfo?)null);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.HostileMove, e, "npc lord release");
		}
	}

	public static void NoteArrived(Pawn p, Map map)
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Expected O, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		try
		{
			if (p == null || map == null || ((Thing)p).Faction == null || ((Thing)p).Faction == Faction.OfPlayer || LordUtility.GetLord(p) != null)
			{
				return;
			}
			if (GenHostility.HostileTo((Thing)(object)p, Faction.OfPlayer))
			{
				if (!p.RaceProps.Humanlike && !p.RaceProps.IsMechanoid && !p.RaceProps.Animal)
				{
					return;
				}
				List<Lord> lords = map.lordManager.lords;
				for (int i = 0; i < lords.Count; i++)
				{
					Lord val = lords[i];
					if (val.faction == ((Thing)p).Faction && val.LordJob is LordJob_AssaultColony)
					{
						val.AddPawn(p);
						return;
					}
				}
				LordMaker.MakeNewLord(((Thing)p).Faction, (LordJob)new LordJob_AssaultColony(((Thing)p).Faction, true, true, false, false, true, false, false), map, Gen.YieldSingle<Pawn>(p));
			}
			else
			{
				LevelComp levelComp = map.Levels();
				if (levelComp != null && levelComp.level == 0)
				{
					LordMaker.MakeNewLord(((Thing)p).Faction, (LordJob)new LordJob_ExitMapBest((LocomotionUrgency)3, false, true), map, Gen.YieldSingle<Pawn>(p));
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.HostileMove, e, "npc lord join");
		}
	}
}
