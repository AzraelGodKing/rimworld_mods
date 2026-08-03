using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Targeter), "TargeterUpdate")]
internal static class Patch_Targeter_Update_BelowTarget
{
	private static void Postfix(Targeter __instance)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelOrders)
		{
			return;
		}
		try
		{
			ITargetingSource targetingSource = __instance.targetingSource;
			if (targetingSource != null && CrossLevelCombatUI.TryGetBelowHover(targetingSource, out var _, out var drawAt))
			{
				GenDraw.DrawTargetHighlightWithLayer(drawAt, (AltitudeLayer)39);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "cross level targeter crosshair");
		}
	}
}
