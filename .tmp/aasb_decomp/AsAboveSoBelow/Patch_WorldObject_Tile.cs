using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class Patch_WorldObject_Tile
{
	private static void Postfix(WorldObject __instance, ref PlanetTile __result)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (((PlanetTile)(ref __result)).Valid)
		{
			return;
		}
		PocketMapParent val = (PocketMapParent)(object)((__instance is PocketMapParent) ? __instance : null);
		if (val == null || !ABGuard.On(ABGuard.World))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings != null && settings.worldIntegration)
		{
			Map map = ((MapParent)val).Map;
			if (map != null && ColumnWorld.TryGetColumnGround(map, out var ground))
			{
				__result = ground.Tile;
			}
		}
	}
}
