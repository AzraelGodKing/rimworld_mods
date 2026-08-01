using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
internal static class Patch_Incident_PocketRedirect
{
	private static void Prefix(IncidentWorker __instance, IncidentParms parms)
	{
		try
		{
			if (!ABGuard.On(ABGuard.Threats) || __instance is IncidentWorker_DeepDrillInfestation)
			{
				return;
			}
			IncidentDef def = __instance.def;
			ABIncidentLevelPolicy aBIncidentLevelPolicy = ((def != null) ? ((Def)def).GetModExtension<ABIncidentLevelPolicy>() : null);
			if ((aBIncidentLevelPolicy != null && aBIncidentLevelPolicy.allowOnPocketLevels) || parms == null)
			{
				return;
			}
			IIncidentTarget target = parms.target;
			Map val = (Map)(object)((target is Map) ? target : null);
			if (val == null || val.Disposed)
			{
				return;
			}
			LevelComp levelComp = val.Levels();
			if (levelComp != null && levelComp.level != 0)
			{
				Map groundMap = levelComp.groundMap;
				if (groundMap != null && !groundMap.Disposed && groundMap != val)
				{
					parms.target = (IIncidentTarget)(object)groundMap;
					ABLog.Dev("Redirected incident " + (((Def)(__instance.def?)).defName ?? "unknown") + " from pocket level " + levelComp.level + " to the column surface.");
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Threats, e, "pocket incident redirect");
		}
	}
}
