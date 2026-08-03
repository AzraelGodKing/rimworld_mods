using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(CaravanFormingUtility), "StartFormingCaravan")]
internal static class Patch_StartFormingCaravan_Column
{
	private static readonly List<Pawn> trimmed = new List<Pawn>();

	private static readonly FieldRef<LordJob_FormAndSendCaravan, IntVec3> MeetingPointRef = AccessTools.FieldRefAccess<LordJob_FormAndSendCaravan, IntVec3>("meetingPoint");

	private static bool Prefix(List<Pawn> pawns, List<Pawn> downedPawns)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		trimmed.Clear();
		if (pawns == null || pawns.Count == 0)
		{
			return true;
		}
		Map ground = null;
		for (int i = 0; i < pawns.Count; i++)
		{
			Map mapHeld = ((Thing)pawns[i]).MapHeld;
			if (mapHeld != null)
			{
				LevelComp levelComp = mapHeld.Levels();
				if (levelComp != null && levelComp.level == 0)
				{
					ground = mapHeld;
					break;
				}
			}
		}
		if (ground == null)
		{
			Messages.Message(TaggedString.op_Implicit(Translator.Translate("AB_CaravanNeedsSurfacePawn")), MessageTypeDefOf.RejectInput, false);
			return false;
		}
		if (!CaravanAcrossLevels.Active(ground, out var _))
		{
			return true;
		}
		for (int num = pawns.Count - 1; num >= 0; num--)
		{
			if (((Thing)pawns[num]).MapHeld != ground)
			{
				trimmed.Add(pawns[num]);
				pawns.RemoveAt(num);
			}
		}
		downedPawns?.RemoveAll((Pawn p) => ((Thing)p).MapHeld != ground);
		return true;
	}

	private static void Postfix(List<Pawn> pawns)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		if (trimmed.Count == 0)
		{
			return;
		}
		try
		{
			Lord val = ((pawns.Count > 0) ? LordUtility.GetLord(pawns[0]) : null);
			if (val == null || !(val.LordJob is LordJob_FormAndSendCaravan))
			{
				return;
			}
			Map ground = val.Map;
			IntVec3 dest = IntVec3.Invalid;
			LordJob lordJob = val.LordJob;
			LordJob_FormAndSendCaravan val2 = (LordJob_FormAndSendCaravan)(object)((lordJob is LordJob_FormAndSendCaravan) ? lordJob : null);
			if (val2 != null)
			{
				dest = MeetingPointRef.Invoke(val2);
			}
			int num = 0;
			for (int i = 0; i < trimmed.Count; i++)
			{
				Pawn p = trimmed[i];
				if (!CrossLevelWork.TryStairsJobToward(p, ground, dest, out var job))
				{
					Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_CaravanPawnNoStairs", NamedArgument.op_Implicit(((Entity)p).LabelShort))), LookTargets.op_Implicit((Thing)(object)p), MessageTypeDefOf.RejectInput, false);
					continue;
				}
				Lord target = val;
				ABPendingOrders.Set(p, ground, delegate
				{
					if (target.ownedPawns != null && ground.lordManager.lords.Contains(target) && ((Thing)p).Spawned && !p.Dead && !p.Downed && LordUtility.GetLord(p) == null)
					{
						target.AddPawn(p);
					}
				});
				Pawn_JobTracker jobs = p.jobs;
				if (jobs != null)
				{
					jobs.StartJob(job, (JobCondition)16, (ThinkNode)null, false, true, (ThinkTreeDef)null, (JobTag?)null, false, false, (bool?)null, false, true, false);
				}
				num++;
			}
			if (num > 0)
			{
				Messages.Message(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("AB_CaravanPawnsDescending", NamedArgument.op_Implicit(num))), MessageTypeDefOf.SilentInput, false);
			}
		}
		finally
		{
			trimmed.Clear();
		}
	}
}
