using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class CrossLevelTargeting
{
	private static readonly FieldRef<Targeter, Action<LocalTargetInfo>> ActionRef = AccessTools.FieldRefAccess<Targeter, Action<LocalTargetInfo>>("action");

	private static readonly List<Pawn> tmpPawns = new List<Pawn>();

	internal static bool TryHandle(Targeter targeter)
	{
		ITargetingSource targetingSource = targeter.targetingSource;
		Action<LocalTargetInfo> action = ActionRef.Invoke(targeter);
		tmpPawns.Clear();
		AttackMode mode;
		if (targetingSource != null)
		{
			Thing caster = targetingSource.Caster;
			Building_Turret val = (Building_Turret)(object)((caster is Building_Turret) ? caster : null);
			if (val != null)
			{
				return TryHandleTurret(val);
			}
			Pawn casterPawn = targetingSource.CasterPawn;
			if (casterPawn == null)
			{
				return false;
			}
			if (!CrossLevelCombat.IsEquippedGunVerb(targetingSource, casterPawn, out var _))
			{
				return TryHandleGenericSource(targetingSource, casterPawn);
			}
			mode = AttackMode.ForceRanged;
			tmpPawns.Add(casterPawn);
		}
		else
		{
			if (action == null)
			{
				return false;
			}
			mode = AttackMode.ForceMelee;
			CollectSelectedDraftedPawns(tmpPawns);
		}
		if (tmpPawns.Count == 0)
		{
			return false;
		}
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			return false;
		}
		ITargetingSource obj = ((targetingSource is Verb) ? targetingSource : null);
		TargetingParameters tp = ((obj != null) ? ((Verb)obj).targetParams : null) ?? CrossLevelCombatUI.AttackAnyParams;
		Thing val2 = ResolveAttackTarget(currentMap, tmpPawns[0], tp, targetingSource);
		if (val2 == null)
		{
			return false;
		}
		Map mapHeld = val2.MapHeld;
		if (mapHeld == null)
		{
			return false;
		}
		bool flag = mapHeld != currentMap;
		for (int i = 0; i < tmpPawns.Count; i++)
		{
			if (flag)
			{
				break;
			}
			if (((Thing)tmpPawns[i]).Map != mapHeld)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		for (int j = 0; j < tmpPawns.Count; j++)
		{
			EngageCrossLevel(tmpPawns[j], val2, mode);
		}
		targeter.StopTargeting();
		return true;
	}

	private static bool TryHandleTurret(Building_Turret turret)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		Verb_LaunchProjectile val = CrossLevelTurret.LauncherVerb(turret);
		if (val == null)
		{
			return false;
		}
		if (!BelowSelection.TryGetBelowView(out var sky, out var lower) || sky != Find.CurrentMap)
		{
			return false;
		}
		Vector3 val2 = UI.MouseMapPosition();
		IntVec3 val3 = IntVec3Utility.ToIntVec3(val2);
		if (!GenGrid.InBounds(val3, sky))
		{
			return false;
		}
		bool flag = sky.terrainGrid.TerrainAt(val3) == ABDefOf.AB_OpenAir;
		if (((Thing)turret).Map == sky && flag)
		{
			List<Thing> list = BelowSelection.SelectablesUnderMouse(sky, lower, val2);
			LocalTargetInfo target;
			if (list.Count > 0)
			{
				target = LocalTargetInfo.op_Implicit(list[0]);
			}
			else
			{
				if (!CrossLevelTurret.IsArc(val))
				{
					return false;
				}
				IntVec3 val4 = IntVec3Utility.ToIntVec3(LevelRenderer.ScreenToBelowPos(val2));
				if (!GenGrid.InBounds(val4, lower))
				{
					return false;
				}
				target = LocalTargetInfo.op_Implicit(val4);
			}
			return CrossLevelTurret.TryOrder(turret, target, lower);
		}
		if (((Thing)turret).Map == lower && !flag)
		{
			foreach (LocalTargetInfo item in GenUI.TargetsAtMouse(TargetingParameters.ForAttackAny(), true, (ITargetingSource)null))
			{
				LocalTargetInfo current = item;
				if (((LocalTargetInfo)(ref current)).Thing != null)
				{
					return CrossLevelTurret.TryOrder(turret, LocalTargetInfo.op_Implicit(((LocalTargetInfo)(ref current)).Thing), sky);
				}
			}
			if (CrossLevelTurret.IsArc(val))
			{
				return CrossLevelTurret.TryOrder(turret, LocalTargetInfo.op_Implicit(val3), sky);
			}
			return false;
		}
		return false;
	}

	private static bool TryHandleGenericSource(ITargetingSource source, Pawn caster)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		if (!BelowSelection.TryGetBelowView(out var sky, out var lower) || sky != Find.CurrentMap)
		{
			return false;
		}
		foreach (LocalTargetInfo item in GenUI.TargetsAtMouse(source.targetParams, true, (ITargetingSource)null))
		{
			LocalTargetInfo current = item;
			if (((LocalTargetInfo)(ref current)).Thing != null)
			{
				return false;
			}
		}
		List<Thing> list = BelowSelection.SelectablesUnderMouse(sky, lower, UI.MouseMapPosition());
		Thing val = null;
		for (int i = 0; i < list.Count; i++)
		{
			if (source.targetParams != null && source.targetParams.CanTarget(new TargetInfo(list[i]), source))
			{
				val = list[i];
				break;
			}
		}
		if (val == null)
		{
			return false;
		}
		Map mapHeld = val.MapHeld;
		if (((Thing)caster).MapHeld == mapHeld)
		{
			source.OrderForceTarget(new LocalTargetInfo(val));
			return true;
		}
		Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairsCached(caster, mapHeld);
		if (building_ABStairs == null || building_ABStairs.CounterpartTowards(mapHeld) == null)
		{
			Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_NoStairsToLevel", NamedArgument.op_Implicit(Translator.Translate("AB_LevelBelow")))), LookTargets.op_Implicit((Thing)(object)caster), MessageTypeDefOf.RejectInput, false);
			return true;
		}
		Thing target = val;
		ITargetingSource src = source;
		CrossLevelOrders.RouteThenRun(caster, mapHeld, building_ABStairs, delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			src.OrderForceTarget(new LocalTargetInfo(target));
		});
		return true;
	}

	private static void CollectSelectedDraftedPawns(List<Pawn> into)
	{
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			object obj = selectedObjects[i];
			Pawn val = (Pawn)((obj is Pawn) ? obj : null);
			if (val != null && val.IsColonistPlayerControlled && val.Drafted && !val.Downed && ((Thing)val).Spawned)
			{
				into.Add(val);
			}
		}
	}

	private static Thing ResolveAttackTarget(Map cur, Pawn sample, TargetingParameters tp, ITargetingSource source)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (BelowSelection.TryGetBelowView(out var sky, out var lower) && sky == cur)
		{
			List<Thing> list = BelowSelection.SelectablesUnderMouse(sky, lower, UI.MouseMapPosition());
			for (int i = 0; i < list.Count; i++)
			{
				Thing val = list[i];
				if ((object)val != sample && tp != null && tp.CanTarget(new TargetInfo(val), source))
				{
					return val;
				}
			}
		}
		foreach (LocalTargetInfo item in GenUI.TargetsAtMouse(tp, true, (ITargetingSource)null))
		{
			LocalTargetInfo current = item;
			if (((LocalTargetInfo)(ref current)).Thing != null && (object)((LocalTargetInfo)(ref current)).Thing != sample)
			{
				return ((LocalTargetInfo)(ref current)).Thing;
			}
		}
		return null;
	}

	private static void EngageCrossLevel(Pawn pawn, Thing target, AttackMode mode)
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (pawn == null || target == null)
		{
			return;
		}
		Map mapHeld = target.MapHeld;
		if (mapHeld == null)
		{
			return;
		}
		if (((Thing)pawn).Map == mapHeld)
		{
			CrossLevelOrders.IssueEngageJob(pawn, target, mode);
		}
		else
		{
			if (mode != AttackMode.ForceMelee && CrossLevelCombat.TryStartCrossGapAttack(pawn, target))
			{
				return;
			}
			Building_ABStairs building_ABStairs = CrossLevelWork.NearestUsableStairsCached(pawn, mapHeld);
			if (building_ABStairs == null || building_ABStairs.CounterpartTowards(mapHeld) == null)
			{
				string text = TaggedString.op_Implicit((mapHeld.Level() > ((Thing)pawn).Map.Level()) ? Translator.Translate("AB_LevelAbove") : Translator.Translate("AB_LevelBelow"));
				Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_NoStairsToLevel", NamedArgument.op_Implicit(text))), LookTargets.op_Implicit((Thing)(object)pawn), MessageTypeDefOf.RejectInput, false);
			}
			else
			{
				CrossLevelOrders.RouteThenRun(pawn, mapHeld, building_ABStairs, delegate
				{
					CrossLevelOrders.IssueEngageJob(pawn, target, mode);
				});
			}
		}
	}
}
