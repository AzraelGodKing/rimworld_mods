using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
internal static class ABAnimalTabCompat
{
	private static readonly FieldRef<object, IEnumerable<Pawn>> AllPawnsRef;

	private static readonly FieldRef<object, IEnumerable<Pawn>> FilteredPawnsRef;

	private static readonly PropertyInfo FiltersProp;

	private static readonly MethodInfo AllowsMethod;

	private static bool broken;

	static ABAnimalTabCompat()
	{
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		try
		{
			if (ABDetect.Active("Fluffy.AnimalTab"))
			{
				Type type = AccessTools.TypeByName("AnimalTab.MainTabWindow_Animals");
				MethodInfo methodInfo = ((type != null) ? AccessTools.Method(type, "RecachePawns", (Type[])null, (Type[])null) : null);
				FieldInfo fieldInfo = ((type != null) ? AccessTools.Field(type, "_allPawns") : null);
				FieldInfo fieldInfo2 = ((type != null) ? AccessTools.Field(type, "_filteredPawns") : null);
				if (methodInfo == null || fieldInfo == null || fieldInfo2 == null)
				{
					Log.Warning("[As above, So below] Animal Tab detected but its window internals were not found; other levels' animals will not appear in its tab.");
					return;
				}
				AllPawnsRef = AccessTools.FieldRefAccess<IEnumerable<Pawn>>(type, "_allPawns");
				FilteredPawnsRef = AccessTools.FieldRefAccess<IEnumerable<Pawn>>(type, "_filteredPawns");
				FiltersProp = AccessTools.Property(type, "Filters");
				Type type2 = AccessTools.TypeByName("AnimalTab.FilterWorker");
				AllowsMethod = ((type2 != null) ? AccessTools.Method(type2, "Allows", (Type[])null, (Type[])null) : null);
				HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(ABAnimalTabCompat), "RecachePostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
				ABLog.Dev("Animal Tab detected, cross level animals enabled in its tab.");
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] Animal Tab compat setup failed: " + ex.Message);
		}
	}

	private static void RecachePostfix(object __instance)
	{
		if (broken || !ABGuard.On(ABGuard.Ui))
		{
			return;
		}
		try
		{
			Map currentMap = Find.CurrentMap;
			LevelComp levelComp = currentMap?.Controller();
			if (levelComp == null || levelComp.MapByLevel.Count <= 1)
			{
				return;
			}
			List<Pawn> list = new List<Pawn>();
			foreach (KeyValuePair<int, Map> item in levelComp.MapByLevel.OrderByDescending((KeyValuePair<int, Map> k) => k.Key))
			{
				Map value = item.Value;
				if (value == null || value == currentMap || value.Disposed)
				{
					continue;
				}
				List<Pawn> list2 = value.mapPawns.PawnsInFaction(Faction.OfPlayer);
				for (int num = 0; num < list2.Count; num++)
				{
					if (list2[num].RaceProps.Animal)
					{
						list.Add(list2[num]);
					}
				}
			}
			if (list.Count != 0)
			{
				IEnumerable<Pawn> enumerable = AllPawnsRef.Invoke(__instance);
				if (enumerable != null)
				{
					AllPawnsRef.Invoke(__instance) = enumerable.Concat(list);
				}
				IEnumerable<Pawn> enumerable2 = FilteredPawnsRef.Invoke(__instance);
				if (enumerable2 != null)
				{
					FilteredPawnsRef.Invoke(__instance) = enumerable2.Concat(list.Where(PassesFilters));
				}
			}
		}
		catch (Exception ex)
		{
			broken = true;
			Log.Warning("[As above, So below] Animal Tab cross level append failed and shut itself down: " + ex.Message);
		}
	}

	private static bool PassesFilters(Pawn p)
	{
		try
		{
			if (FiltersProp == null || AllowsMethod == null)
			{
				return true;
			}
			if (!(FiltersProp.GetValue(null) is IEnumerable enumerable))
			{
				return true;
			}
			object[] parameters = new object[1] { p };
			foreach (object item in enumerable)
			{
				if (item != null && AllowsMethod.Invoke(item, parameters) is bool flag && !flag)
				{
					return false;
				}
			}
			return true;
		}
		catch (Exception)
		{
			return true;
		}
	}
}
