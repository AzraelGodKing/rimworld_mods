using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(SelectionDrawer), "DrawSelectionBracketFor")]
internal static class Patch_SelectionDrawer_DrawSelectionBracketFor
{
	private static bool Prefix(object obj, out bool __state)
	{
		__state = false;
		if (ABGuard.On(ABGuard.Ui))
		{
			Thing val = (Thing)((obj is Thing) ? obj : null);
			if (val != null)
			{
				if (!BelowSelection.IsBelowSelected(val, out var sky, out var lower))
				{
					return true;
				}
				if (!BelowSelection.VisibleFromAbove(val, sky, lower))
				{
					return false;
				}
				LevelRenderer.EnsureBelowTransform();
				LevelRenderer.OffsetActive = true;
				__state = true;
				return true;
			}
		}
		return true;
	}

	private static void Postfix(bool __state)
	{
		if (__state)
		{
			LevelRenderer.OffsetActive = false;
		}
	}
}
