using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
internal static class Patch_ColonistBar_LevelOrder
{
	private static readonly FieldRef<ColonistBar, bool> DirtyRef = AccessTools.FieldRefAccess<ColonistBar, bool>("entriesDirty");

	private static readonly FieldRef<ColonistBar, List<Entry>> EntriesRef = AccessTools.FieldRefAccess<ColonistBar, List<Entry>>("cachedEntries");

	private static readonly FieldRef<ColonistBar, List<Vector2>> DrawLocsRef = AccessTools.FieldRefAccess<ColonistBar, List<Vector2>>("cachedDrawLocs");

	private static readonly FieldRef<ColonistBar, float> ScaleRef = AccessTools.FieldRefAccess<ColonistBar, float>("cachedScale");

	private static readonly FieldRef<ColonistBar, ColonistBarDrawLocsFinder> FinderRef = AccessTools.FieldRefAccess<ColonistBar, ColonistBarDrawLocsFinder>("drawLocsFinder");

	private static readonly FieldRef<ColonistBar, ColonistBarColonistDrawer> DrawerRef = AccessTools.FieldRefAccess<ColonistBar, ColonistBarColonistDrawer>("drawer");

	private static int ltoActive = -1;

	private static bool LtoActive
	{
		get
		{
			if (ltoActive < 0)
			{
				ltoActive = (ABDetect.Active("DerekBickley.LTOColonyGroupsFinal") ? 1 : 0);
			}
			return ltoActive == 1;
		}
	}

	private static void Prefix(ColonistBar __instance, out bool __state)
	{
		__state = DirtyRef.Invoke(__instance);
	}

	private static void Postfix(ColonistBar __instance, bool __state)
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		if (!__state || !ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		try
		{
			List<Entry> list = EntriesRef.Invoke(__instance);
			if (list == null || list.Count == 0 || LtoActive)
			{
				return;
			}
			bool flag = false;
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				LevelComp levelComp = maps[i].Levels();
				if (levelComp != null && levelComp.level == 0 && levelComp.MapByLevel.Count > 1)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			List<int> list2 = new List<int>();
			Dictionary<int, List<Entry>> dictionary = new Dictionary<int, List<Entry>>();
			Dictionary<int, Map> dictionary2 = new Dictionary<int, Map>();
			for (int j = 0; j < list.Count; j++)
			{
				Entry val = list[j];
				if (!dictionary.TryGetValue(val.group, out var value))
				{
					value = new List<Entry>();
					dictionary[val.group] = value;
					list2.Add(val.group);
					dictionary2[val.group] = val.map;
				}
				value.Add(val);
			}
			Dictionary<int, long> sortKey = new Dictionary<int, long>(list2.Count);
			for (int k = 0; k < list2.Count; k++)
			{
				int key = list2[k];
				sortKey[key] = KeyFor(dictionary2[key], k);
			}
			List<int> list3 = new List<int>(list2);
			list3.Sort((int a, int b) => sortKey[a].CompareTo(sortKey[b]));
			bool flag2 = ABMod.Settings?.oneColonistBar ?? true;
			Dictionary<int, long> dictionary3 = new Dictionary<int, long>(list3.Count);
			Dictionary<long, int> dictionary4 = new Dictionary<long, int>();
			for (int num = 0; num < list3.Count; num++)
			{
				long num2 = (flag2 ? ColumnKey(dictionary2[list3[num]], list3[num]) : (((long)list3[num] << 1) | 1));
				dictionary3[list3[num]] = num2;
				dictionary4[num2] = ((!dictionary4.TryGetValue(num2, out var value2)) ? 1 : (value2 + 1));
			}
			List<Entry> list4 = new List<Entry>(list.Count);
			int num3 = -1;
			long num4 = long.MinValue;
			for (int num5 = 0; num5 < list3.Count; num5++)
			{
				int key2 = list3[num5];
				long num6 = dictionary3[key2];
				bool flag3 = dictionary4[num6] > 1;
				List<Entry> list5 = dictionary[key2];
				for (int num7 = 0; num7 < list5.Count; num7++)
				{
					if (!flag3 || list5[num7].pawn != null)
					{
						if (num3 < 0 || num6 != num4)
						{
							num3++;
							num4 = num6;
						}
						list4.Add(new Entry(list5[num7].pawn, list5[num7].map, num3));
					}
				}
			}
			list.Clear();
			list.AddRange(list4);
			DrawerRef.Invoke(__instance).Notify_RecachedEntries();
			float num8 = default(float);
			FinderRef.Invoke(__instance).CalculateDrawLocs(DrawLocsRef.Invoke(__instance), ref num8, num3 + 1);
			ScaleRef.Invoke(__instance) = num8;
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "colonist bar level order");
		}
	}

	private static long KeyFor(Map map, int originalIndex)
	{
		if (map == null)
		{
			return 140737488289792L + originalIndex;
		}
		LevelComp levelComp = map.Levels();
		Map val = map.GroundMap() ?? map;
		int num = levelComp?.level ?? 0;
		return ((long)val.uniqueID << 16) + (1 - num);
	}

	private static long ColumnKey(Map map, int originalGroup)
	{
		if (map == null)
		{
			return ((long)originalGroup << 1) | 1;
		}
		Map val = map.GroundMap() ?? map;
		return (long)val.uniqueID << 1;
	}
}
