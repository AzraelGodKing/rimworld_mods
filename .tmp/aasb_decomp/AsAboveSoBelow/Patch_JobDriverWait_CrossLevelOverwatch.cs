using HarmonyLib;
using Verse;
using Verse.AI;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
internal static class Patch_JobDriverWait_CrossLevelOverwatch
{
	private static void Postfix(JobDriver_Wait __instance)
	{
		if (ABGuard.On(ABGuard.Combat))
		{
			Pawn pawn = ((JobDriver)__instance).pawn;
			if (pawn != null && !(pawn.stances?.curStance is Stance_Busy))
			{
				CrossLevelAutoEngage.TryOverwatchCrossFire(pawn);
			}
		}
	}
}
