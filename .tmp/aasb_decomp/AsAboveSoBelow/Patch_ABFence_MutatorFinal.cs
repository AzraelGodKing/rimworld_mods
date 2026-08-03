using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(GenStep_MutatorFinal), "Generate")]
internal static class Patch_ABFence_MutatorFinal
{
	private static void Finalizer(Map map)
	{
		ABLandmarkPlacement.EndScope(map);
	}
}
