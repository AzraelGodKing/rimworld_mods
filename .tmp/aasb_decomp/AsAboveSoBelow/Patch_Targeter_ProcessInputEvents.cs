using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Targeter), "ProcessInputEvents")]
internal static class Patch_Targeter_ProcessInputEvents
{
	private static bool Prefix(Targeter __instance)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 0 || Event.current.button != 0 || !__instance.IsTargeting)
		{
			return true;
		}
		if (!ABGuard.On(ABGuard.Movement))
		{
			return true;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelOrders)
		{
			return true;
		}
		try
		{
			if (CrossLevelTargeting.TryHandle(__instance))
			{
				Event.current.Use();
				return false;
			}
			return true;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross level targeting");
			return true;
		}
	}
}
