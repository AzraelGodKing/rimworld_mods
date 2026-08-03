using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Skyfaller), "SpawnSetup")]
internal static class Patch_Skyfaller_Transit
{
	private static void Postfix(Skyfaller __instance, Map map, bool respawningAfterLoad)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (respawningAfterLoad || PodTransit.InTransfer || !LevelComp.AnySkyLevels || !ABGuard.On(ABGuard.Transit))
			{
				return;
			}
			ABSettings settings = ABMod.Settings;
			if (settings == null || !settings.podTransit || !PodTransit.IsTransitDef(((Thing)__instance).def))
			{
				return;
			}
			LevelComp levelComp = map.Levels();
			if (levelComp == null)
			{
				return;
			}
			if (levelComp.level == 0)
			{
				Map upperMap = levelComp.upperMap;
				if (upperMap != null && !upperMap.Disposed && __instance.ticksToImpact >= 40 && PodTransit.GapOpen(upperMap, GenAdj.OccupiedRect((Thing)(object)__instance)))
				{
					map.GetComponent<PodTransitComp>()?.RegisterLift(__instance);
				}
			}
			else if (levelComp.level == 1 && levelComp.lowerMap != null && !levelComp.lowerMap.Disposed && PodTransit.GapOpen(map, GenAdj.OccupiedRect((Thing)(object)__instance)))
			{
				int at = Math.Max(1, Math.Min(22, __instance.ticksToImpact / 2));
				map.GetComponent<PodTransitComp>()?.RegisterDescent(__instance, at);
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Transit, e, "skyfaller transit spawn hook");
		}
	}
}
