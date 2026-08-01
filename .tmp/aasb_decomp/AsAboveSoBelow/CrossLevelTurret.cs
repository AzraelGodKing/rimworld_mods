using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsAboveSoBelow;

internal static class CrossLevelTurret
{
	private enum Phase
	{
		Warmup,
		Burst,
		Cooldown
	}

	private sealed class Entry
	{
		public Building_Turret turret;

		public LocalTargetInfo target;

		public Map targetMap;

		public bool arc;

		public bool auto;

		public Phase phase;

		public int nextEventTick;

		public int burstShotsLeft;

		public int revalidateAt;
	}

	private enum FireResult
	{
		Fired,
		Hold,
		Dead
	}

	private const int RevalidateInterval = 30;

	private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();

	private static readonly Dictionary<int, int> nextAutoTry = new Dictionary<int, int>();

	private static readonly List<int> tmpDead = new List<int>();

	private static readonly List<Pawn> tmpTargets = new List<Pawn>();

	private const int EventRetryTicks = 120;

	private const int AutoRetryTicks = 700;

	private const int MaxAutoAcquiresPerScan = 3;

	private const int MaxAutoTargetProbes = 4;

	internal static Verb_LaunchProjectile LauncherVerb(Building_Turret turret)
	{
		Verb obj = ((turret != null) ? turret.AttackVerb : null);
		return (Verb_LaunchProjectile)(object)((obj is Verb_LaunchProjectile) ? obj : null);
	}

	internal static bool IsArc(Verb_LaunchProjectile verb)
	{
		if (verb == null)
		{
			return false;
		}
		if (verb.Projectile?.projectile?.flyOverhead == true)
		{
			return true;
		}
		if (((Verb)verb).verbProps.defaultProjectile?.projectile?.flyOverhead == true)
		{
			return true;
		}
		return !((Verb)verb).verbProps.requireLineOfSight;
	}

	internal static bool TurretCanFire(Building_Turret turret, Thing target, Verb_LaunchProjectile verb, out CrossLevelCombat.GapShot shot)
	{
		return CrossLevelCombat.CanCrossGapFire((Thing)(object)turret, target, verb, out shot);
	}

	internal static bool TryOrder(Building_Turret turret, LocalTargetInfo target, Map targetMap)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Verb_LaunchProjectile val = LauncherVerb(turret);
			if (val == null || !((LocalTargetInfo)(ref target)).IsValid || targetMap == null)
			{
				return false;
			}
			bool flag = IsArc(val);
			CrossLevelCombat.GapShot shot;
			if (flag)
			{
				if (!CrossLevelCombat.CanArcFireAt(((Thing)turret).Map, ((Thing)turret).Position, ((LocalTargetInfo)(ref target)).Cell, targetMap, val, out shot))
				{
					Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_NoArcPath")), LookTargets.op_Implicit((Thing)(object)turret), MessageTypeDefOf.RejectInput, false);
					return false;
				}
			}
			else if (!((LocalTargetInfo)(ref target)).HasThing || !TurretCanFire(turret, ((LocalTargetInfo)(ref target)).Thing, val, out shot))
			{
				Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_NoGapLine")), LookTargets.op_Implicit((Thing)(object)turret), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			Store(turret, target, targetMap, flag, auto: false);
			SoundDef turretAcquireTarget = SoundDefOf.TurretAcquireTarget;
			if (turretAcquireTarget != null)
			{
				SoundStarter.PlayOneShot(turretAcquireTarget, SoundInfo.op_Implicit(new TargetInfo(((Thing)turret).Position, ((Thing)turret).Map, false)));
			}
			Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate(flag ? "AB_MortarTargetSet" : "AB_TurretTargetSet", NamedArgument.op_Implicit(((Entity)turret).LabelShort))), LookTargets.op_Implicit((Thing)(object)turret), MessageTypeDefOf.SilentInput, false);
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "cross level turret order");
			return false;
		}
	}

	private static void Store(Building_Turret turret, LocalTargetInfo target, Map targetMap, bool arc, bool auto)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (entries.Count > 128)
		{
			entries.Clear();
		}
		entries[((Thing)turret).thingIDNumber] = new Entry
		{
			turret = turret,
			target = target,
			targetMap = targetMap,
			arc = arc,
			auto = auto,
			phase = Phase.Warmup,
			nextEventTick = Find.TickManager.TicksGame + WarmupTicks(turret),
			revalidateAt = Find.TickManager.TicksGame + 30
		};
	}

	internal static bool HasOrder(Building_Turret turret, out LocalTargetInfo target, out Map targetMap)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		target = LocalTargetInfo.Invalid;
		targetMap = null;
		if (turret == null || !entries.TryGetValue(((Thing)turret).thingIDNumber, out var value))
		{
			return false;
		}
		target = value.target;
		targetMap = value.targetMap;
		return true;
	}

	internal static void Cancel(Building_Turret turret)
	{
		if (turret != null)
		{
			entries.Remove(((Thing)turret).thingIDNumber);
		}
	}

	internal static void ClearAll()
	{
		entries.Clear();
		nextAutoTry.Clear();
	}

	internal static void TryAutoAcquire(Building_TurretGun turret)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!CrossLevelAutoEngage.AutoEngageEnabled || turret == null || !((Thing)turret).Spawned || entries.ContainsKey(((Thing)turret).thingIDNumber))
			{
				return;
			}
			LocalTargetInfo forcedTarget = ((Building_Turret)turret).ForcedTarget;
			if (((LocalTargetInfo)(ref forcedTarget)).IsValid)
			{
				return;
			}
			int ticksGame = Find.TickManager.TicksGame;
			if (nextAutoTry.TryGetValue(((Thing)turret).thingIDNumber, out var value) && ticksGame < value)
			{
				return;
			}
			Map val = PairedMapOf(((Thing)turret).Map);
			if (val == null)
			{
				Charge((Building_Turret)(object)turret, ticksGame + 2800);
				return;
			}
			Verb_LaunchProjectile val2 = LauncherVerb((Building_Turret)(object)turret);
			if (val2 == null || ((Thing)turret).Faction == null)
			{
				Charge((Building_Turret)(object)turret, ticksGame + 2800);
				return;
			}
			if (val.mapPawns.AllPawnsSpawned.Count == 0)
			{
				Charge((Building_Turret)(object)turret, ticksGame + 120);
				return;
			}
			Pawn val3 = FindAutoTarget((Building_Turret)(object)turret, val2, val);
			if (val3 == null)
			{
				Charge((Building_Turret)(object)turret, ticksGame + 120);
				return;
			}
			Store((Building_Turret)(object)turret, LocalTargetInfo.op_Implicit((Thing)(object)val3), val, IsArc(val2), auto: true);
			ABLog.Dev(((Entity)turret).LabelShort + " auto-acquired a cross-level target (event path).");
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Combat, e, "turret event acquisition");
		}
	}

	private static Map PairedMapOf(Map map)
	{
		LevelComp levelComp = map?.Levels();
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

	internal static void AcquireAuto(Map sky, Map surface)
	{
		int num = 0;
		num += AcquireOnMap(sky, surface, 3);
		AcquireOnMap(surface, sky, 3 - num);
	}

	private static int AcquireOnMap(Map shooterMap, Map targetMap, int budget)
	{
		if (budget <= 0)
		{
			return 0;
		}
		int num = 0;
		int ticksGame = Find.TickManager.TicksGame;
		num += AcquireFromList(shooterMap.listerBuildings.allBuildingsColonist, shooterMap, targetMap, budget, ticksGame);
		if (num < budget)
		{
			num += AcquireFromList(shooterMap.listerBuildings.allBuildingsNonColonist, shooterMap, targetMap, budget - num, ticksGame);
		}
		return num;
	}

	private static int AcquireFromList(List<Building> buildings, Map shooterMap, Map targetMap, int budget, int now)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		for (int i = 0; i < buildings.Count && num < budget; i++)
		{
			Building obj = buildings[i];
			Building_Turret val = (Building_Turret)(object)((obj is Building_Turret) ? obj : null);
			if (val == null || entries.ContainsKey(((Thing)val).thingIDNumber) || (nextAutoTry.TryGetValue(((Thing)val).thingIDNumber, out var value) && now < value))
			{
				continue;
			}
			Verb_LaunchProjectile val2 = LauncherVerb(val);
			if (val2 == null || ((Thing)val).Faction == null)
			{
				continue;
			}
			LocalTargetInfo val3 = val.CurrentTarget;
			if (!((LocalTargetInfo)(ref val3)).IsValid)
			{
				val3 = val.ForcedTarget;
				if (!((LocalTargetInfo)(ref val3)).IsValid && ReadyToFire(val, val2))
				{
					Pawn val4 = FindAutoTarget(val, val2, targetMap);
					if (val4 == null)
					{
						Charge(val, now + 700);
						continue;
					}
					Store(val, LocalTargetInfo.op_Implicit((Thing)(object)val4), targetMap, IsArc(val2), auto: true);
					num++;
					ABLog.Dev(((Entity)val).LabelShort + " auto-acquired a cross-level target.");
					continue;
				}
			}
			Charge(val, now + 700);
		}
		return num;
	}

	private static void Charge(Building_Turret turret, int untilTick)
	{
		if (nextAutoTry.Count > 256)
		{
			nextAutoTry.Clear();
		}
		nextAutoTry[((Thing)turret).thingIDNumber] = untilTick;
	}

	private static Pawn FindAutoTarget(Building_Turret turret, Verb_LaunchProjectile verb, Map targetMap)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		tmpTargets.Clear();
		IReadOnlyList<Pawn> allPawnsSpawned = targetMap.mapPawns.AllPawnsSpawned;
		for (int i = 0; i < allPawnsSpawned.Count; i++)
		{
			Pawn val = allPawnsSpawned[i];
			if (val != null && !val.Dead && !val.Downed && ((Thing)val).Spawned && GenHostility.HostileTo((Thing)(object)val, (Thing)(object)turret))
			{
				tmpTargets.Add(val);
			}
		}
		if (tmpTargets.Count == 0)
		{
			return null;
		}
		IntVec3 origin = ((Thing)turret).Position;
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
			IntVec3 val3 = ((Thing)a).Position - origin;
			int lengthHorizontalSquared = ((IntVec3)(ref val3)).LengthHorizontalSquared;
			val3 = ((Thing)b).Position - origin;
			return lengthHorizontalSquared.CompareTo(((IntVec3)(ref val3)).LengthHorizontalSquared);
		});
		int num = Math.Min(tmpTargets.Count, 4);
		for (int num2 = 0; num2 < num; num2++)
		{
			Pawn val2 = tmpTargets[num2];
			if (IsArc(verb) ? CrossLevelCombat.CanArcFireAt(((Thing)turret).Map, ((Thing)turret).Position, ((Thing)val2).Position, targetMap, verb, out var shot) : TurretCanFire(turret, (Thing)(object)val2, verb, out shot))
			{
				return val2;
			}
		}
		return null;
	}

	internal static void TickPair(Map sky, Map surface, int nowOverride = -1)
	{
		if (entries.Count == 0)
		{
			return;
		}
		int num = ((nowOverride >= 0) ? nowOverride : Find.TickManager.TicksGame);
		tmpDead.Clear();
		foreach (KeyValuePair<int, Entry> entry in entries)
		{
			Entry value = entry.Value;
			Building_Turret turret = value.turret;
			if (turret == null || ((Thing)turret).Destroyed || !((Thing)turret).Spawned)
			{
				tmpDead.Add(entry.Key);
			}
			else
			{
				if (((Thing)turret).Map != sky && ((Thing)turret).Map != surface)
				{
					continue;
				}
				if (num >= value.revalidateAt)
				{
					value.revalidateAt = num + 30;
					if (!Revalidate(value, turret))
					{
						tmpDead.Add(entry.Key);
						continue;
					}
				}
				FaceTarget(turret, value);
				if (num < value.nextEventTick)
				{
					continue;
				}
				switch (value.phase)
				{
				case Phase.Warmup:
					value.phase = Phase.Burst;
					value.burstShotsLeft = Mathf.Max(1, ((Verb)(LauncherVerb(turret)?)).verbProps.burstShotCount ?? 1);
					value.nextEventTick = num;
					goto case Phase.Burst;
				case Phase.Burst:
					switch (TryFireOne(value, turret, num))
					{
					case FireResult.Dead:
						tmpDead.Add(entry.Key);
						break;
					default:
					{
						value.burstShotsLeft--;
						if (value.burstShotsLeft > 0)
						{
							value.nextEventTick = num + Mathf.Max(1, ((Verb)(LauncherVerb(turret)?)).verbProps.ticksBetweenBurstShots ?? 10);
							break;
						}
						value.phase = Phase.Cooldown;
						Verb_LaunchProjectile val = LauncherVerb(turret);
						CompMannable obj = ThingCompUtility.TryGetComp<CompMannable>((Thing)(object)turret);
						Pawn manner = ((obj != null) ? obj.ManningPawn : null);
						value.nextEventTick = num + ((val != null) ? CooldownTicks(turret, val, manner) : 250);
						break;
					}
					case FireResult.Hold:
						break;
					}
					break;
				case Phase.Cooldown:
					value.phase = Phase.Warmup;
					value.nextEventTick = num + WarmupTicks(turret);
					break;
				}
			}
		}
		for (int i = 0; i < tmpDead.Count; i++)
		{
			entries.Remove(tmpDead[i]);
		}
	}

	internal static void DrawVisuals(Map cur)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		if (entries.Count == 0)
		{
			return;
		}
		Map val = cur.Levels()?.lowerMap;
		foreach (KeyValuePair<int, Entry> entry in entries)
		{
			Entry value = entry.Value;
			Building_Turret turret = value.turret;
			if (turret == null || !((Thing)turret).Spawned)
			{
				continue;
			}
			Vector3 val2;
			if (!((LocalTargetInfo)(ref value.target)).HasThing)
			{
				IntVec3 cell = ((LocalTargetInfo)(ref value.target)).Cell;
				val2 = ((IntVec3)(ref cell)).ToVector3Shifted();
			}
			else
			{
				val2 = ((LocalTargetInfo)(ref value.target)).Thing.DrawPos;
			}
			Vector3 val3 = val2;
			if (((Thing)turret).Map == cur)
			{
				if (value.targetMap == val)
				{
					val3 = LevelRenderer.ShiftedBelowDrawPos(val3);
				}
				CrossLevelCombatUI.DrawLine(((Thing)turret).DrawPos, val3);
				if (Find.Selector.IsSelected((object)turret))
				{
					CrossLevelCombatUI.DrawTargetMarker(val3);
				}
			}
			else if (val != null && ((Thing)turret).Map == val && value.targetMap == cur)
			{
				CrossLevelCombatUI.DrawLine(LevelRenderer.ShiftedBelowDrawPos(((Thing)turret).DrawPos), val3);
				if (Find.Selector.IsSelected((object)turret))
				{
					CrossLevelCombatUI.DrawTargetMarker(val3);
				}
			}
		}
	}

	private static bool Revalidate(Entry e, Building_Turret turret)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		if (e.targetMap == null || e.targetMap.Disposed)
		{
			return false;
		}
		if (((LocalTargetInfo)(ref e.target)).HasThing)
		{
			Thing thing = ((LocalTargetInfo)(ref e.target)).Thing;
			if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != e.targetMap)
			{
				return false;
			}
		}
		else if (!GenGrid.InBounds(((LocalTargetInfo)(ref e.target)).Cell, e.targetMap))
		{
			return false;
		}
		LocalTargetInfo currentTarget = turret.CurrentTarget;
		if (((LocalTargetInfo)(ref currentTarget)).IsValid)
		{
			return !e.auto;
		}
		Verb_LaunchProjectile val = LauncherVerb(turret);
		if (val == null)
		{
			return false;
		}
		bool num;
		CrossLevelCombat.GapShot shot;
		if (!e.arc)
		{
			if (!((LocalTargetInfo)(ref e.target)).HasThing)
			{
				goto IL_0141;
			}
			num = TurretCanFire(turret, ((LocalTargetInfo)(ref e.target)).Thing, val, out shot);
		}
		else
		{
			num = CrossLevelCombat.CanArcFireAt(((Thing)turret).Map, ((Thing)turret).Position, ((LocalTargetInfo)(ref e.target)).Cell, e.targetMap, val, out shot);
		}
		if (num)
		{
			return true;
		}
		goto IL_0141;
		IL_0141:
		if (!e.auto)
		{
			Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_GapLineLost", NamedArgument.op_Implicit(((Entity)turret).LabelShort))), LookTargets.op_Implicit((Thing)(object)turret), MessageTypeDefOf.NeutralEvent, false);
		}
		return false;
	}

	private static void FaceTarget(Building_Turret turret, Entry e)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		Building_TurretGun val = (Building_TurretGun)(object)((turret is Building_TurretGun) ? turret : null);
		if (val != null && val.Top != null)
		{
			IntVec3 cell = ((LocalTargetInfo)(ref e.target)).Cell;
			IntVec3 val2 = cell - ((Thing)turret).Position;
			Vector3 val3 = ((IntVec3)(ref val2)).ToVector3();
			if (((Vector3)(ref val3)).sqrMagnitude > 0.01f)
			{
				val.Top.CurRotation = Vector3Utility.AngleFlat(val3);
			}
		}
	}

	private static FireResult TryFireOne(Entry e, Building_Turret turret, int now)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		Verb_LaunchProjectile val = LauncherVerb(turret);
		if (val == null)
		{
			return FireResult.Dead;
		}
		if (!ReadyToFire(turret, val))
		{
			e.nextEventTick = now + 60;
			return FireResult.Hold;
		}
		CompMannable obj = ThingCompUtility.TryGetComp<CompMannable>((Thing)(object)turret);
		Pawn manningPawn = ((obj != null) ? obj.ManningPawn : null);
		if (e.arc)
		{
			if (!CrossLevelCombat.CanArcFireAt(((Thing)turret).Map, ((Thing)turret).Position, ((LocalTargetInfo)(ref e.target)).Cell, e.targetMap, val, out var shot))
			{
				return FireResult.Dead;
			}
			ThingWithComps equipmentSource = ((Verb)val).EquipmentSource;
			if (equipmentSource != null)
			{
				CompChangeableProjectile obj2 = ThingCompUtility.TryGetComp<CompChangeableProjectile>((Thing)(object)equipmentSource);
				if (obj2 != null)
				{
					obj2.Notify_ProjectileLaunched();
				}
			}
			return (!CrossLevelCombat.FireArcShot((Thing)(object)turret, manningPawn, val, e.target, e.targetMap, shot.distance)) ? FireResult.Dead : FireResult.Fired;
		}
		if (!((LocalTargetInfo)(ref e.target)).HasThing)
		{
			return FireResult.Dead;
		}
		ThingWithComps equipmentSource2 = ((Verb)val).EquipmentSource;
		if (equipmentSource2 != null)
		{
			CompChangeableProjectile obj3 = ThingCompUtility.TryGetComp<CompChangeableProjectile>((Thing)(object)equipmentSource2);
			if (obj3 != null)
			{
				obj3.Notify_ProjectileLaunched();
			}
		}
		return (!CrossLevelCombat.Fire((Thing)(object)turret, val, ((LocalTargetInfo)(ref e.target)).Thing)) ? FireResult.Dead : FireResult.Fired;
	}

	private static bool ReadyToFire(Building_Turret turret, Verb_LaunchProjectile verb)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		CompMannable val = ThingCompUtility.TryGetComp<CompMannable>((Thing)(object)turret);
		if (val != null && !val.MannedNow)
		{
			return false;
		}
		CompPowerTrader val2 = ThingCompUtility.TryGetComp<CompPowerTrader>((Thing)(object)turret);
		if (val2 != null && !val2.PowerOn)
		{
			return false;
		}
		if (verb.Projectile == null)
		{
			return false;
		}
		LocalTargetInfo currentTarget = turret.CurrentTarget;
		if (((LocalTargetInfo)(ref currentTarget)).IsValid)
		{
			return false;
		}
		return true;
	}

	private static int WarmupTicks(Building_Turret turret)
	{
		BuildingProperties building = ((Thing)turret).def.building;
		float num = ((building != null) ? ((FloatRange)(ref building.turretBurstWarmupTime)).RandomInRange : 0f);
		return Mathf.Max(1, Mathf.RoundToInt(num * 60f));
	}

	private static int CooldownTicks(Building_Turret turret, Verb_LaunchProjectile verb, Pawn manner)
	{
		float num = ((Thing)turret).def.building?.turretBurstCooldownTime ?? (-1f);
		if (num <= 0f)
		{
			try
			{
				num = ((Verb)verb).verbProps.AdjustedCooldown((Verb)(object)verb, manner);
			}
			catch
			{
				num = ((Verb)verb).verbProps.defaultCooldownTime;
			}
		}
		return Mathf.Max(1, Mathf.RoundToInt(num * 60f));
	}
}
