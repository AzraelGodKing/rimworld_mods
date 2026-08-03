using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(TerrainGrid), "SetTerrain")]
internal static class Patch_ABFence_SetTerrain
{
	private static bool Prefix(IntVec3 c, Map ___map)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!ABLandmarkPlacement.Fenced(___map))
		{
			return true;
		}
		return ABLandmarkPlacement.CellOk(c);
	}
}
