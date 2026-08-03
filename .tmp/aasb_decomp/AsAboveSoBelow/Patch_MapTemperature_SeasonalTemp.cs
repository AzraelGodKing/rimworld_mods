using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_MapTemperature_SeasonalTemp
{
	private static void Postfix(Map ___map, ref float __result)
	{
		Map val = ClimateSync.SkyGroundOrNull(___map);
		if (val != null)
		{
			__result = val.mapTemperature.SeasonalTemp;
		}
	}
}
