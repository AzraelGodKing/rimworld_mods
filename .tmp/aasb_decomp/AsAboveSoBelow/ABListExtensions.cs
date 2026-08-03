using System.Collections.Generic;

namespace AsAboveSoBelow;

internal static class ABListExtensions
{
	internal static List<T> ToListSafe<T>(this IEnumerable<T> source)
	{
		List<T> list = new List<T>();
		if (source == null)
		{
			return list;
		}
		foreach (T item in source)
		{
			list.Add(item);
		}
		return list;
	}
}
