using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

internal static class CrossLevelAnimals
{
	private const int LingerTicksBase = 5000;

	private const int AmbientSpacingTicks = 12000;

	private const int BasementWildCap = 4;

	private const float AmbientChancePerScan = 0.15f;

	private static readonly Dictionary<int, int> leaveAfter = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> nextAmbient = new Dictionary<int, int>();

	private static readonly List<Pawn> tmpEligible = new List<Pawn>();

	internal static bool Enabled
	{
		get
		{
			ABSettings settings = ABMod.Settings;
			return ABGuard.On(ABGuard.HostileMove) && settings != null && settings.crossLevelAnimalWander;
		}
	}

	internal static void ClearAll()
	{
		leaveAfter.Clear();
		nextAmbient.Clear();
	}

	private static bool IsWildAnimal(Pawn p)
	{
		return p != null && ((Thing)p).Faction == null && p.RaceProps.Animal && !p.Dead && !p.Downed && ((Thing)p).Spawned;
	}

	internal static bool WildAnimalWantsOff(Pawn p)
	{
		if (!Enabled)
		{
			return false;
		}
		Need_Food val = p.needs?.food;
		if (val != null)
		{
			Pawn_HealthTracker health = p.health;
			if (health != null)
			{
				HediffSet hediffSet = health.hediffSet;
				if (((hediffSet != null) ? new bool?(hediffSet.HasHediff(HediffDefOf.Malnutrition, false)) : ((bool?)null)) == true)
				{
					goto IL_0082;
				}
			}
			if (((Need)val).CurLevelPercentage < 0.25f)
			{
				goto IL_0082;
			}
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (leaveAfter.TryGetValue(((Thing)p).thingIDNumber, out var value))
		{
			return ticksGame >= value;
		}
		if (leaveAfter.Count > 256)
		{
			leaveAfter.Clear();
		}
		leaveAfter[((Thing)p).thingIDNumber] = ticksGame + 2500 + ((Thing)p).thingIDNumber * 977 % 5000;
		return false;
		IL_0082:
		return true;
	}

	internal static void NoteArrived(Pawn p)
	{
		if (IsWildAnimal(p))
		{
			if (leaveAfter.Count > 256)
			{
				leaveAfter.Clear();
			}
			int ticksGame = Find.TickManager.TicksGame;
			leaveAfter[((Thing)p).thingIDNumber] = ticksGame + 2500 + ((Thing)p).thingIDNumber * 977 % 5000;
		}
	}

	public static void ScanSurfaceAmbient(LevelComp comp)
	{
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled)
		{
			return;
		}
		Map map = ((MapComponent)comp).map;
		Map lowerMap = comp.lowerMap;
		if (lowerMap == null || lowerMap.Disposed)
		{
			return;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if ((nextAmbient.TryGetValue(map.uniqueID, out var value) && ticksGame < value) || !Rand.Chance(0.15f))
		{
			return;
		}
		if (WildAnimalCount(lowerMap) >= 4)
		{
			Charge(map, ticksGame + 12000);
			return;
		}
		float outdoorTemp = lowerMap.mapTemperature.OutdoorTemp;
		tmpEligible.Clear();
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		int num = 0;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			if (num >= 80)
			{
				break;
			}
			Pawn val = allPawnsSpawned[i];
			num++;
			if (!IsWildAnimal(val) || val.InMentalState)
			{
				continue;
			}
			JobDef curJobDef = val.CurJobDef;
			if (curJobDef != JobDefOf.PredatorHunt && curJobDef != ABDefOf.AB_UseStairs)
			{
				FloatRange val2 = GenTemperature.ComfortableTemperatureRange(val);
				if (((FloatRange)(ref val2)).Includes(outdoorTemp))
				{
					tmpEligible.Add(val);
				}
			}
		}
		if (tmpEligible.Count == 0)
		{
			Charge(map, ticksGame + 6000);
			return;
		}
		Pawn val3 = tmpEligible[Rand.Range(0, tmpEligible.Count)];
		if (TryDescend(val3, lowerMap))
		{
			Charge(map, ticksGame + 12000);
			ABLog.Dev("Wild " + ((Entity)val3).LabelShort + " wandering down into the basement.");
		}
		else
		{
			Charge(map, ticksGame + 3000);
		}
	}

	private static void Charge(Map surface, int untilTick)
	{
		if (nextAmbient.Count > 64)
		{
			nextAmbient.Clear();
		}
		nextAmbient[surface.uniqueID] = untilTick;
	}

	private static int WildAnimalCount(Map map)
	{
		int num = 0;
		IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			if (IsWildAnimal(allPawnsSpawned[i]))
			{
				num++;
			}
		}
		return num;
	}

	internal static bool TryDescend(Pawn p, Map basement)
	{
		try
		{
			if (p == null || !((Thing)p).Spawned || basement == null || basement.Disposed)
			{
				return false;
			}
			Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairs(p, basement, checkReachability: true);
			Building_ABStairs building_ABStairs2 = building_ABStairs?.CounterpartTowards(basement);
			if (building_ABStairs2 == null)
			{
				return false;
			}
			Job val = CrossLevelWork.MakeStairsJob(building_ABStairs, building_ABStairs2);
			Pawn_JobTracker jobs = p.jobs;
			if (jobs != null)
			{
				jobs.StartJob(val, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
			}
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.HostileMove, e, "animal ambient descent");
			return false;
		}
	}
}
