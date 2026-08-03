using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(RitualBehaviorWorker), "TryExecuteOn")]
internal static class Patch_Ritual_TryExecuteOn_Gather
{
	private static bool Prefix(RitualBehaviorWorker __instance, TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		if (ABRitualAttendance.Executing || !ABRitualAttendance.Enabled)
		{
			return true;
		}
		try
		{
			Map map = ((TargetInfo)(ref target)).Map;
			if (map == null || assignments == null)
			{
				return true;
			}
			bool flag = false;
			List<Pawn> participants = assignments.Participants;
			for (int i = 0; i < participants.Count; i++)
			{
				Pawn val = participants[i];
				if (val != null && !val.Dead && ((Thing)val).Spawned && ((Thing)val).MapHeld != map)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
			if (ABRitualAttendance.BeginGather(__instance, target, organizer, ritual, obligation, assignments, playerForced))
			{
				return false;
			}
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "ritual gather intercept");
			return true;
		}
	}
}
