using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
internal static class Patch_DynamicDrawManager_DrawDynamicThings
{
	private static void Postfix(Map ___map)
	{
		LevelRenderer.DrawBelowDynamic(___map);
	}
}
