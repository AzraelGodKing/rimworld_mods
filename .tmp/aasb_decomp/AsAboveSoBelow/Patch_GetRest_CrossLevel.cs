using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
internal static class Patch_GetRest_CrossLevel
{
	private static void Postfix(Pawn pawn, ref Job __result)
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
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
			if ((__result != null && ((LocalTargetInfo)(ref __result.targetA)).HasThing) || !NeedsCross.EligibleColonist(pawn))
			{
				return;
			}
			LevelComp levelComp = ((Thing)pawn).Map.Levels();
			if (levelComp == null || (levelComp.upperMap == null && levelComp.lowerMap == null))
			{
				return;
			}
			Pawn_Ownership ownership = pawn.ownership;
			Building_Bed val = ((ownership != null) ? ownership.OwnedBed : null);
			if (val != null && ((Thing)val).Spawned && ((Thing)val).Map != ((Thing)pawn).Map)
			{
				Map val2 = ((Thing)pawn).Map.GroundMap();
				if (val2 != null && val2 == ((Thing)val).Map.GroundMap())
				{
					Map val3 = ((((Thing)val).Map.Level() > levelComp.level) ? levelComp.upperMap : levelComp.lowerMap);
					IntVec3 dest = ((((Thing)val).Map == val3) ? ((Thing)val).Position : IntVec3.Invalid);
					if (CrossLevelWork.TryStairsJobToward(pawn, val3, dest, out var job))
					{
						__result = job;
					}
				}
			}
			else if (val == null && !NeedsCross.OnCooldown(NeedsCross.RestNext, pawn))
			{
				if (TryFreeBedTowards(pawn, levelComp.upperMap, out var job2) || TryFreeBedTowards(pawn, levelComp.lowerMap, out job2))
				{
					__result = job2;
				}
				else
				{
					NeedsCross.Charge(NeedsCross.RestNext, pawn);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "cross level rest");
		}
	}

	private static bool TryFreeBedTowards(Pawn pawn, Map target, out Job job)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		job = null;
		if (!CrossLevelWork.TryResolveStairs(pawn, target, out var stairs, out var exit))
		{
			return false;
		}
		Building_Bed found = null;
		if (!ABVirtualPosition.WithPawnAt(pawn, target, ((Thing)exit).Position, () => (found = RestUtility.FindBedFor(pawn)) != null))
		{
			return false;
		}
		StairRouter.Reroute(pawn, target, StairRouter.DestHint((Thing)(object)found, target), ref stairs, ref exit);
		job = CrossLevelWork.MakeStairsJob(stairs, exit);
		return true;
	}
}
