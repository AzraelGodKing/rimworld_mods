using System;
using System.Runtime.CompilerServices;
using Verse;

namespace AsAboveSoBelow;

public static class LevelExtensions
{
	private static readonly ConditionalWeakTable<Map, LevelComp> cache = new ConditionalWeakTable<Map, LevelComp>();

	public static LevelComp Levels(this Map map)
	{
		if (map == null)
		{
			return null;
		}
		if (cache.TryGetValue(map, out var value))
		{
			return value;
		}
		value = map.GetComponent<LevelComp>();
		if (value != null)
		{
			try
			{
				cache.Add(map, value);
			}
			catch (ArgumentException)
			{
			}
		}
		return value;
	}

	public static int Level(this Map map)
	{
		return map.Levels()?.level ?? 0;
	}

	public static Map UpperMap(this Map map)
	{
		return map.Levels()?.upperMap;
	}

	public static Map LowerMap(this Map map)
	{
		return map.Levels()?.lowerMap;
	}

	public static Map GroundMap(this Map map)
	{
		LevelComp levelComp = map.Levels();
		if (levelComp == null)
		{
			return map;
		}
		if (levelComp.groundMap != null)
		{
			return levelComp.groundMap;
		}
		return (levelComp.level == 0) ? map : null;
	}

	public static LevelComp Controller(this Map map)
	{
		Map val = map.GroundMap();
		return (val != null) ? val.Levels() : map.Levels();
	}

	public static bool ConnectedToOtherLevel(this Map map)
	{
		LevelComp levelComp = map.Levels();
		return levelComp != null && (levelComp.upperMap != null || levelComp.lowerMap != null);
	}

	public static bool TryLinkedLevels(this Map map, out LevelComp comp)
	{
		comp = map.Levels();
		return comp != null && (comp.upperMap != null || comp.lowerMap != null);
	}

	public static bool SameColumn(this Map a, Map b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		if (a == b)
		{
			return true;
		}
		Map val = a.GroundMap();
		return val != null && val == b.GroundMap();
	}

	public static bool IsLevelMap(this Map map)
	{
		LevelComp levelComp = map.Levels();
		return levelComp != null && levelComp.level != 0;
	}
}
