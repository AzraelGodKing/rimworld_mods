using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class FloatMenuOptionProvider_CaptureAcrossLevels : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Expected O, but got Unknown
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelNeeds)
		{
			return null;
		}
		Pawn taker = context.FirstSelectedPawn;
		if (taker != null)
		{
			Pawn victim = (Pawn)(object)((clickedThing is Pawn) ? clickedThing : null);
			if (victim != null)
			{
				Map map = ((Thing)taker).Map;
				if (map == null || !map.ConnectedToOtherLevel() || ((Thing)victim).Map != map)
				{
					return null;
				}
				if (!victim.Downed || victim.Dead || victim.IsPrisonerOfColony)
				{
					return null;
				}
				if (((Thing)victim).Faction != null && !FactionUtility.HostileTo(((Thing)victim).Faction, Faction.OfPlayer))
				{
					return null;
				}
				if (!ReachabilityUtility.CanReach(taker, LocalTargetInfo.op_Implicit((Thing)(object)victim), (PathEndMode)1, (Danger)3, false, false, (TraverseMode)0))
				{
					return null;
				}
				try
				{
					if (RestUtility.FindBedFor(victim, taker, false, false, (GuestStatus?)(GuestStatus)1) != null)
					{
						return null;
					}
					Building_ABStairs exit;
					Building_ABStairs stairs = TakePawnAcrossLevels.FindStairsTowardBed(taker, victim, (GuestStatus)1, out exit);
					if (stairs == null || exit == null)
					{
						return null;
					}
					string text = TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate((((Thing)exit).Map.Level() > map.Level()) ? "AB_CaptureUpTo" : "AB_CaptureDownTo", NamedArgument.op_Implicit(((Entity)victim).LabelShort)));
					FloatMenuOption val = new FloatMenuOption(text, (Action)delegate
					{
						//IL_0011: Unknown result type (might be due to invalid IL or missing references)
						//IL_001c: Unknown result type (might be due to invalid IL or missing references)
						//IL_002e: Unknown result type (might be due to invalid IL or missing references)
						//IL_0033: Unknown result type (might be due to invalid IL or missing references)
						Job val2 = JobMaker.MakeJob(ABDefOf.AB_CaptureAcrossLevels, LocalTargetInfo.op_Implicit((Thing)(object)victim), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
						val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
						val2.count = 1;
						val2.playerForced = true;
						taker.jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)0, false);
					}, (MenuOptionPriority)5, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
					return FloatMenuUtility.DecoratePrioritizedTask(val, taker, LocalTargetInfo.op_Implicit((Thing)(object)victim), "ReservedBy", (ReservationLayerDef)null);
				}
				catch (Exception e)
				{
					ABGuard.Disable(ABGuard.Logistics, e, "cross level capture option");
					return null;
				}
			}
		}
		return null;
	}
}
