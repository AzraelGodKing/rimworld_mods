using System;
using System.Collections.Generic;
using Verse;

namespace AsAboveSoBelow;

internal static class NeedMigration
{
	internal const int TierNone = 0;

	internal const int TierNormal = 1;

	internal const int TierMentalSafe = 2;

	internal static bool Any;

	private static readonly Dictionary<string, bool> registeredNames = new Dictionary<string, bool>();

	private static readonly Dictionary<Type, int> typeCache = new Dictionary<Type, int>();

	internal static void Register(string fullTypeName, bool mentalSafe)
	{
		if (!GenText.NullOrEmpty(fullTypeName))
		{
			registeredNames[fullTypeName] = mentalSafe;
			typeCache.Clear();
			Any = true;
		}
	}

	internal static int TierOf(Type type)
	{
		if (typeCache.TryGetValue(type, out var value))
		{
			return value;
		}
		value = (registeredNames.TryGetValue(type.FullName, out var value2) ? ((!value2) ? 1 : 2) : 0);
		if (typeCache.Count > 256)
		{
			typeCache.Clear();
		}
		typeCache[type] = value;
		return value;
	}
}
