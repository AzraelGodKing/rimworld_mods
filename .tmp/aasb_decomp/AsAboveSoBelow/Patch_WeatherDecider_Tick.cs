using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(WeatherDecider), "WeatherDeciderTick")]
internal static class Patch_WeatherDecider_Tick
{
	private static readonly FieldRef<WeatherDecider, Map> MapRef = AccessTools.FieldRefAccess<WeatherDecider, Map>("map");

	private static bool Prefix(WeatherDecider __instance)
	{
		if (!ABGuard.On(ABGuard.Weather))
		{
			return true;
		}
		Map val = MapRef.Invoke(__instance);
		return val == null || val.Level() == 0;
	}
}
