using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch]
internal static class Patch_DropRaid_Divert
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type[] array = new Type[4]
		{
			typeof(PawnsArrivalModeWorker_EdgeDrop),
			typeof(PawnsArrivalModeWorker_CenterDrop),
			typeof(PawnsArrivalModeWorker_RandomDrop),
			typeof(PawnsArrivalModeWorker_EdgeDropGroups)
		};
		foreach (Type type in array)
		{
			MethodBase m = AccessTools.DeclaredMethod(type, "TryResolveRaidSpawnCenter", (Type[])null, (Type[])null);
			if (m != null)
			{
				yield return m;
			}
		}
	}

	private static void Prefix(IncidentParms parms)
	{
		try
		{
			Map val = ThreatDivert.SkyDropTarget(parms);
			if (val != null)
			{
				parms.target = (IIncidentTarget)(object)val;
				ABLog.Dev("Diverted drop raid to sky map " + val.uniqueID + ".");
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Threats, e, "drop raid divert");
		}
	}
}
