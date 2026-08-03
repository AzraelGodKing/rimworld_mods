using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
internal static class Patch_ResourceCounter_AllLevels
{
	private static readonly FieldRef<ResourceCounter, Map> MapRef = AccessTools.FieldRefAccess<ResourceCounter, Map>("map");

	private static readonly FieldRef<ResourceCounter, Dictionary<ThingDef, int>> CountsRef = AccessTools.FieldRefAccess<ResourceCounter, Dictionary<ThingDef, int>>("countedAmounts");

	private static readonly Func<ResourceCounter, Thing, bool> ShouldCountDel = AccessTools.MethodDelegate<Func<ResourceCounter, Thing, bool>>(AccessTools.Method(typeof(ResourceCounter), "ShouldCount", (Type[])null, (Type[])null), (object)null, true, (Type[])null);

	private static void Postfix(ResourceCounter __instance)
	{
		if (!ABGuard.On(ABGuard.Logistics))
		{
			return;
		}
		try
		{
			Map val = MapRef.Invoke(__instance);
			LevelComp levelComp = val?.Controller();
			if (levelComp == null || levelComp.MapByLevel.Count <= 1)
			{
				return;
			}
			Dictionary<ThingDef, int> dictionary = CountsRef.Invoke(__instance);
			foreach (KeyValuePair<int, Map> item in levelComp.MapByLevel)
			{
				Map value = item.Value;
				if (value == null || value == val || value.Disposed)
				{
					continue;
				}
				List<SlotGroup> allGroupsListForReading = value.haulDestinationManager.AllGroupsListForReading;
				for (int i = 0; i < allGroupsListForReading.Count; i++)
				{
					foreach (Thing heldThing in allGroupsListForReading[i].HeldThings)
					{
						Thing innerIfMinified = MinifyUtility.GetInnerIfMinified(heldThing);
						if (innerIfMinified.def.CountAsResource && ShouldCountDel(__instance, innerIfMinified))
						{
							dictionary[innerIfMinified.def] += innerIfMinified.stackCount;
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Logistics, e, "colony wide resource counts");
		}
	}
}
