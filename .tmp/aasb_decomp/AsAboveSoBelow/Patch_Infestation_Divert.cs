using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(IncidentWorker_Infestation), "TryExecuteWorker")]
internal static class Patch_Infestation_Divert
{
	private static void Prefix(IncidentParms parms)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!ThreatDivert.BasementInfestationEligible(parms, out var basement, out var surface))
			{
				return;
			}
			IntVec3 val = default(IntVec3);
			bool flag = InfestationCellFinder.TryFindCell(ref val, surface);
			if (!flag || Rand.Chance(ABMod.Settings.threatDivertChance))
			{
				parms.target = (IIncidentTarget)(object)basement;
				if (!InfestationCellFinder.TryFindCell(ref val, basement) && ThreatDivert.TryFindBasementInfestationCell(basement, out var best))
				{
					parms.infestationLocOverride = best;
				}
				ABLog.Dev("Diverted infestation to basement map " + basement.uniqueID + (flag ? " (roll)" : " (surface has no valid cell)") + (parms.infestationLocOverride.HasValue ? " with relaxed seed cell." : "."));
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Threats, e, "infestation divert");
		}
	}
}
