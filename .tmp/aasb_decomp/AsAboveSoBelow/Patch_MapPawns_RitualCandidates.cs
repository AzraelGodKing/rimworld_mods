using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_MapPawns_RitualCandidates
{
	private static void Postfix(Map ___map, ref List<Pawn> __result)
	{
		try
		{
			List<Pawn> list = ABRitualAttendance.TryMergeCandidates(___map, __result);
			if (list != null)
			{
				__result = list;
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "ritual candidate merge");
		}
	}
}
