using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public static class ColonistBarKFCompat
{
	private static readonly bool ready;

	private static readonly FieldInfo cachedEntriesF;

	private static readonly FieldInfo entriesDirtyF;

	private static readonly FieldInfo cachedScaleF;

	private static readonly PropertyInfo drawLocsP;

	private static readonly ConstructorInfo entryCtor;

	private static readonly FieldInfo entryPawnF;

	private static readonly FieldInfo entryMapF;

	private static readonly FieldInfo entryGroupF;

	private static readonly FieldInfo drawLocsFinderF;

	private static readonly MethodInfo calcDrawLocsM;

	private static readonly FieldInfo drawerF;

	private static readonly MethodInfo notifyRecachedM;

	private static int ltoActive;

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

	static ColonistBarKFCompat()
	{
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Expected O, but got Unknown
		//IL_029c: Expected O, but got Unknown
		ltoActive = -1;
		try
		{
			if (!ABDetect.Active("Mlie.ColonistBarKF"))
			{
				return;
			}
			Type type = AccessTools.TypeByName("ColonistBarKF.Bar.ColBarHelper_KF");
			Type type2 = AccessTools.TypeByName("ColonistBarKF.EntryKF");
			Type type3 = AccessTools.TypeByName("ColonistBarKF.Bar.ColonistBar_KF");
			Type type4 = AccessTools.TypeByName("ColonistBarKF.Bar.ColonistBarDrawLocsFinder_Kf");
			Type type5 = AccessTools.TypeByName("ColonistBarKF.Bar.ColonistBarColonistDrawer_KF");
			if (type == null || type2 == null || type3 == null || type4 == null)
			{
				Log.Warning("[As above, So below] Colonist Bar KF detected but its bar types were not found; column merge disabled for it.");
				return;
			}
			cachedEntriesF = AccessTools.Field(type, "cachedEntries");
			entriesDirtyF = AccessTools.Field(type, "EntriesDirty");
			cachedScaleF = AccessTools.Field(type, "CachedScale");
			drawLocsP = AccessTools.Property(type, "DrawLocs");
			entryCtor = AccessTools.Constructor(type2, new Type[3]
			{
				typeof(Pawn),
				typeof(Map),
				typeof(int)
			}, false);
			entryPawnF = AccessTools.Field(type2, "pawn");
			entryMapF = AccessTools.Field(type2, "map");
			entryGroupF = AccessTools.Field(type2, "group");
			drawLocsFinderF = AccessTools.Field(type3, "DrawLocsFinder");
			drawerF = AccessTools.Field(type3, "Drawer");
			calcDrawLocsM = AccessTools.Method(type4, "CalculateDrawLocs", new Type[2]
			{
				typeof(List<Vector2>),
				typeof(float).MakeByRefType()
			}, (Type[])null);
			notifyRecachedM = ((type5 != null) ? AccessTools.Method(type5, "Notify_RecachedEntries", (Type[])null, (Type[])null) : null);
			MethodInfo methodInfo = AccessTools.Method(type, "CheckRecacheEntries", (Type[])null, (Type[])null);
			if (cachedEntriesF == null || entriesDirtyF == null || cachedScaleF == null || drawLocsP == null || entryCtor == null || entryPawnF == null || entryMapF == null || entryGroupF == null || drawLocsFinderF == null || calcDrawLocsM == null || methodInfo == null)
			{
				Log.Warning("[As above, So below] Colonist Bar KF internals did not match; column merge disabled for it.");
				return;
			}
			HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(ColonistBarKFCompat), "Prefix", (Type[])null), new HarmonyMethod(typeof(ColonistBarKFCompat), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
			ready = true;
			ABLog.Dev("Colonist Bar KF detected, column merge enabled.");
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] Colonist Bar KF compat setup failed: " + ex.Message);
		}
	}

	private static void Prefix(object __instance, out bool __state)
	{
		__state = false;
		if (ready)
		{
			try
			{
				__state = (bool)entriesDirtyF.GetValue(__instance);
			}
			catch
			{
				__state = false;
			}
		}
	}

	private static void Postfix(object __instance, bool __state)
	{
		if (!ready || !__state || !ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		ABSettings settings = ABMod.Settings;
		if (settings == null || !settings.oneColonistBar || LtoActive)
		{
			return;
		}
		try
		{
			Merge(__instance);
		}
		catch (Exception e)
		{
			ABGuard.Disable(ABGuard.Ui, e, "colonist bar KF merge");
		}
	}

	private static void Merge(object helper)
	{
		IList list = cachedEntriesF.GetValue(helper) as IList;
		int num = list?.Count ?? 0;
		if (num == 0)
		{
			return;
		}
		Pawn[] array = (Pawn[])(object)new Pawn[num];
		Map[] array2 = (Map[])(object)new Map[num];
		int[] array3 = new int[num];
		for (int i = 0; i < num; i++)
		{
			object obj = list[i];
			ref Pawn reference = ref array[i];
			object value = entryPawnF.GetValue(obj);
			reference = (Pawn)((value is Pawn) ? value : null);
			ref Map reference2 = ref array2[i];
			object value2 = entryMapF.GetValue(obj);
			reference2 = (Map)((value2 is Map) ? value2 : null);
			array3[i] = (int)entryGroupF.GetValue(obj);
		}
		List<int> list2 = new List<int>();
		Dictionary<int, Map> groupMap = new Dictionary<int, Map>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, List<int>> dictionary2 = new Dictionary<int, List<int>>();
		for (int j = 0; j < num; j++)
		{
			int num2 = array3[j];
			if (!dictionary2.TryGetValue(num2, out var value3))
			{
				value3 = (dictionary2[num2] = new List<int>());
				list2.Add(num2);
				groupMap[num2] = array2[j];
				dictionary[num2] = j;
			}
			value3.Add(j);
		}
		Dictionary<int, long> colKey = new Dictionary<int, long>(list2.Count);
		Dictionary<long, int> dictionary3 = new Dictionary<long, int>();
		Dictionary<long, int> colAnchor = new Dictionary<long, int>();
		for (int k = 0; k < list2.Count; k++)
		{
			int num3 = list2[k];
			long num4 = ColumnKey(groupMap[num3], num3);
			colKey[num3] = num4;
			dictionary3[num4] = ((!dictionary3.TryGetValue(num4, out var value4)) ? 1 : (value4 + 1));
			if (!colAnchor.TryGetValue(num4, out var value5) || dictionary[num3] < value5)
			{
				colAnchor[num4] = dictionary[num3];
			}
		}
		bool flag = false;
		foreach (int value8 in dictionary3.Values)
		{
			if (value8 > 1)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		List<int> list4 = new List<int>(list2);
		list4.Sort(delegate(int a, int b)
		{
			int num12 = colAnchor[colKey[a]];
			int num13 = colAnchor[colKey[b]];
			return (num12 != num13) ? num12.CompareTo(num13) : LevelOrder(groupMap[a]).CompareTo(LevelOrder(groupMap[b]));
		});
		List<object> list5 = new List<object>(num);
		int num5 = -1;
		long num6 = long.MinValue;
		for (int num7 = 0; num7 < list4.Count; num7++)
		{
			int key = list4[num7];
			long num8 = colKey[key];
			bool flag2 = dictionary3[num8] > 1;
			List<int> list6 = dictionary2[key];
			for (int num9 = 0; num9 < list6.Count; num9++)
			{
				int num10 = list6[num9];
				if (!flag2 || array[num10] != null)
				{
					if (num5 < 0 || num8 != num6)
					{
						num5++;
						num6 = num8;
					}
					list5.Add(entryCtor.Invoke(new object[3]
					{
						array[num10],
						array2[num10],
						num5
					}));
				}
			}
		}
		list.Clear();
		for (int num11 = 0; num11 < list5.Count; num11++)
		{
			list.Add(list5[num11]);
		}
		object value6 = drawLocsFinderF.GetValue(null);
		object value7 = drawLocsP.GetValue(helper);
		object[] array4 = new object[2] { value7, 0f };
		calcDrawLocsM.Invoke(value6, array4);
		cachedScaleF.SetValue(helper, (float)array4[1]);
		object obj2 = drawerF?.GetValue(null);
		if (obj2 != null)
		{
			notifyRecachedM?.Invoke(obj2, null);
		}
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

	private static int LevelOrder(Map map)
	{
		return (map != null) ? (1 - map.Level()) : 0;
	}
}
