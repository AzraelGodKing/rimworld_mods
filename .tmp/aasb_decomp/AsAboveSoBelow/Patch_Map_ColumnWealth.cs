using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_Map_ColumnWealth
{
	private static void Postfix(Map __instance, ref float __result)
	{
		if (!ABGuard.On(ABGuard.World))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings != null && settings.columnWealth && Find.Storyteller?.difficulty != null && !Find.Storyteller.difficulty.fixedWealthMode)
		{
			LevelComp levelComp = __instance.Levels();
			if (levelComp != null && levelComp.level == 0)
			{
				__result += ChildWealth(levelComp.upperMap) + ChildWealth(levelComp.lowerMap);
			}
		}
	}

	private static float ChildWealth(Map child)
	{
		if (child == null || child.Disposed || child.wealthWatcher == null)
		{
			return 0f;
		}
		return child.wealthWatcher.WealthItems + child.wealthWatcher.WealthBuildings * 0.5f + child.wealthWatcher.WealthPawns;
	}
}
