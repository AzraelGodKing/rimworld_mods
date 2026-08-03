using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class FloatMenuOptionProvider_TakePrisonerAcrossLevels : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected O, but got Unknown
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelPrisoners)
		{
			return null;
		}
		Pawn taker = context.FirstSelectedPawn;
		if (taker == null || clickedPawn == null || !clickedPawn.IsPrisonerOfColony)
		{
			return null;
		}
		Map map = ((Thing)taker).Map;
		if (map == null || !map.ConnectedToOtherLevel() || ((Thing)clickedPawn).Map != map)
		{
			return null;
		}
		if (clickedPawn.InAggroMentalState || ForbidUtility.IsForbidden((Thing)(object)clickedPawn, taker) || !ReservationUtility.CanReserveAndReach(taker, LocalTargetInfo.op_Implicit((Thing)(object)clickedPawn), (PathEndMode)1, (Danger)3, 1, -1, (ReservationLayerDef)null, false))
		{
			return null;
		}
		try
		{
			Building_ABStairs exit;
			Building_ABStairs stairs = TakePawnAcrossLevels.FindStairsTowardBed(taker, clickedPawn, (GuestStatus)1, out exit);
			if (stairs == null || exit == null)
			{
				return null;
			}
			string text = TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate((((Thing)exit).Map.Level() > map.Level()) ? "AB_TakePrisonerUpTo" : "AB_TakePrisonerDownTo", NamedArgument.op_Implicit(((Entity)clickedPawn).LabelShort)));
			FloatMenuOption val = new FloatMenuOption(text, (Action)delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				Job val2 = JobMaker.MakeJob(ABDefOf.AB_TakePrisonerAcrossLevels, LocalTargetInfo.op_Implicit((Thing)(object)clickedPawn), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
				val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
				val2.count = 1;
				val2.playerForced = true;
				taker.jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)0, false);
			}, (MenuOptionPriority)5, (Action<Rect>)null, (Thing)(object)clickedPawn, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
			return FloatMenuUtility.DecoratePrioritizedTask(val, taker, LocalTargetInfo.op_Implicit((Thing)(object)clickedPawn), "ReservedBy", (ReservationLayerDef)null);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level take prisoner option");
			return null;
		}
	}
}
