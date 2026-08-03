using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
internal static class Patch_FloatMenuMakerMap_GetOptions_CrossLevel
{
	private static bool Prefix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (CrossLevelOrders.Redirecting || !ABGuard.On(ABGuard.Movement))
		{
			return true;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelOrders)
		{
			return true;
		}
		if (!CrossLevelOrders.ShouldRedirect(selectedPawns, clickPos, out var cur, out var targetMap, out var pawn))
		{
			return true;
		}
		try
		{
			if (pawn.Drafted && ((Thing)pawn).Map == targetMap && targetMap != cur && CrossLevelOrders.FindAttackTargetAt(targetMap, clickPos, pawn) == null)
			{
				ABBelowGotoDrag.Start(pawn);
				context = new FloatMenuContext(new List<Pawn> { pawn }, clickPos, cur);
				__result = new List<FloatMenuOption>();
				return false;
			}
			__result = CrossLevelOrders.BuildOptions(pawn, clickPos, cur, targetMap, out context);
			return false;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross level float menu");
			return true;
		}
	}
}
