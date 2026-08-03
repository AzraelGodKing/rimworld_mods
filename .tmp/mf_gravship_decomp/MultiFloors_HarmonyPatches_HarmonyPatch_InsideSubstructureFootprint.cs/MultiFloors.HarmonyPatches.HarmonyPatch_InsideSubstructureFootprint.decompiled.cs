using HarmonyLib;
using RimWorld;
using Verse;

namespace MultiFloors.HarmonyPatches;

[HotSwappable]
[HarmonyPatch]
[HarmonyPatchCategory("MultiFloors.Gravship")]
public static class HarmonyPatch_InsideSubstructureFootprint
{
	[HarmonyPatch(typeof(GravshipUtility), "InsideFootprint")]
	[HarmonyPrefix]
	public static bool CheckSubGravEngine(ref bool __result, Map map)
	{
		if (ModsConfig.OdysseyActive && map.listerThings.ThingsOfDef(MiscDefOfs.MF_SubGravEngine).Count > 0)
		{
			__result = true;
			return false;
		}
		return true;
	}
}
