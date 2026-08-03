using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
internal static class Patch_PawnRenderer_ParallelGetPreRenderResults
{
	private static void Prefix(ref bool disableCache)
	{
		if (LevelRenderer.OffsetActive && LevelRenderer.BelowThingScale < 0.999f)
		{
			disableCache = true;
		}
	}
}
