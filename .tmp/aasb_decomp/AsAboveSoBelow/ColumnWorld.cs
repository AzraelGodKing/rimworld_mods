using Verse;

namespace AsAboveSoBelow;

internal static class ColumnWorld
{
	internal static bool TryGetColumnGround(Map map, out Map ground)
	{
		ground = null;
		if (map == null || map.Disposed)
		{
			return false;
		}
		LevelComp levelComp = map.Levels();
		if (levelComp == null || levelComp.level == 0)
		{
			return false;
		}
		ground = levelComp.groundMap;
		return ground != null && !ground.Disposed;
	}
}
