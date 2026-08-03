using HarmonyLib;
using RimWorld;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(RitualBehaviorWorker), "CanStartRitualNow")]
internal static class Patch_Ritual_CanStartNow_Scope
{
	private static void Prefix()
	{
		ABRitualAttendance.EnterScope();
	}

	private static void Finalizer()
	{
		ABRitualAttendance.ExitScope();
	}
}
