using System;
using HarmonyLib;
using RimWorld.QuestGen;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_Map_IsPlayerHome_Column
{
	private static void Postfix(Map __instance, ref bool __result)
	{
		if (__result || !ABGuard.On(ABGuard.Social))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelSocial || Patch_AlertsScope_VanillaHome.Depth > 0 || QuestGen.Working)
		{
			return;
		}
		try
		{
			if (ColumnWorld.TryGetColumnGround(__instance, out var ground))
			{
				__result = ground.IsPlayerHome;
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Social, e, "column player-home identity");
		}
	}
}
