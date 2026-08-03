using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(ThoughtWorker_PsychicBondProximity), "NearPsychicBondedPerson")]
internal static class Patch_PsychicBondProximity_CrossLevel
{
	private static void Postfix(Pawn pawn, Hediff_PsychicBond bondHediff, ref bool __result)
	{
		if (__result || !ABGuard.On(ABGuard.Social))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelSocial)
		{
			return;
		}
		try
		{
			Thing obj = ((HediffWithTarget)(bondHediff?)).target;
			Pawn val = (Pawn)(object)((obj is Pawn) ? obj : null);
			if (val != null && pawn != null)
			{
				Map mapHeld = ((Thing)pawn).MapHeld;
				Map mapHeld2 = ((Thing)val).MapHeld;
				if (mapHeld != null && mapHeld2 != null && mapHeld != mapHeld2 && mapHeld.SameColumn(mapHeld2))
				{
					__result = true;
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Social, e, "cross-level psychic bond proximity");
		}
	}
}
