using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Selector), "SelectUnderMouse")]
internal static class Patch_Selector_SelectUnderMouse
{
	private static bool Prefix(Selector __instance)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (!ABGuard.On(ABGuard.Ui))
		{
			return true;
		}
		try
		{
			if (!BelowSelection.TryGetBelowView(out var sky, out var lower))
			{
				return true;
			}
			if (!GenGrid.InBounds(UI.MouseCell(), sky))
			{
				return true;
			}
			Vector3 clickPos = UI.MouseMapPosition();
			if (BelowSelection.SkyBlocksSelection(sky, clickPos))
			{
				return true;
			}
			List<Thing> list = BelowSelection.SelectablesUnderMouse(sky, lower, clickPos);
			if (list.Count == 0)
			{
				return true;
			}
			BelowSelection.HandleBelowClick(__instance, list);
			return false;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "below select under mouse");
			return true;
		}
	}
}
