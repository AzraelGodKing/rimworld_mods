using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace MultiFloors.HarmonyPatches;

[HotSwappable]
[HarmonyPatch]
[HarmonyPatchCategory("MultiFloors.Map")]
public static class HarmonyPatch_AllowLaunchOnUpperLevel
{
	[HarmonyPatch(typeof(CameraJumper), "GetWorldTargetOfMap")]
	[HarmonyPostfix]
	public static void GetDecoratedWorldTarget(ref GlobalTargetInfo __result, Map map)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (!((GlobalTargetInfo)(ref __result)).IsValid && map != null && map.Level() > 0)
		{
			Map obj = map.GroundMap();
			bool? obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				MapParent parent = obj.Parent;
				obj2 = ((parent != null) ? new bool?(((WorldObject)parent).Spawned) : ((bool?)null));
			}
			bool? flag = obj2;
			if (flag == true)
			{
				__result = GlobalTargetInfo.op_Implicit((WorldObject)(object)map.GroundMap().Parent);
			}
		}
	}
}
