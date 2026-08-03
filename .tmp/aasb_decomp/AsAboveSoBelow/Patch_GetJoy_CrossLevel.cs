using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(JobGiver_GetJoy), "TryGiveJob")]
[HarmonyPriority(200)]
internal static class Patch_GetJoy_CrossLevel
{
	private const int RetryCooldownTicks = 600;

	private static bool inVirtualScan;

	private static readonly ABPawnCooldown retryCooldown = new ABPawnCooldown();

	private static void Postfix(Pawn pawn, ref Job __result, JobGiver_GetJoy __instance)
	{
		if (__result != null || inVirtualScan || !ABGuard.On(ABGuard.Logistics))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelNeeds || pawn == null || !((Thing)pawn).Spawned || pawn.Downed || pawn.Drafted || !pawn.IsColonistPlayerControlled || LordUtility.GetLord(pawn) != null || !((Thing)pawn).Map.TryLinkedLevels(out var comp))
		{
			return;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (!retryCooldown.Ready(pawn, ticksGame))
		{
			return;
		}
		retryCooldown.ChargeUntil(pawn, ticksGame + 600);
		try
		{
			__result = TryTowards(__instance, pawn, comp.upperMap) ?? TryTowards(__instance, pawn, comp.lowerMap);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level recreation");
		}
	}

	private static Job TryTowards(JobGiver_GetJoy giver, Pawn pawn, Map target)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || target.Disposed)
		{
			return null;
		}
		if (!CrossLevelWork.TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			return null;
		}
		Job probe = null;
		inVirtualScan = true;
		bool flag;
		try
		{
			flag = ABVirtualPosition.WithPawnAt(pawn, target, ((Thing)exit).Position, delegate
			{
				//IL_000f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0015: Unknown result type (might be due to invalid IL or missing references)
				//IL_0016: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				ThinkResult val = ((ThinkNode)giver).TryIssueJobPackage(pawn, default(JobIssueParams));
				return (probe = ((ThinkResult)(ref val)).Job) != null;
			});
		}
		finally
		{
			inVirtualScan = false;
		}
		if (!flag)
		{
			return null;
		}
		StairRouter.Reroute(pawn, target, StairRouter.DestHint(probe, target), ref stairs, ref exit);
		ABLog.Dev("Joy migration: " + ((Entity)pawn).LabelShort + " heading to level " + target.Level() + " for recreation.");
		return CrossLevelWork.MakeStairsJob(stairs, exit);
	}
}
