using HarmonyLib;
using RimWorld;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Dialog_BeginRitual), "CreateRitualRoleAssignments")]
internal static class Patch_Ritual_CreateAssignments_Scope
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
