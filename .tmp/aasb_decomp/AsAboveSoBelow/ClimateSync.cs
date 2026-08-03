using Verse;

namespace AsAboveSoBelow;

internal static class ClimateSync
{
	public static Map SkyGroundOrNull(Map map)
	{
		if (!LevelComp.AnySkyLevels)
		{
			return null;
		}
		if (!ABGuard.On(ABGuard.Climate) || map == null)
		{
			return null;
		}
		LevelComp levelComp = map.Levels();
		if (levelComp == null || levelComp.level != 1)
		{
			return null;
		}
		Map val = levelComp.lowerMap ?? levelComp.groundMap;
		if (val == null || val.Disposed)
		{
			return null;
		}
		return val;
	}
}
