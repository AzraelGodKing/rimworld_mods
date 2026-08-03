using System;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
internal static class Patch_MechCommandRange_CrossLevel
{
	private static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		if (__result || !ABGuard.On(ABGuard.Movement))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.crossLevelOrders)
		{
			return;
		}
		try
		{
			Pawn val = ((mech != null) ? MechanitorUtility.GetOverseer(mech) : null);
			if (val?.mechanitor != null)
			{
				Map mapHeld = ((Thing)mech).MapHeld;
				Map mapHeld2 = ((Thing)val).MapHeld;
				if (mapHeld != null && mapHeld2 != null && mapHeld != mapHeld2 && mapHeld.SameColumn(mapHeld2))
				{
					__result = val.mechanitor.CanCommandTo(target);
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Movement, e, "cross-level mech command range");
		}
	}
}
