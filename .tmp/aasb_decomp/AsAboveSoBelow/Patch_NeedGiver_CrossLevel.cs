using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(ThinkNode_JobGiver), "TryIssueJobPackage")]
[HarmonyPriority(200)]
internal static class Patch_NeedGiver_CrossLevel
{
	private const int RetryCooldownTicks = 600;

	private static bool inVirtualScan;

	private static readonly ABPawnCooldown retryCooldown = new ABPawnCooldown();

	private static void Postfix(ThinkNode_JobGiver __instance, Pawn pawn, ref ThinkResult __result)
	{
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedMigration.Any || ((ThinkResult)(ref __result)).Job != null || inVirtualScan || CrossLevelWork.VirtualScanActive)
		{
			return;
		}
		int num = NeedMigration.TierOf(((object)__instance).GetType());
		if (num == 0 || !ABGuard.On(ABGuard.Logistics))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelNeeds || pawn == null || !((Thing)pawn).Spawned || pawn.Downed || pawn.Drafted || LordUtility.GetLord(pawn) != null)
		{
			return;
		}
		if (pawn.InMentalState)
		{
			if (num != 2 || ((Thing)pawn).Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike)
			{
				return;
			}
		}
		else if (!pawn.IsColonistPlayerControlled)
		{
			return;
		}
		if (!((Thing)pawn).Map.TryLinkedLevels(out var comp))
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
			Job val = TryTowards(__instance, pawn, comp.upperMap) ?? TryTowards(__instance, pawn, comp.lowerMap);
			if (val != null)
			{
				__result = new ThinkResult(val, (ThinkNode)(object)__instance, (JobTag?)(JobTag)5, false);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "modded need migration");
		}
	}

	private static Job TryTowards(ThinkNode_JobGiver giver, Pawn pawn, Map target)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
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
		ABLog.Dev("Need migration: " + ((Entity)pawn).LabelShort + " heading to level " + target.Level() + " for " + ((object)giver).GetType().Name + (pawn.InMentalState ? " (mental state)." : "."));
		return CrossLevelWork.MakeStairsJob(stairs, exit);
	}
}
