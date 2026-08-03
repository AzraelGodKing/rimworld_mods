using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Building_TurretGun), "TryFindNewTarget")]
internal static class Patch_TurretGun_TryFindNewTarget
{
	private static void Postfix(Building_TurretGun __instance, LocalTargetInfo __result)
	{
		if (!((LocalTargetInfo)(ref __result)).IsValid && ABGuard.On(ABGuard.Combat))
		{
			CrossLevelTurret.TryAutoAcquire(__instance);
		}
	}
}
