using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
internal static class Patch_JobGiverWork_CrossLevel
{
	private static void Postfix(JobGiver_Work __instance, Pawn pawn, ref ThinkResult __result)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		if (CrossLevelWork.VirtualScanActive || !ABGuard.On(ABGuard.Movement))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelWork || pawn == null || !((Thing)pawn).Spawned || ((Thing)pawn).Map == null || !pawn.IsColonistPlayerControlled || pawn.Drafted || pawn.Downed || LordUtility.GetLord(pawn) != null)
		{
			return;
		}
		try
		{
			ThinkResult? val;
			if (((ThinkResult)(ref __result)).Job != null)
			{
				if (__instance.emergency || !settings.priorityCrossLevelWork)
				{
					return;
				}
				val = CrossLevelWork.TryMigrateForBetterWork(__instance, pawn, __result);
			}
			else
			{
				val = (__instance.emergency ? CrossLevelWork.TryMigrateForEmergencyWork(__instance, pawn) : CrossLevelWork.TryMigrateForWork(__instance, pawn));
			}
			if (val.HasValue)
			{
				__result = val.Value;
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross level work");
		}
	}
}
