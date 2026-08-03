using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(RitualObligationTargetFilter), "GetBlockingIssues")]
internal static class Patch_Ritual_BlockingIssues_Reach
{
	private static bool Prefix(RitualObligationTargetFilter __instance, TargetInfo target, RitualRoleAssignments assignments, ref IEnumerable<string> __result)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!ABRitualAttendance.Enabled)
		{
			return true;
		}
		__result = Issues(target, assignments);
		return false;
	}

	private static IEnumerable<string> Issues(TargetInfo target, RitualRoleAssignments assignments)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		foreach (RitualRole role in assignments.AllRolesForReading)
		{
			if (role.mustBeAbleToReachTarget)
			{
				Pawn pawn = assignments.FirstAssignedPawn(role);
				if (pawn != null && !((((Thing)pawn).MapHeld != ((TargetInfo)(ref target)).Map) ? (((TargetInfo)(ref target)).Map != null && CrossLevelWork.NearestUsableStairsCached(pawn, ((TargetInfo)(ref target)).Map)?.CounterpartTowards(((TargetInfo)(ref target)).Map) != null) : ReachabilityUtility.CanReach(pawn, (LocalTargetInfo)target, (PathEndMode)2, DangerUtility.NormalMaxDanger(pawn), false, false, (TraverseMode)0)))
				{
					yield return TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("RitualTargetUnreachable", NamedArgument.op_Implicit(role.LabelCap)));
				}
			}
		}
	}
}
