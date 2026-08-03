using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(DesignationManager), "AddDesignation")]
internal static class Patch_Designation_WorkChanged
{
	private static void Postfix(DesignationManager __instance)
	{
		LevelWorkSummary.Notify_WorkChanged(__instance.map);
	}
}
