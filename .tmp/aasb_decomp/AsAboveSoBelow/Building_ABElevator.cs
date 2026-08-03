using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class Building_ABElevator : Building_ABStairs
{
	public override bool AutoLinkOnSpawn => false;

	protected override void SetLink(int delta, Building_ABStairs other)
	{
		if (delta >= 0)
		{
			counterpart = other;
		}
		else
		{
			counterpartSecond = other;
		}
	}

	protected override Building_ABStairs GetLink(int delta)
	{
		return (delta >= 0) ? base.Counterpart : base.SecondCounterpart;
	}

	protected override string LinkLine()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		string text = ((base.Counterpart != null) ? TaggedString.op_Implicit(Translator.Translate("AB_LinkedAbove")) : null);
		string text2 = ((base.SecondCounterpart != null) ? TaggedString.op_Implicit(Translator.Translate("AB_LinkedBelow")) : null);
		if (text == null && text2 == null)
		{
			return TaggedString.op_Implicit(Translator.Translate("AB_NotLinkedLine"));
		}
		if (text != null && text2 != null)
		{
			return text + " " + text2;
		}
		return text ?? text2;
	}

	protected override List<Gizmo> BuildGizmos()
	{
		List<Gizmo> list = new List<Gizmo>();
		AddDirection(list, 1);
		AddDirection(list, -1);
		return list;
	}

	private void AddDirection(List<Gizmo> list, int delta)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		Building_ABStairs link = GetLink(delta);
		bool flag = delta > 0;
		if (link != null)
		{
			Building_ABStairs target = link;
			list.Add((Gizmo)new Command_Action
			{
				defaultLabel = TaggedString.op_Implicit(Translator.Translate(flag ? "AB_ViewAbove" : "AB_ViewBelow")),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("AB_ViewOtherLevelDesc")),
				icon = (Texture)(object)((BuildableDef)((Thing)this).def).uiIcon,
				action = delegate
				{
					LevelCamera.JumpPreservingView(((Thing)target).Map);
				}
			});
			return;
		}
		Command_Action val = new Command_Action
		{
			defaultLabel = TaggedString.op_Implicit(Translator.Translate(flag ? "AB_ExtendUp" : "AB_ExtendDown")),
			defaultDesc = TaggedString.op_Implicit(Translator.Translate(flag ? "AB_ExtendUpDesc" : "AB_ExtendDownDesc")),
			icon = (Texture)(object)((BuildableDef)((Thing)this).def).uiIcon
		};
		int num = ((Thing)this).Map.Level() + delta;
		if (num < -1 || num > 1)
		{
			((Gizmo)val).Disable(TaggedString.op_Implicit(Translator.Translate("AB_LevelCap")));
		}
		else
		{
			int d = delta;
			val.action = delegate
			{
				LongEventHandler.ExecuteWhenFinished((Action)delegate
				{
					EstablishLink(d);
				});
			};
		}
		list.Add((Gizmo)(object)val);
	}

	protected override List<FloatMenuOption> BuildUseOptions(Pawn selPawn)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (!Building_ABStairs.CanBeOrderedToUse(selPawn))
		{
			return list;
		}
		bool reachable = ReachabilityUtility.CanReach(selPawn, LocalTargetInfo.op_Implicit((Thing)(object)this), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0);
		AddRide(list, selPawn, base.Counterpart, TaggedString.op_Implicit(Translator.Translate("AB_GoUp")), reachable);
		AddRide(list, selPawn, base.SecondCounterpart, TaggedString.op_Implicit(Translator.Translate("AB_GoDown")), reachable);
		AddTwoHop(list, selPawn, base.Counterpart, 1, reachable);
		AddTwoHop(list, selPawn, base.SecondCounterpart, -1, reachable);
		if (list.Count == 0)
		{
			list.Add(new FloatMenuOption(TaggedString.op_Implicit(Translator.Translate("AB_GoUp") + " (" + Translator.Translate("AB_NotLinkedShort") + ")"), (Action)null, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		return list;
	}

	private void AddTwoHop(List<FloatMenuOption> list, Pawn selPawn, Building_ABStairs mid, int delta, bool reachable)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (mid is Building_ABElevator building_ABElevator)
		{
			Building_ABStairs link = building_ABElevator.GetLink(delta);
			if (link != null)
			{
				string label = TaggedString.op_Implicit(Translator.Translate((delta > 0) ? "AB_RideToSky" : "AB_RideToBasement"));
				AddRide(list, selPawn, link, label, reachable);
			}
		}
	}

	private void AddRide(List<FloatMenuOption> list, Pawn selPawn, Building_ABStairs destination, string label, bool reachable)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		if (destination == null)
		{
			return;
		}
		if (!reachable)
		{
			list.Add(new FloatMenuOption(TaggedString.op_Implicit(label + " (" + Translator.Translate("AB_NoPath") + ")"), (Action)null, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
			return;
		}
		list.Add(new FloatMenuOption(label, (Action)delegate
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			Job val = JobMaker.MakeJob(ABDefOf.AB_UseStairs, LocalTargetInfo.op_Implicit((Thing)(object)this));
			val.targetC = LocalTargetInfo.op_Implicit((Thing)(object)destination);
			selPawn.jobs.TryTakeOrderedJob(val, (JobTag?)(JobTag)0, false);
		}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
	}
}
