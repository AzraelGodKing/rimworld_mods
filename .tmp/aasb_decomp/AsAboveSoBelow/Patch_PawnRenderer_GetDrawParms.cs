using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
internal static class Patch_PawnRenderer_GetDrawParms
{
	private static void Postfix(ref PawnDrawParms __result)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (LevelRenderer.OffsetActive)
		{
			float belowThingScale = LevelRenderer.BelowThingScale;
			if (belowThingScale < 0.999f)
			{
				ref Matrix4x4 matrix = ref __result.matrix;
				matrix *= Matrix4x4.Scale(new Vector3(belowThingScale, 1f, belowThingScale));
			}
		}
	}
}
