using Verse;

namespace AsAboveSoBelow;

internal static class ColumnTrade
{
	internal static bool Active(Map ground, out LevelComp comp)
	{
		comp = null;
		if (!ABGuard.On(ABGuard.World))
		{
			return false;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.worldIntegration)
		{
			return false;
		}
		comp = ground?.Levels();
		return comp != null && comp.level == 0 && (comp.upperMap != null || comp.lowerMap != null);
	}
}
