using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

internal static class CrossLevelOrders
{
	internal static bool Redirecting;

	internal static Map ResolveTargetMap(Map cur, Vector3 clickPos, out Map below)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		below = cur.Levels()?.lowerMap;
		if (below == null || below.Disposed)
		{
			return cur;
		}
		IntVec3 val = IntVec3Utility.ToIntVec3(clickPos);
		if (!GenGrid.InBounds(val, cur))
		{
			return cur;
		}
		return (cur.terrainGrid.TerrainAt(val) == ABDefOf.AB_OpenAir) ? below : cur;
	}

	internal static bool ShouldRedirect(List<Pawn> selectedPawns, Vector3 clickPos, out Map cur, out Map targetMap, out Pawn pawn)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		cur = Find.CurrentMap;
		targetMap = cur;
		pawn = null;
		if (cur == null || selectedPawns == null || selectedPawns.Count != 1)
		{
			return false;
		}
		Pawn val = selectedPawns[0];
		if (val == null || !((Thing)val).Spawned || !val.IsColonistPlayerControlled || ((Thing)val).Map == null)
		{
			return false;
		}
		targetMap = ResolveTargetMap(cur, clickPos, out var below);
		if (((Thing)val).Map != cur && ((Thing)val).Map != below)
		{
			return false;
		}
		if (((Thing)val).Map == cur && targetMap == cur)
		{
			return false;
		}
		pawn = val;
		return true;
	}

	internal static List<FloatMenuOption> BuildOptions(Pawn pawn, Vector3 clickPos, Map cur, Map targetMap, out FloatMenuContext context)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		List<Pawn> list = new List<Pawn> { pawn };
		bool flag = targetMap != cur;
		bool flag2 = ((Thing)pawn).Map == targetMap;
		if (!flag2 && pawn.Drafted)
		{
			Thing val = FindAttackTargetAt(targetMap, clickPos, pawn);
			if (val != null)
			{
				context = new FloatMenuContext(list, clickPos, cur);
				return MakeAttackOptions(pawn, targetMap, val);
			}
		}
		Building_ABStairs stairs = null;
		ABVirtualPosition.Token token = default(ABVirtualPosition.Token);
		bool flag3 = false;
		if (!flag2)
		{
			stairs = CrossLevelWork.NearestUsableStairsCached(pawn, targetMap);
			Building_ABStairs exit = stairs?.CounterpartTowards(targetMap);
			if (stairs == null || exit == null)
			{
				context = new FloatMenuContext(list, clickPos, cur);
				return NoStairsOptions(targetMap, cur);
			}
			Vector3 val2 = ((flag && cur.Levels()?.lowerMap == targetMap) ? LevelRenderer.ScreenToBelowPos(clickPos) : clickPos);
			IntVec3 val3 = IntVec3Utility.ToIntVec3(val2);
			if (GenGrid.InBounds(val3, targetMap))
			{
				StairRouter.Reroute(pawn, targetMap, val3, ref stairs, ref exit);
			}
			if (!ABVirtualPosition.TrySwap(pawn, targetMap, ((Thing)exit).Position, out token))
			{
				context = new FloatMenuContext(list, clickPos, cur);
				return new List<FloatMenuOption>();
			}
			flag3 = true;
		}
		ABCurrentMapSwap.Token token2 = default(ABCurrentMapSwap.Token);
		bool flag4 = false;
		if (flag)
		{
			flag4 = ABCurrentMapSwap.Swap(targetMap, out token2);
		}
		Redirecting = true;
		List<FloatMenuOption> options;
		try
		{
			options = FloatMenuMakerMap.GetOptions(list, clickPos, ref context);
		}
		finally
		{
			Redirecting = false;
			if (flag4)
			{
				ABCurrentMapSwap.Restore(token2);
			}
			if (flag3)
			{
				ABVirtualPosition.Restore(pawn, token);
			}
		}
		if (!flag2)
		{
			Thing val4 = (pawn.Drafted ? FindAttackTarget(context, pawn) : null);
			if (val4 != null)
			{
				return MakeAttackOptions(pawn, targetMap, val4);
			}
			WrapOptions(options, pawn, targetMap, stairs);
		}
		return options;
	}

	private static Thing FindAttackTarget(FloatMenuContext context, Pawn pawn)
	{
		if (((context != null) ? context.ClickedPawns : null) != null)
		{
			for (int i = 0; i < context.ClickedPawns.Count; i++)
			{
				if (IsAttackTarget((Thing)(object)context.ClickedPawns[i], pawn))
				{
					return (Thing)(object)context.ClickedPawns[i];
				}
			}
		}
		if (((context != null) ? context.ClickedThings : null) != null)
		{
			for (int j = 0; j < context.ClickedThings.Count; j++)
			{
				if (IsAttackTarget(context.ClickedThings[j], pawn))
				{
					return context.ClickedThings[j];
				}
			}
		}
		return null;
	}

	internal static bool IsAttackTarget(Thing t, Pawn pawn)
	{
		if (t == null || (object)t == pawn || !t.Spawned || !t.def.destroyable)
		{
			return false;
		}
		if (t.def.noRightClickDraftAttack && GenHostility.HostileTo(t, Faction.OfPlayer))
		{
			return false;
		}
		if (GenHostility.HostileTo(t, Faction.OfPlayer))
		{
			return true;
		}
		Pawn val = (Pawn)(object)((t is Pawn) ? t : null);
		return val != null && WildManUtility.NonHumanlikeOrWildMan(val);
	}

	private static List<FloatMenuOption> MakeAttackOptions(Pawn pawn, Map targetMap, Thing target)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		FloatMenuOption item = new FloatMenuOption(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("Attack", NamedArgument.op_Implicit(((Entity)target).Label), NamedArgument.op_Implicit(target))), (Action)delegate
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if (!CrossLevelCombat.TryStartCrossGapAttack(pawn, target))
			{
				if (!StairRouter.TryBestToward(pawn, targetMap, target.PositionHeld, out var stairs, out var _))
				{
					stairs = CrossLevelWork.NearestUsableStairsCached(pawn, targetMap);
				}
				RouteThenEngage(pawn, targetMap, stairs, target);
			}
		}, (MenuOptionPriority)6, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0)
		{
			autoTakeable = GenHostility.HostileTo(target, Faction.OfPlayer),
			autoTakeablePriority = 40f
		};
		return new List<FloatMenuOption> { item };
	}

	internal static Thing FindAttackTargetAt(Map map, Vector3 clickPos, Pawn pawn)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (map == null)
		{
			return null;
		}
		Map currentMap = Find.CurrentMap;
		if (currentMap != null && currentMap != map && currentMap.Levels()?.lowerMap == map)
		{
			clickPos = LevelRenderer.ScreenToBelowPos(clickPos);
		}
		IntVec3 val = IntVec3Utility.ToIntVec3(clickPos);
		if (!GenGrid.InBounds(val, map))
		{
			return null;
		}
		Thing val2 = AttackTargetInCell(map, val, pawn);
		if (val2 != null)
		{
			return val2;
		}
		for (int i = 0; i < 8; i++)
		{
			IntVec3 val3 = val + GenAdj.AdjacentCells[i];
			if (GenGrid.InBounds(val3, map))
			{
				Thing val4 = AttackTargetInCell(map, val3, pawn);
				if (val4 != null)
				{
					return val4;
				}
			}
		}
		return null;
	}

	private static Thing AttackTargetInCell(Map map, IntVec3 c, Pawn pawn)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		List<Thing> thingList = GridsUtility.GetThingList(c, map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i] is Pawn && IsAttackTarget(thingList[i], pawn))
			{
				return thingList[i];
			}
		}
		for (int j = 0; j < thingList.Count; j++)
		{
			if (IsAttackTarget(thingList[j], pawn))
			{
				return thingList[j];
			}
		}
		return null;
	}

	private static void RouteThenEngage(Pawn pawn, Map targetMap, Building_ABStairs entry, Thing target)
	{
		RouteThenRun(pawn, targetMap, entry, delegate
		{
			IssueEngageJob(pawn, target, AttackMode.Auto);
		});
	}

	internal static void IssueEngageJob(Pawn pawn, Thing target, AttackMode mode)
	{
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (pawn == null || target == null || !((Thing)pawn).Spawned || pawn.Dead || target.Destroyed || !target.Spawned || target.Map != ((Thing)pawn).Map)
			{
				return;
			}
			Verb val = null;
			if (mode != AttackMode.ForceMelee)
			{
				object obj;
				if (mode != AttackMode.ForceRanged)
				{
					obj = pawn.TryGetAttackVerb(target, !pawn.IsColonist, false);
				}
				else
				{
					Pawn_EquipmentTracker equipment = pawn.equipment;
					if (equipment == null)
					{
						obj = null;
					}
					else
					{
						CompEquippable primaryEq = equipment.PrimaryEq;
						obj = ((primaryEq != null) ? primaryEq.PrimaryVerb : null);
					}
				}
				Verb val2 = (Verb)obj;
				if (val2 != null && !val2.verbProps.IsMeleeAttack)
				{
					val = val2;
				}
			}
			if (val != null)
			{
				CastPositionRequest val3 = new CastPositionRequest
				{
					caster = pawn,
					target = target,
					verb = val,
					wantCoverFromTarget = true,
					maxRangeFromTarget = Mathf.Max(val.EffectiveRange * 0.95f, 1.42f)
				};
				IntVec3 val4 = default(IntVec3);
				if (CastPositionFinder.TryFindCastPosition(val3, ref val4) && ((IntVec3)(ref val4)).IsValid && val4 != ((Thing)pawn).Position)
				{
					Job val5 = JobMaker.MakeJob(JobDefOf.Goto, LocalTargetInfo.op_Implicit(val4));
					val5.playerForced = true;
					pawn.jobs.TryTakeOrderedJob(val5, (JobTag?)(JobTag)0, false);
					Job val6 = JobMaker.MakeJob(JobDefOf.AttackStatic, LocalTargetInfo.op_Implicit(target));
					val6.playerForced = true;
					pawn.jobs.jobQueue.EnqueueLast(val6, (JobTag?)(JobTag)0);
				}
				else
				{
					Job val7 = JobMaker.MakeJob(JobDefOf.AttackStatic, LocalTargetInfo.op_Implicit(target));
					val7.playerForced = true;
					pawn.jobs.TryTakeOrderedJob(val7, (JobTag?)(JobTag)0, false);
				}
			}
			else
			{
				Job val8 = JobMaker.MakeJob(JobDefOf.AttackMelee, LocalTargetInfo.op_Implicit(target));
				Pawn val9 = (Pawn)(object)((target is Pawn) ? target : null);
				if (val9 != null)
				{
					val8.killIncappedTarget = val9.Downed;
				}
				val8.playerForced = true;
				pawn.jobs.TryTakeOrderedJob(val8, (JobTag?)(JobTag)0, false);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross level engage");
		}
	}

	private static List<FloatMenuOption> NoStairsOptions(Map targetMap, Map cur)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		string text = TaggedString.op_Implicit((targetMap.Level() > cur.Level()) ? Translator.Translate("AB_LevelAbove") : Translator.Translate("AB_LevelBelow"));
		return new List<FloatMenuOption>
		{
			new FloatMenuOption(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_NoStairsToLevel", NamedArgument.op_Implicit(text))), (Action)null, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0)
		};
	}

	private static void WrapOptions(List<FloatMenuOption> options, Pawn pawn, Map targetMap, Building_ABStairs entry)
	{
		if (options == null)
		{
			return;
		}
		for (int i = 0; i < options.Count; i++)
		{
			FloatMenuOption val = options[i];
			if (val != null && !val.Disabled)
			{
				Action original = val.action;
				val.action = delegate
				{
					RouteThenRun(pawn, targetMap, entry, original);
				};
			}
		}
	}

	internal static void RouteThenRun(Pawn pawn, Map targetMap, Building_ABStairs entry, Action original)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (pawn == null || original == null || !((Thing)pawn).Spawned || pawn.Dead)
			{
				return;
			}
			if (((Thing)pawn).Map == targetMap)
			{
				original();
			}
			else if (entry != null && ((Thing)entry).Spawned)
			{
				Building_ABStairs building_ABStairs = entry.CounterpartTowards(targetMap);
				if (building_ABStairs != null)
				{
					Job val = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)entry));
					val.targetC = LocalTargetInfo.op_Implicit((Thing)(object)building_ABStairs);
					val.playerForced = true;
					ABPendingOrders.Set(pawn, targetMap, original);
					pawn.jobs.TryTakeOrderedJob(val, (JobTag?)(JobTag)0, false);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross level order routing");
		}
	}
}
