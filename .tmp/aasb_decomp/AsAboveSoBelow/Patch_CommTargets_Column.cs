using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(Building_CommsConsole), "GetCommTargets")]
internal static class Patch_CommTargets_Column
{
	private static void Postfix(Pawn myPawn, ref IEnumerable<ICommunicable> __result)
	{
		if (!ABGuard.On(ABGuard.World))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings != null && settings.worldIntegration)
		{
			Map val = ((myPawn != null) ? ((Thing)myPawn).Map : null);
			if (val != null && ColumnWorld.TryGetColumnGround(val, out var ground) && ground.passingShipManager != null && ground.passingShipManager.passingShips.Count != 0)
			{
				__result = __result.Concat(ground.passingShipManager.passingShips.Cast<ICommunicable>());
			}
		}
	}
}
