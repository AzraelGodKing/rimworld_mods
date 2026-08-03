using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(DynamicDrawManager), "DrawSilhouettes")]
internal static class Patch_DynamicDrawManager_DrawSilhouettes
{
	private static bool Prefix()
	{
		return !LevelRenderer.OffsetActive;
	}
}
