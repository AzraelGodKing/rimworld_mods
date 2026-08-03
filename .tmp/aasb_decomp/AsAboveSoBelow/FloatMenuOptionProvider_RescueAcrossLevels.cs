using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

public class FloatMenuOptionProvider_RescueAcrossLevels : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
	{
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Invalid comparison between Unknown and I4
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Expected O, but got Unknown
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
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
		if (taker == null)
		{
			return null;
		}
		Map map = ((Thing)taker).Map;
		if (map == null || !map.ConnectedToOtherLevel() || ((Thing)clickedPawn).Map != map)
		{
			return null;
		}
		if (!HealthAIUtility.CanRescueNow(taker, clickedPawn, true))
		{
			return null;
		}
		if (clickedPawn.mindState != null && clickedPawn.mindState.WillJoinColonyIfRescued)
		{
			return null;
		}
		if (clickedPawn.IsPrisonerOfColony || clickedPawn.IsSlaveOfColony || clickedPawn.IsColonyMech)
		{
			return null;
		}
		if (((Thing)clickedPawn).Faction != null && FactionUtility.HostileTo(((Thing)clickedPawn).Faction, Faction.OfPlayer))
		{
			return null;
		}
		BreastfeedFailReason? val = default(BreastfeedFailReason?);
		if (ChildcareUtility.CanSuckle(clickedPawn, ref val))
		{
			return null;
		}
		if (!HealthAIUtility.ShouldSeekMedicalRest(clickedPawn) && clickedPawn.ageTracker.CurLifeStage.alwaysDowned)
		{
			return null;
		}
		Pawn_PlayerSettings playerSettings = clickedPawn.playerSettings;
		if (playerSettings != null && (int)playerSettings.medCare == 0)
		{
			return null;
		}
		try
		{
			if (RestUtility.FindBedFor(clickedPawn, taker, false, false, (GuestStatus?)null) != null || RestUtility.FindBedFor(clickedPawn, taker, false, true, (GuestStatus?)null) != null)
			{
				return null;
			}
			Building_ABStairs exit;
			Building_ABStairs stairs = TakePawnAcrossLevels.FindStairsTowardBed(taker, clickedPawn, null, out exit);
			if (stairs == null || exit == null)
			{
				return null;
			}
			string text = TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate((((Thing)exit).Map.Level() > map.Level()) ? "AB_RescueUpTo" : "AB_RescueDownTo", NamedArgument.op_Implicit(((Entity)clickedPawn).LabelShort)));
			FloatMenuOption val2 = new FloatMenuOption(text, (Action)delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				Job val3 = JobMaker.MakeJob(ABDefOf.AB_RescueAcrossLevels, LocalTargetInfo.op_Implicit((Thing)(object)clickedPawn), LocalTargetInfo.op_Implicit((Thing)(object)stairs));
				val3.targetC = LocalTargetInfo.op_Implicit((Thing)(object)exit);
				val3.count = 1;
				val3.playerForced = true;
				taker.jobs.TryTakeOrderedJob(val3, (JobTag?)(JobTag)0, false);
				PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, (KnowledgeAmount)6);
			}, (MenuOptionPriority)8, (Action<Rect>)null, (Thing)(object)clickedPawn, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
			return FloatMenuUtility.DecoratePrioritizedTask(val2, taker, LocalTargetInfo.op_Implicit((Thing)(object)clickedPawn), "ReservedBy", (ReservationLayerDef)null);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level rescue option");
			return null;
		}
	}
}
