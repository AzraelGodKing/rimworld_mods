using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AsAboveSoBelow;

internal static class CrossLevelCombat
{
	internal struct GapShot
	{
		public Map targetMap;

		public float distance;
	}

	internal const float GapHeight = 4f;

	internal static readonly Dictionary<int, Thing> PendingTargets = new Dictionary<int, Thing>();

	private static readonly List<IntVec3> tmpCandidates = new List<IntVec3>();

	internal static bool Enabled
	{
		get
		{
			ABSettings settings = ABMod.Settings;
			return ABGuard.On(ABGuard.Combat) && settings != null && settings.crossLevelCombat;
		}
	}

	internal static bool AreCrossGapPaired(Map a, Map b, out Map skyMap, out Map surfaceMap)
	{
		skyMap = null;
		surfaceMap = null;
		if (a == null || b == null || a == b || a.Disposed || b.Disposed)
		{
			return false;
		}
		LevelComp levelComp = a.Levels();
		if (levelComp == null)
		{
			return false;
		}
		if (levelComp.level == 1 && levelComp.lowerMap == b)
		{
			skyMap = a;
			surfaceMap = b;
			return true;
		}
		if (levelComp.level == 0 && levelComp.upperMap == b)
		{
			skyMap = b;
			surfaceMap = a;
			return true;
		}
		return false;
	}

	private static bool ExposedToGap(Map skyMap, IntVec3 col)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (!GenGrid.InBounds(col, skyMap))
		{
			return false;
		}
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		if (skyMap.terrainGrid.TerrainAt(col) == aB_OpenAir)
		{
			return true;
		}
		for (int i = 0; i < 4; i++)
		{
			IntVec3 val = col + GenAdj.CardinalDirections[i];
			if (GenGrid.InBounds(val, skyMap) && skyMap.terrainGrid.TerrainAt(val) == aB_OpenAir)
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasApertureTowards(Map skyMap, IntVec3 from, IntVec3 to)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		if (skyMap.terrainGrid.TerrainAt(from) == aB_OpenAir)
		{
			return true;
		}
		for (int i = 0; i < 4; i++)
		{
			IntVec3 val = from + GenAdj.CardinalDirections[i];
			if (GenGrid.InBounds(val, skyMap) && skyMap.terrainGrid.TerrainAt(val) == aB_OpenAir)
			{
				return true;
			}
		}
		IntVec3 val2 = to - from;
		int num = Mathf.Max(1, (int)(((IntVec3)(ref val2)).LengthHorizontal * 0.5f));
		int num2 = 0;
		foreach (IntVec3 item in GenSight.PointsOnLineOfSight(from, to))
		{
			if (++num2 > num)
			{
				break;
			}
			if (GenGrid.InBounds(item, skyMap) && skyMap.terrainGrid.TerrainAt(item) == aB_OpenAir)
			{
				return true;
			}
		}
		return false;
	}

	internal static Verb_LaunchProjectile GetRangedVerb(Pawn p)
	{
		if (p == null)
		{
			return null;
		}
		Pawn_EquipmentTracker equipment = p.equipment;
		object obj;
		if (equipment == null)
		{
			obj = null;
		}
		else
		{
			CompEquippable primaryEq = equipment.PrimaryEq;
			obj = ((primaryEq != null) ? primaryEq.PrimaryVerb : null);
		}
		Verb val = (Verb)obj;
		Verb_LaunchProjectile val2 = (Verb_LaunchProjectile)(object)((val is Verb_LaunchProjectile) ? val : null);
		if (val2 != null && !val.verbProps.IsMeleeAttack)
		{
			return val2;
		}
		VerbTracker verbTracker = p.verbTracker;
		List<Verb> list = ((verbTracker != null) ? verbTracker.AllVerbs : null);
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Verb obj2 = list[i];
				Verb_LaunchProjectile val3 = (Verb_LaunchProjectile)(object)((obj2 is Verb_LaunchProjectile) ? obj2 : null);
				if (val3 != null && !list[i].verbProps.IsMeleeAttack)
				{
					return val3;
				}
			}
		}
		return null;
	}

	internal static bool IsEquippedGunVerb(ITargetingSource source, Pawn caster, out Verb_LaunchProjectile gunVerb)
	{
		gunVerb = null;
		if (caster != null)
		{
			Verb val = (Verb)(object)((source is Verb) ? source : null);
			if (val != null && !(val is Verb_CastAbility))
			{
				Verb_LaunchProjectile val2 = (Verb_LaunchProjectile)(object)((val is Verb_LaunchProjectile) ? val : null);
				if (val2 != null && !val.verbProps.IsMeleeAttack)
				{
					Pawn_EquipmentTracker equipment = caster.equipment;
					object obj;
					if (equipment == null)
					{
						obj = null;
					}
					else
					{
						CompEquippable primaryEq = equipment.PrimaryEq;
						obj = ((primaryEq != null) ? primaryEq.PrimaryVerb : null);
					}
					if (val != obj)
					{
						return false;
					}
					if (val2.Projectile?.projectile?.flyOverhead == true)
					{
						return false;
					}
					gunVerb = val2;
					return true;
				}
			}
		}
		return false;
	}

	internal unsafe static bool CanCrossGapFire(Thing shooter, Thing target, Verb_LaunchProjectile verb, out GapShot shot)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		shot = default(GapShot);
		if (shooter == null || !shooter.Spawned)
		{
			return false;
		}
		CellRect val = GenAdj.OccupiedRect(shooter);
		Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (CanFireFrom(shooter.Map, current, target, verb, out shot))
				{
					return true;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		return false;
	}

	internal static bool CanFireFrom(Map shooterMap, IntVec3 sCol, Thing target, Verb_LaunchProjectile verb, out GapShot shot)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		shot = default(GapShot);
		if (!Enabled || shooterMap == null || verb == null || target == null)
		{
			return false;
		}
		if (target.Destroyed || !target.Spawned)
		{
			return false;
		}
		Map mapHeld = target.MapHeld;
		if (!AreCrossGapPaired(shooterMap, mapHeld, out var skyMap, out var _))
		{
			return false;
		}
		if (!GenGrid.InBounds(sCol, shooterMap))
		{
			return false;
		}
		IntVec3 position = target.Position;
		if (!GenGrid.InBounds(position, skyMap))
		{
			return false;
		}
		if (shooterMap == skyMap)
		{
			if (skyMap.terrainGrid.TerrainAt(position) != ABDefOf.AB_OpenAir)
			{
				return false;
			}
			if (!HasApertureTowards(skyMap, sCol, position))
			{
				return false;
			}
		}
		else
		{
			if (shooterMap.roofGrid.Roofed(sCol))
			{
				return false;
			}
			if (!ExposedToGap(skyMap, position))
			{
				return false;
			}
		}
		if (!GenSight.LineOfSight(sCol, position, skyMap, true, (Func<IntVec3, bool>)null, 0, 0))
		{
			return false;
		}
		IntVec3 val = position - sCol;
		float num = ((IntVec3)(ref val)).LengthHorizontalSquared;
		float num2 = Mathf.Sqrt(num + 16f);
		float effectiveRange = ((Verb)verb).EffectiveRange;
		if (effectiveRange <= 1.42f || num2 > effectiveRange)
		{
			return false;
		}
		float minRange = ((Verb)verb).verbProps.minRange;
		if (minRange > 0f && num2 < minRange)
		{
			return false;
		}
		shot = new GapShot
		{
			targetMap = mapHeld,
			distance = num2
		};
		return true;
	}

	internal static bool CanArcFireAt(Map shooterMap, IntVec3 sCol, IntVec3 tCol, Map targetMap, Verb_LaunchProjectile verb, out GapShot shot)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		shot = default(GapShot);
		if (!Enabled || shooterMap == null || verb == null)
		{
			return false;
		}
		if (!AreCrossGapPaired(shooterMap, targetMap, out var skyMap, out var _))
		{
			return false;
		}
		if (!GenGrid.InBounds(sCol, shooterMap) || !GenGrid.InBounds(tCol, targetMap))
		{
			return false;
		}
		TerrainDef aB_OpenAir = ABDefOf.AB_OpenAir;
		IntVec3 val = ((shooterMap == skyMap) ? tCol : sCol);
		if (!GenGrid.InBounds(val, skyMap) || skyMap.terrainGrid.TerrainAt(val) != aB_OpenAir)
		{
			return false;
		}
		IntVec3 val2 = tCol - sCol;
		float num = ((IntVec3)(ref val2)).LengthHorizontalSquared;
		float num2 = Mathf.Sqrt(num + 16f);
		float effectiveRange = ((Verb)verb).EffectiveRange;
		if (effectiveRange <= 1.42f || num2 > effectiveRange)
		{
			return false;
		}
		float minRange = ((Verb)verb).verbProps.minRange;
		if (minRange > 0f && num2 < minRange)
		{
			return false;
		}
		shot = new GapShot
		{
			targetMap = targetMap,
			distance = num2
		};
		return true;
	}

	internal static bool FireArcShot(Thing shooter, Pawn manningPawn, Verb_LaunchProjectile verb, LocalTargetInfo target, Map targetMap, float distance)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (shooter == null || verb == null || targetMap == null || targetMap.Disposed || !((LocalTargetInfo)(ref target)).IsValid)
			{
				return false;
			}
			ThingDef projectile = verb.Projectile;
			if (projectile == null)
			{
				return false;
			}
			IntVec3 cell = ((LocalTargetInfo)(ref target)).Cell;
			IntVec3 position = shooter.Position;
			position.x = Mathf.Clamp(position.x, 0, targetMap.Size.x - 1);
			position.z = Mathf.Clamp(position.z, 0, targetMap.Size.z - 1);
			Vector3 val = ((IntVec3)(ref position)).ToVector3Shifted();
			Projectile val2 = (Projectile)GenSpawn.Spawn(projectile, position, targetMap, (WipeMode)0);
			Thing equipmentSource = (Thing)(object)((Verb)verb).EquipmentSource;
			float num = ((Verb)verb).verbProps.ForcedMissRadius;
			if (manningPawn != null)
			{
				num *= ((Verb)verb).verbProps.GetForceMissFactorFor(equipmentSource, manningPawn);
			}
			float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, cell - position);
			IntVec3 val3 = cell;
			if (num2 > 0.5f)
			{
				int num3 = GenRadial.NumCellsInRadius(num2);
				val3 = cell + GenRadial.RadialPattern[Rand.Range(0, num3)];
			}
			ProjectileHitFlags val4 = (ProjectileHitFlags)4;
			if (Rand.Chance(0.5f))
			{
				val4 = (ProjectileHitFlags)(-1);
			}
			if (val3 == cell && ((LocalTargetInfo)(ref target)).HasThing)
			{
				val2.Launch(shooter, val, target, target, val4, false, equipmentSource, (ThingDef)null);
			}
			else
			{
				val2.Launch(shooter, val, LocalTargetInfo.op_Implicit(val3), target, val4, false, equipmentSource, (ThingDef)null);
			}
			SoundDef soundCast = ((Verb)verb).verbProps.soundCast;
			if (soundCast != null)
			{
				SoundStarter.PlayOneShot(soundCast, SoundInfo.InMap(new TargetInfo(shooter.Position, shooter.Map, false), (MaintenanceType)0));
			}
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "cross level arc shot");
			return false;
		}
	}

	internal static float ComputeAimChance(Thing shooter, Verb_LaunchProjectile verb, Thing target, float distance)
	{
		float num = 1f;
		if (((Verb)verb).verbProps.canGoWild)
		{
			num *= ShotReport.HitFactorFromShooter(shooter, distance, (float?)null);
		}
		num *= ((Verb)verb).verbProps.GetHitChanceFactor((Thing)(object)((Verb)verb).EquipmentSource, distance);
		Map mapHeld = target.MapHeld;
		if (mapHeld != null)
		{
			num *= mapHeld.weatherManager.CurWeatherAccuracyMultiplier;
		}
		num += DarknessOffset(shooter, target);
		if (num < 0.0201f)
		{
			num = 0.0201f;
		}
		num *= TargetSizeFactor(target);
		return Mathf.Clamp01(num);
	}

	private static float DarknessOffset(Thing shooter, Thing target)
	{
		if (!ModsConfig.IdeologyActive || shooter == null || target == null)
		{
			return 0f;
		}
		try
		{
			if (DarknessCombatUtility.IsOutdoorsAndLit(target))
			{
				return StatExtension.GetStatValue(shooter, StatDefOf.ShootingAccuracyOutdoorsLitOffset, true, -1);
			}
			if (DarknessCombatUtility.IsOutdoorsAndDark(target))
			{
				return StatExtension.GetStatValue(shooter, StatDefOf.ShootingAccuracyOutdoorsDarkOffset, true, -1);
			}
			if (DarknessCombatUtility.IsIndoorsAndDark(target))
			{
				return StatExtension.GetStatValue(shooter, StatDefOf.ShootingAccuracyIndoorsDarkOffset, true, -1);
			}
			if (DarknessCombatUtility.IsIndoorsAndLit(target))
			{
				return StatExtension.GetStatValue(shooter, StatDefOf.ShootingAccuracyIndoorsLitOffset, true, -1);
			}
		}
		catch
		{
		}
		return 0f;
	}

	private static float TargetSizeFactor(Thing t)
	{
		Pawn val = (Pawn)(object)((t is Pawn) ? t : null);
		float num = ((val == null) ? (t.def.fillPercent * (float)t.def.size.x * (float)t.def.size.z * 2.5f) : val.BodySize);
		return Mathf.Clamp(num, 0.5f, 2f);
	}

	internal static bool Fire(Thing shooter, Verb_LaunchProjectile verb, Thing target)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Invalid comparison between Unknown and I4
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (shooter == null || verb == null || target == null)
			{
				return false;
			}
			if (!CanCrossGapFire(shooter, target, verb, out var shot))
			{
				return false;
			}
			ThingDef projectile = verb.Projectile;
			if (projectile == null)
			{
				return false;
			}
			Map targetMap = shot.targetMap;
			IntVec3 val = shooter.Position - target.Position;
			Vector3 val2 = ((IntVec3)(ref val)).ToVector3();
			val2.y = 0f;
			val2 = ((((Vector3)(ref val2)).sqrMagnitude > 0.001f) ? ((Vector3)(ref val2)).normalized : Vector3.forward);
			float num = Mathf.Min(2f, Mathf.Max(1f, shot.distance - 1f));
			Vector3 val3 = target.DrawPos + val2 * num;
			IntVec3 val4 = IntVec3Utility.ToIntVec3(val3);
			if (!GenGrid.InBounds(val4, targetMap))
			{
				val4 = target.Position;
				val3 = target.DrawPos;
			}
			Projectile val5 = (Projectile)GenSpawn.Spawn(projectile, val4, targetMap, (WipeMode)0);
			float num2 = ComputeAimChance(shooter, verb, target, shot.distance);
			bool flag = Rand.Chance(num2);
			Thing equipmentSource = (Thing)(object)((Verb)verb).EquipmentSource;
			if (flag)
			{
				ProjectileHitFlags val6 = (ProjectileHitFlags)1;
				if (!target.def.destroyable || (int)target.def.Fillage == 2)
				{
					val6 = (ProjectileHitFlags)(val6 | 4);
				}
				val5.Launch(shooter, val3, LocalTargetInfo.op_Implicit(target), LocalTargetInfo.op_Implicit(target), val6, false, equipmentSource, (ThingDef)null);
			}
			else
			{
				ShootLine val7 = default(ShootLine);
				((ShootLine)(ref val7))._002Ector(val4, target.Position);
				bool flag2 = projectile.projectile != null && projectile.projectile.flyOverhead;
				((ShootLine)(ref val7)).ChangeDestToMissWild(num2, flag2, targetMap);
				ProjectileHitFlags val8 = (ProjectileHitFlags)6;
				val5.Launch(shooter, val3, LocalTargetInfo.op_Implicit(((ShootLine)(ref val7)).Dest), LocalTargetInfo.op_Implicit(target), val8, false, equipmentSource, (ThingDef)null);
			}
			SoundDef soundCast = ((Verb)verb).verbProps.soundCast;
			if (soundCast != null)
			{
				SoundStarter.PlayOneShot(soundCast, SoundInfo.InMap(new TargetInfo(shooter.Position, shooter.Map, false), (MaintenanceType)0));
			}
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "cross level fire");
			return false;
		}
	}

	internal static IntVec3 FindFiringCell(Pawn shooter, Thing target, Verb_LaunchProjectile verb)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		if (shooter == null || !((Thing)shooter).Spawned)
		{
			return IntVec3.Invalid;
		}
		if (CanFireFrom(((Thing)shooter).Map, ((Thing)shooter).Position, target, verb, out var shot))
		{
			return ((Thing)shooter).Position;
		}
		Map map = ((Thing)shooter).Map;
		float num = Mathf.Clamp(((Verb)verb).EffectiveRange, 4f, 30f);
		tmpCandidates.Clear();
		int num2 = 0;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Position, num, true))
		{
			if (++num2 > 600)
			{
				break;
			}
			if (GenGrid.InBounds(item, map) && GenGrid.Standable(item, map) && CanFireFrom(map, item, target, verb, out shot))
			{
				tmpCandidates.Add(item);
				if (tmpCandidates.Count >= 64)
				{
					break;
				}
			}
		}
		IntVec3 origin = ((Thing)shooter).Position;
		tmpCandidates.Sort(delegate(IntVec3 a, IntVec3 b)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			IntVec3 val = a - origin;
			int lengthHorizontalSquared = ((IntVec3)(ref val)).LengthHorizontalSquared;
			val = b - origin;
			return lengthHorizontalSquared.CompareTo(((IntVec3)(ref val)).LengthHorizontalSquared);
		});
		int num3 = 0;
		for (int num4 = 0; num4 < tmpCandidates.Count; num4++)
		{
			if (++num3 > 16)
			{
				break;
			}
			if (ReachabilityUtility.CanReach(shooter, LocalTargetInfo.op_Implicit(tmpCandidates[num4]), (PathEndMode)1, (Danger)3, false, false, (TraverseMode)0))
			{
				return tmpCandidates[num4];
			}
		}
		return IntVec3.Invalid;
	}

	internal static bool TryStartCrossGapAttack(Pawn pawn, Thing target)
	{
		try
		{
			if (!Enabled || pawn == null || !((Thing)pawn).Spawned || target == null)
			{
				return false;
			}
			if (!pawn.Drafted || pawn.Downed || pawn.Dead)
			{
				return false;
			}
			return StartAttackJob(pawn, target, playerForced: true, allowReposition: true);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "start cross level attack");
			return false;
		}
	}

	internal static bool TryStartAutoEngage(Pawn pawn, Thing target, bool allowReposition)
	{
		try
		{
			if (!Enabled || pawn == null || !((Thing)pawn).Spawned || target == null || pawn.Downed || pawn.Dead || pawn.jobs == null)
			{
				return false;
			}
			return StartAttackJob(pawn, target, playerForced: false, allowReposition);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "auto cross level engage");
			return false;
		}
	}

	private static bool StartAttackJob(Pawn pawn, Thing target, bool playerForced, bool allowReposition)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		Verb_LaunchProjectile rangedVerb = GetRangedVerb(pawn);
		if (rangedVerb == null)
		{
			return false;
		}
		GapShot shot;
		IntVec3 val = ((!allowReposition) ? (CanFireFrom(((Thing)pawn).Map, ((Thing)pawn).Position, target, rangedVerb, out shot) ? ((Thing)pawn).Position : IntVec3.Invalid) : FindFiringCell(pawn, target, rangedVerb));
		if (!((IntVec3)(ref val)).IsValid)
		{
			return false;
		}
		Job val2 = JobMaker.MakeJob(ABDefOf.AB_CrossLevelAttack);
		val2.SetTarget((TargetIndex)2, LocalTargetInfo.op_Implicit(val));
		val2.playerForced = playerForced;
		if (PendingTargets.Count > 64)
		{
			PendingTargets.Clear();
		}
		PendingTargets[((Thing)pawn).thingIDNumber] = target;
		if (playerForced)
		{
			pawn.jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)0, false);
		}
		else
		{
			pawn.jobs.StartJob(val2, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
		}
		return true;
	}
}
