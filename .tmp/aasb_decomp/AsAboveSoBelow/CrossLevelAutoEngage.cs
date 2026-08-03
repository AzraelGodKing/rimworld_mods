using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

internal static class CrossLevelAutoEngage
{
	private const int FailCooldownTicks = 700;

	private const int MaxEngagesPerScan = 4;

	private const int MaxTargetProbes = 4;

	private static readonly ABPawnCooldown cooldown = new ABPawnCooldown();

	private static readonly List<Pawn> tmpTargets = new List<Pawn>();

	private const int OverwatchRetryTicks = 60;

	internal static bool AutoEngageEnabled
	{
		get
		{
			ABSettings settings = ABMod.Settings;
			return CrossLevelCombat.Enabled && settings != null && settings.crossLevelAutoEngage;
		}
	}

	public static void ScanPair(Map sky, Map surface)
	{
		if (AutoEngageEnabled && sky != null && surface != null && !sky.Disposed && !surface.Disposed)
		{
			ScanHostiles(surface, sky);
			ScanHostiles(sky, surface);
			CrossLevelTurret.AcquireAuto(sky, surface);
		}
	}

	public static bool TryEngageInsteadOfDescend(Pawn p)
	{
		if (!AutoEngageEnabled || ((p != null) ? ((Thing)p).Map : null) == null)
		{
			return false;
		}
		Map val = PairedMap(((Thing)p).Map);
		if (val == null)
		{
			return false;
		}
		return TryEngageAcross(p, val, allowReposition: true);
	}

	private static Map PairedMap(Map map)
	{
		LevelComp levelComp = map.Levels();
		if (levelComp == null)
		{
			return null;
		}
		if (levelComp.level == 1)
		{
			return levelComp.lowerMap;
		}
		return (levelComp.level == 0) ? levelComp.upperMap : null;
	}

	private static void ScanHostiles(Map shooterMap, Map targetMap)
	{
		if (targetMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Count == 0)
		{
			return;
		}
		IReadOnlyList<Pawn> allPawnsSpawned = shooterMap.mapPawns.AllPawnsSpawned;
		int num = 0;
		int ticksGame = Find.TickManager.TicksGame;
		int num2 = allPawnsSpawned.Count - 1;
		while (num2 >= 0 && num < 4)
		{
			if (num2 < allPawnsSpawned.Count)
			{
				Pawn val = allPawnsSpawned[num2];
				if (val != null && !val.Dead && !val.Downed && ((Thing)val).Spawned && ((Thing)val).Faction != Faction.OfPlayer && GenHostility.HostileTo((Thing)(object)val, Faction.OfPlayer) && !val.InMentalState && !ABVehicleCompat.IsVehicle(val))
				{
					JobDef curJobDef = val.CurJobDef;
					if (curJobDef != ABDefOf.AB_CrossLevelAttack && curJobDef != ABDefOf.AB_UseStairs && cooldown.Ready(val, ticksGame) && (HostileDescend.IsIdle(val) || !HostileDescend.HasReachableTarget(val)))
					{
						if (CrossLevelCombat.GetRangedVerb(val) == null)
						{
							cooldown.ChargeUntil(val, ticksGame + 1400);
						}
						else if (TryEngageAcross(val, targetMap, allowReposition: true))
						{
							num++;
							ABLog.Dev("Hostile " + ((Entity)val).LabelShort + " auto-engaging across the gap.");
						}
						else
						{
							cooldown.ChargeUntil(val, ticksGame + 700);
						}
					}
				}
			}
			num2--;
		}
	}

	internal static void TryOverwatchCrossFire(Pawn p)
	{
		try
		{
			if (!AutoEngageEnabled || p == null || !((Thing)p).Spawned || p.Dead || p.Downed || !p.Drafted || p.drafter == null || !p.drafter.FireAtWill || p.InMentalState)
			{
				return;
			}
			int ticksGame = Find.TickManager.TicksGame;
			if (cooldown.Ready(p, ticksGame))
			{
				Map val = PairedMap(((Thing)p).Map);
				if (val == null || val.Disposed)
				{
					cooldown.ChargeUntil(p, ticksGame + 1400);
				}
				else if (HasNearbySameMapThreat(p))
				{
					cooldown.ChargeUntil(p, ticksGame + 60);
				}
				else if (CrossLevelCombat.GetRangedVerb(p) == null)
				{
					cooldown.ChargeUntil(p, ticksGame + 1400);
				}
				else if (TryEngageAcross(p, val, allowReposition: false))
				{
					ABLog.Dev("Colonist " + ((Entity)p).LabelShort + " returning fire across the gap.");
				}
				else
				{
					cooldown.ChargeUntil(p, ticksGame + 60);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "overwatch cross fire");
		}
	}

	private static bool HasNearbySameMapThreat(Pawn p)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		IntVec3 val2;
		if (((Thing)p).Faction == null)
		{
			List<Pawn> list = ((Thing)p).Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			for (int i = 0; i < list.Count; i++)
			{
				Pawn val = list[i];
				if (!val.Dead && !val.Downed)
				{
					val2 = ((Thing)val).Position - ((Thing)p).Position;
					if ((float)((IntVec3)(ref val2)).LengthHorizontalSquared <= 1600f)
					{
						return true;
					}
				}
			}
			return false;
		}
		int num = 0;
		foreach (IAttackTarget item in ((Thing)p).Map.attackTargetsCache.TargetsHostileToFaction(((Thing)p).Faction))
		{
			if (++num > 10)
			{
				return true;
			}
			Thing thing = item.Thing;
			if (thing != null && !thing.Destroyed && thing.Spawned && !item.ThreatDisabled((IAttackTargetSearcher)(object)p))
			{
				val2 = thing.Position - ((Thing)p).Position;
				if ((float)((IntVec3)(ref val2)).LengthHorizontalSquared <= 1600f)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool TryEngageAcross(Pawn shooter, Map targetMap, bool allowReposition)
	{
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		Verb_LaunchProjectile rangedVerb = CrossLevelCombat.GetRangedVerb(shooter);
		if (rangedVerb == null || targetMap == null || targetMap.Disposed)
		{
			return false;
		}
		bool flag = GenHostility.HostileTo((Thing)(object)shooter, Faction.OfPlayer);
		tmpTargets.Clear();
		IReadOnlyList<Pawn> allPawnsSpawned = targetMap.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn val = allPawnsSpawned[i];
			if (val == null || val.Dead || !((Thing)val).Spawned)
			{
				continue;
			}
			if (flag)
			{
				if (((Thing)val).Faction != Faction.OfPlayer || val.Downed)
				{
					continue;
				}
			}
			else if (!GenHostility.HostileTo((Thing)(object)val, Faction.OfPlayer) || val.Downed)
			{
				continue;
			}
			tmpTargets.Add(val);
		}
		if (tmpTargets.Count == 0)
		{
			return false;
		}
		IntVec3 origin = ((Thing)shooter).Position;
		tmpTargets.Sort(delegate(Pawn a, Pawn b)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			IntVec3 val2 = ((Thing)a).Position - origin;
			int lengthHorizontalSquared = ((IntVec3)(ref val2)).LengthHorizontalSquared;
			val2 = ((Thing)b).Position - origin;
			return lengthHorizontalSquared.CompareTo(((IntVec3)(ref val2)).LengthHorizontalSquared);
		});
		int num = Math.Min(tmpTargets.Count, 4);
		for (int num2 = 0; num2 < num; num2++)
		{
			Pawn target = tmpTargets[num2];
			if ((allowReposition || CrossLevelCombat.CanFireFrom(((Thing)shooter).Map, ((Thing)shooter).Position, (Thing)(object)target, rangedVerb, out var _)) && CrossLevelCombat.TryStartAutoEngage(shooter, (Thing)(object)target, allowReposition))
			{
				return true;
			}
		}
		return false;
	}
}
