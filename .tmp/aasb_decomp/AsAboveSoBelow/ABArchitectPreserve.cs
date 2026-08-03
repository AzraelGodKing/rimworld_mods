using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

internal static class ABArchitectPreserve
{
	private static readonly FieldRef<MainTabWindow_Architect, List<ArchitectCategoryTab>> PanelsRef = AccessTools.FieldRefAccess<MainTabWindow_Architect, List<ArchitectCategoryTab>>("desPanelsCached");

	public static void Around(Action jump)
	{
		bool flag = false;
		DesignationCategoryDef val = null;
		Designator val2 = null;
		try
		{
			flag = Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect;
			if (flag)
			{
				MainTabWindow tabWindow = MainButtonDefOf.Architect.TabWindow;
				MainTabWindow_Architect val3 = (MainTabWindow_Architect)(object)((tabWindow is MainTabWindow_Architect) ? tabWindow : null);
				if (val3 != null)
				{
					val = val3.selectedDesPanel?.def;
				}
			}
			DesignatorManager designatorManager = Find.DesignatorManager;
			val2 = ((designatorManager != null) ? designatorManager.SelectedDesignator : null);
		}
		catch
		{
		}
		jump();
		try
		{
			if (!flag || Find.MainTabsRoot.OpenTab != MainButtonDefOf.Architect)
			{
				return;
			}
			MainTabWindow tabWindow2 = MainButtonDefOf.Architect.TabWindow;
			MainTabWindow_Architect val4 = (MainTabWindow_Architect)(object)((tabWindow2 is MainTabWindow_Architect) ? tabWindow2 : null);
			if (val4 == null)
			{
				return;
			}
			if (val != null)
			{
				List<ArchitectCategoryTab> list = PanelsRef.Invoke(val4);
				if (list != null)
				{
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i]?.def == val)
						{
							val4.selectedDesPanel = list[i];
							break;
						}
					}
				}
			}
			if (val2 != null)
			{
				Find.DesignatorManager.Select(val2);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "architect state preserve");
		}
	}
}
