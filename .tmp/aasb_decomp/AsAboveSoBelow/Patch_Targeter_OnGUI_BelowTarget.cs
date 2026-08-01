using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Targeter), "TargeterOnGUI")]
internal static class Patch_Targeter_OnGUI_BelowTarget
{
	private static bool Prefix(Targeter __instance)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Ui))
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
			ITargetingSource targetingSource = __instance.targetingSource;
			if (targetingSource == null)
			{
				return true;
			}
			if (!CrossLevelCombatUI.TryGetBelowHover(targetingSource, out var target, out var _))
			{
				return true;
			}
			targetingSource.OnGUI(target);
			if (targetingSource.GetVerb?.verbProps?.mouseTargetingText != null)
			{
				Widgets.MouseAttachedLabel(targetingSource.GetVerb.verbProps.mouseTargetingText, 0f, 0f, (Color?)null);
			}
			return false;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "cross level targeter cursor");
			return true;
		}
	}
}
