using System;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(MapDrawer), "MapMeshDirty", new Type[]
{
	typeof(IntVec3),
	typeof(ulong),
	typeof(bool),
	typeof(bool)
})]
internal static class Patch_MapDrawer_MapMeshDirty
{
	private static void Postfix(Map ___map, IntVec3 loc, ulong dirtyFlags)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		LevelSync.OnLowerMeshDirty(___map, loc, dirtyFlags);
	}
}
