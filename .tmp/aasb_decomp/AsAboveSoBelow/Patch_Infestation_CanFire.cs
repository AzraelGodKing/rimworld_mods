using System;
using HarmonyLib;
using RimWorld;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(IncidentWorker_Infestation), "CanFireNowSub")]
internal static class Patch_Infestation_CanFire
{
	private static void Postfix(IncidentParms parms, ref bool __result)
	{
		try
		{
			if (!__result && ThreatDivert.BasementInfestationEligible(parms, out var _, out var _))
			{
				__result = true;
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Threats, e, "infestation can-fire");
		}
	}
}
