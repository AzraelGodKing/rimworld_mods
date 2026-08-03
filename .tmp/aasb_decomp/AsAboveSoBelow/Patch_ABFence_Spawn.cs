using System;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[]
{
	typeof(Thing),
	typeof(IntVec3),
	typeof(Map),
	typeof(Rot4),
	typeof(WipeMode),
	typeof(bool),
	typeof(bool)
})]
internal static class Patch_ABFence_Spawn
{
	private unsafe static bool Prefix(Thing newThing, IntVec3 loc, Map map, Rot4 rot, ref Thing __result)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (!ABLandmarkPlacement.Fenced(map))
		{
			return true;
		}
		IntVec2 size = (IntVec2)(((_003F?)newThing?.def?.size) ?? IntVec2.One);
		if (!ABLandmarkPlacement.RectOk(loc, rot, size))
		{
			string[] obj = new string[5]
			{
				"Landmark fence: vetoed off-plateau spawn of ",
				((Def)(newThing?.def?)).defName ?? "?",
				" at ",
				null,
				null
			};
			IntVec3 val = loc;
			obj[3] = ((object)(*(IntVec3*)(&val))/*cast due to .constrained prefix*/).ToString();
			obj[4] = ".";
			ABLog.Dev(string.Concat(obj));
			__result = null;
			return false;
		}
		return true;
	}
}
