using System;
using Verse;

namespace AsAboveSoBelow;

internal static class ABLandmarkPlacement
{
	private static Map scopeMap;

	private static bool[] mask;

	private static int maskW;

	private static int maskH;

	internal static void BeginScope(Map map, bool[] plateauMask)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		scopeMap = map;
		mask = plateauMask;
		maskW = map.Size.x;
		maskH = map.Size.z;
	}

	internal static void EndScope(Map map)
	{
		if (scopeMap == map)
		{
			scopeMap = null;
			mask = null;
		}
	}

	internal static bool Fenced(Map map)
	{
		return scopeMap != null && map == scopeMap && mask != null && ABGuard.On(ABGuard.LevelGen);
	}

	internal static bool CellOk(IntVec3 c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (c.x < 0 || c.z < 0 || c.x >= maskW || c.z >= maskH)
		{
			return false;
		}
		return mask[c.z * maskW + c.x];
	}

	internal unsafe static bool RectOk(IntVec3 root, Rot4 rot, IntVec2 size)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		CellRect val = GenAdj.OccupiedRect(root, rot, size);
		Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
		try
		{
			while (((Enumerator)(ref enumerator)).MoveNext())
			{
				IntVec3 current = ((Enumerator)(ref enumerator)).Current;
				if (!CellOk(current))
				{
					return false;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		return true;
	}
}
