using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class FloatMenuOptionProvider_HaulAcrossLevels : FloatMenuOptionProvider
{
	protected override bool Drafted => false;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return null;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelHauling)
		{
			return null;
		}
		Pawn pawn = context.FirstSelectedPawn;
		if (pawn == null || clickedThing == null || !clickedThing.def.EverHaulable)
		{
			return null;
		}
		Map map = ((Thing)pawn).Map;
		if (map == null || !map.ConnectedToOtherLevel() || clickedThing.Map != map)
		{
			return null;
		}
		if (ForbidUtility.IsForbidden(clickedThing, pawn) || !ReachabilityUtility.CanReach(pawn, LocalTargetInfo.op_Implicit(clickedThing), (PathEndMode)3, (Danger)3, false, false, (TraverseMode)0))
		{
			return null;
		}
		try
		{
			Building_ABStairs stairs;
			Map target = CrossLevelHaul.TargetLevelFor(pawn, clickedThing, out stairs);
			if (target == null || stairs == null)
			{
				return null;
			}
			string text = TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate((target.Level() > map.Level()) ? "AB_HaulUpTo" : "AB_HaulDownTo", NamedArgument.op_Implicit(((Entity)clickedThing).LabelShort)));
			FloatMenuOption val = new FloatMenuOption(text, (Action)delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_003e: Unknown result type (might be due to invalid IL or missing references)
				Job val2 = JobMaker.MakeJob(ABDefOf.AB_HaulAcrossLevels, LocalTargetInfo.op_Implicit(clickedThing), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
				val2.targetC = LocalTargetInfo.op_Implicit((Thing)(object)stairs.CounterpartTowards(target));
				val2.count = Mathf.Min(clickedThing.stackCount, pawn.carryTracker.MaxStackSpaceEver(clickedThing.def));
				val2.playerForced = true;
				pawn.jobs.TryTakeOrderedJob(val2, (JobTag?)(JobTag)0, false);
			}, (MenuOptionPriority)5, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
			return FloatMenuUtility.DecoratePrioritizedTask(val, pawn, LocalTargetInfo.op_Implicit(clickedThing), "ReservedBy", (ReservationLayerDef)null);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level haul option");
			return null;
		}
	}
}
