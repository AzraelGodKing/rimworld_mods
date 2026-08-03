using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
internal static class Patch_GetFood_CrossLevel
{
	private static void Postfix(Pawn pawn, ref Job __result)
	{
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelNeeds)
		{
			return;
		}
		try
		{
			if (__result != null || (!NeedsCross.EligibleColonist(pawn) && !NeedsCross.EligiblePetForFood(pawn)) || pawn.needs?.food == null)
			{
				return;
			}
			LevelComp levelComp = ((Thing)pawn).Map.Levels();
			if (levelComp != null && (levelComp.upperMap != null || levelComp.lowerMap != null) && !NeedsCross.OnCooldown(NeedsCross.FoodNext, pawn))
			{
				if (TryFoodTowards(pawn, levelComp.upperMap, out var job) || TryFoodTowards(pawn, levelComp.lowerMap, out job))
				{
					__result = job;
				}
				else
				{
					NeedsCross.Charge(NeedsCross.FoodNext, pawn);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level food");
		}
	}

	private static bool TryFoodTowards(Pawn pawn, Map target, out Job job)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		job = null;
		if (!CrossLevelWork.TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			return false;
		}
		Thing source = null;
		ThingDef val = default(ThingDef);
		if (!ABVirtualPosition.WithPawnAt(pawn, target, ((Thing)exit).Position, () => FoodUtility.TryFindBestFoodSourceFor(pawn, pawn, false, ref source, ref val, true, false, false, false, true, false, false, false, false, false, false, (FoodPreferability)0)))
		{
			return false;
		}
		StairRouter.Reroute(pawn, target, StairRouter.DestHint(source, target), ref stairs, ref exit);
		job = CrossLevelWork.MakeStairsJob(stairs, exit);
		return true;
	}
}
