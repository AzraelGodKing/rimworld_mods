using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(MapDrawer), "DrawMapMesh")]
internal static class Patch_MapDrawer_DrawMapMesh
{
	private static void Prefix(Map ___map)
	{
		LevelRenderer.DrawBelowStatic(___map);
	}
}
