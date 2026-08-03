using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
internal static class Patch_PlaySettings_LevelButtons
{
	private static string upTip;

	private static string downTip;

	private static string UpTip => upTip ?? (upTip = TaggedString.op_Implicit(Translator.Translate("AB_ViewAbove") + "\n" + Translator.Translate("AB_LevelWidgetTip")));

	private static string DownTip => downTip ?? (downTip = TaggedString.op_Implicit(Translator.Translate("AB_ViewBelow") + "\n" + Translator.Translate("AB_LevelWidgetTip")));

	private static void Postfix(WidgetRow row, bool worldView)
	{
		if (worldView || row == null || !ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.showLevelWidget)
		{
			return;
		}
		try
		{
			Map currentMap = Find.CurrentMap;
			if (currentMap == null)
			{
				return;
			}
			Map val = currentMap.UpperMap();
			Map val2 = currentMap.LowerMap();
			if (val == null && val2 == null)
			{
				return;
			}
			bool pushed = ABDeclutterCompat.PushUnsuppressed();
			try
			{
				if (val != null && !val.Disposed && (Object)(object)ABIcons.UpStairs != (Object)null && row.ButtonIcon(ABIcons.UpStairs, UpTip, (Color?)null, (Color?)null, (Color?)null, true, -1f))
				{
					LevelCamera.JumpPreservingView(val);
				}
				if (val2 != null && !val2.Disposed && (Object)(object)ABIcons.DownStairs != (Object)null && row.ButtonIcon(ABIcons.DownStairs, DownTip, (Color?)null, (Color?)null, (Color?)null, true, -1f))
				{
					LevelCamera.JumpPreservingView(val2);
				}
			}
			finally
			{
				ABDeclutterCompat.Pop(pushed);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "level view buttons");
		}
	}
}
