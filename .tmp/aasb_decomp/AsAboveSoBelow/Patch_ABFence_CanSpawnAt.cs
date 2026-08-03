using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(GenSpawn), "CanSpawnAt")]
internal static class Patch_ABFence_CanSpawnAt
{
	private static bool Prefix(ThingDef def, IntVec3 loc, Map map, ref bool __result)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!ABLandmarkPlacement.Fenced(map))
		{
			return true;
		}
		if (!ABLandmarkPlacement.RectOk(loc, Rot4.North, def?.size ?? IntVec2.One))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
