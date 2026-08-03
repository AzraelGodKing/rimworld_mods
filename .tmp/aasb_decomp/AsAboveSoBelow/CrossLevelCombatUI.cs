using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class CrossLevelCombatUI
{
	internal static readonly HashSet<Pawn> ActiveShooters = new HashSet<Pawn>();

	private static readonly List<Pawn> tmpStale = new List<Pawn>();

	internal static readonly TargetingParameters AttackAnyParams = TargetingParameters.ForAttackAny();

	internal static void DrawEngagementVisuals(Map cur)
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		if (ActiveShooters.Count == 0)
		{
			return;
		}
		Map val = cur.Levels()?.lowerMap;
		tmpStale.Clear();
		foreach (Pawn activeShooter in ActiveShooters)
		{
			if (activeShooter == null || activeShooter.Dead || !((Thing)activeShooter).Spawned || !(activeShooter.jobs?.curDriver is JobDriver_ABCrossLevelAttack { Target: var target } jobDriver_ABCrossLevelAttack))
			{
				tmpStale.Add(activeShooter);
			}
			else
			{
				if (target == null || target.Destroyed || !target.Spawned)
				{
					continue;
				}
				if (((Thing)activeShooter).Map == cur)
				{
					Vector3 val2 = ((target.MapHeld == val) ? LevelRenderer.ShiftedBelowDrawPos(target.DrawPos) : target.DrawPos);
					DrawLine(((Thing)activeShooter).DrawPos, val2);
					if (jobDriver_ABCrossLevelAttack.Warming)
					{
						GenDraw.DrawAimPie((Thing)(object)activeShooter, new LocalTargetInfo(target), (int)((float)jobDriver_ABCrossLevelAttack.WarmupTicksLeft * 0.5f), 0.2f);
					}
					if (Find.Selector.IsSelected((object)activeShooter))
					{
						DrawTargetMarker(val2);
					}
				}
				else if (val != null && ((Thing)activeShooter).Map == val)
				{
					DrawLine(LevelRenderer.ShiftedBelowDrawPos(((Thing)activeShooter).DrawPos), target.DrawPos);
					if (Find.Selector.IsSelected((object)activeShooter))
					{
						DrawTargetMarker(target.DrawPos);
					}
				}
			}
		}
		for (int i = 0; i < tmpStale.Count; i++)
		{
			ActiveShooters.Remove(tmpStale[i]);
		}
	}

	internal static void DrawLine(Vector3 a, Vector3 b)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		b.y = (a.y = Altitudes.AltitudeFor((AltitudeLayer)39));
		GenDraw.DrawLineBetween(a, b);
	}

	internal static void DrawTargetMarker(Vector3 at)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		GenDraw.DrawTargetHighlightWithLayer(at, (AltitudeLayer)39);
	}

	internal static bool TryGetBelowHover(ITargetingSource source, out LocalTargetInfo target, out Vector3 drawAt)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		target = LocalTargetInfo.Invalid;
		drawAt = Vector3.zero;
		if (source == null)
		{
			return false;
		}
		if (!BelowSelection.TryGetBelowView(out var sky, out var lower) || sky != Find.CurrentMap)
		{
			return false;
		}
		Vector3 val = UI.MouseMapPosition();
		Thing caster = source.Caster;
		Building_Turret val2 = (Building_Turret)(object)((caster is Building_Turret) ? caster : null);
		if (val2 != null)
		{
			Verb_LaunchProjectile val3 = CrossLevelTurret.LauncherVerb(val2);
			IntVec3 val4 = IntVec3Utility.ToIntVec3(val);
			if (val3 == null || !GenGrid.InBounds(val4, sky))
			{
				return false;
			}
			bool flag = CrossLevelTurret.IsArc(val3);
			bool flag2 = sky.terrainGrid.TerrainAt(val4) == ABDefOf.AB_OpenAir;
			CrossLevelCombat.GapShot shot;
			if (((Thing)val2).Map == sky && flag2)
			{
				List<Thing> list = BelowSelection.SelectablesUnderMouse(sky, lower, val);
				if (list.Count > 0 && !flag && CrossLevelTurret.TurretCanFire(val2, list[0], val3, out shot))
				{
					target = LocalTargetInfo.op_Implicit(list[0]);
					drawAt = LevelRenderer.ShiftedBelowDrawPos(list[0].DrawPos);
					return true;
				}
				if (flag)
				{
					IntVec3 val5 = IntVec3Utility.ToIntVec3(LevelRenderer.ScreenToBelowPos(val));
					if (GenGrid.InBounds(val5, lower) && CrossLevelCombat.CanArcFireAt(sky, ((Thing)val2).Position, val5, lower, val3, out shot))
					{
						target = LocalTargetInfo.op_Implicit(val5);
						drawAt = LevelRenderer.ShiftedBelowDrawPos(((IntVec3)(ref val5)).ToVector3Shifted());
						return true;
					}
				}
			}
			else if (((Thing)val2).Map == lower && !flag2)
			{
				if (flag && CrossLevelCombat.CanArcFireAt(lower, ((Thing)val2).Position, val4, sky, val3, out shot))
				{
					target = LocalTargetInfo.op_Implicit(val4);
					drawAt = ((IntVec3)(ref val4)).ToVector3Shifted();
					return true;
				}
				if (!flag)
				{
					foreach (LocalTargetInfo item in GenUI.TargetsAtMouse(AttackAnyParams, true, (ITargetingSource)null))
					{
						LocalTargetInfo current = item;
						if (((LocalTargetInfo)(ref current)).Thing != null && CrossLevelTurret.TurretCanFire(val2, ((LocalTargetInfo)(ref current)).Thing, val3, out shot))
						{
							target = LocalTargetInfo.op_Implicit(((LocalTargetInfo)(ref current)).Thing);
							drawAt = ((LocalTargetInfo)(ref current)).Thing.DrawPos;
							return true;
						}
					}
				}
			}
			return false;
		}
		Pawn casterPawn = source.CasterPawn;
		if (casterPawn == null)
		{
			return false;
		}
		TargetingParameters val6 = source.targetParams ?? AttackAnyParams;
		foreach (LocalTargetInfo item2 in GenUI.TargetsAtMouse(val6, true, (ITargetingSource)null))
		{
			LocalTargetInfo current2 = item2;
			if (((LocalTargetInfo)(ref current2)).Thing != null)
			{
				return false;
			}
		}
		List<Thing> list2 = BelowSelection.SelectablesUnderMouse(sky, lower, val);
		for (int i = 0; i < list2.Count; i++)
		{
			if ((object)list2[i] != casterPawn && source.targetParams != null && source.targetParams.CanTarget(new TargetInfo(list2[i]), source))
			{
				target = LocalTargetInfo.op_Implicit(list2[i]);
				drawAt = LevelRenderer.ShiftedBelowDrawPos(list2[i].DrawPos);
				return true;
			}
		}
		return false;
	}
}
