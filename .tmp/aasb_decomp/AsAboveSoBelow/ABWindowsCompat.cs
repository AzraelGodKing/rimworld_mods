using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
internal static class ABWindowsCompat
{
	private static FieldRef<Thing, sbyte> mapIndexRef;

	private static FieldInfo eventField;

	private static bool broken;

	static ABWindowsCompat()
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		try
		{
			if (ABDetect.Active("JPT.OpenTheWindows"))
			{
				Type type = AccessTools.TypeByName("OpenTheWindows.Building_Window");
				Type type2 = AccessTools.TypeByName("OpenTheWindows.MapUpdateWatcher");
				MethodInfo methodInfo = ((type != null) ? AccessTools.Method(type, "MapUpdateHandler", (Type[])null, (Type[])null) : null);
				eventField = ((type2 != null) ? AccessTools.Field(type2, "MapUpdate") : null);
				if (methodInfo == null || eventField == null || !eventField.IsStatic)
				{
					Log.Warning("[As above, So below] Open The Windows detected but its update event internals were not found; the window event shield is off.");
					return;
				}
				mapIndexRef = AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");
				HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(ABWindowsCompat), "MapUpdatePrefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				ABLog.Dev("Open The Windows detected, window event shield active.");
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[As above, So below] Open The Windows compat setup failed: " + ex.Message);
		}
	}

	private static bool MapUpdatePrefix(object __instance)
	{
		if (broken)
		{
			return true;
		}
		try
		{
			Thing val = (Thing)((__instance is Thing) ? __instance : null);
			if (val == null)
			{
				return true;
			}
			if (val.Destroyed)
			{
				Unsubscribe(val);
				return false;
			}
			sbyte b = mapIndexRef.Invoke(val);
			if (b < 0)
			{
				return true;
			}
			List<Map> maps = Find.Maps;
			if (maps != null && b < maps.Count)
			{
				return true;
			}
			Unsubscribe(val);
			return false;
		}
		catch (Exception ex)
		{
			broken = true;
			Log.Warning("[As above, So below] Open The Windows event shield hit an error and turned itself off: " + ex.Message);
			return true;
		}
	}

	private static void Unsubscribe(object window)
	{
		Delegate obj = (Delegate)eventField.GetValue(null);
		if ((object)obj == null)
		{
			return;
		}
		Delegate[] invocationList = obj.GetInvocationList();
		Delegate obj2 = obj;
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (invocationList[i].Target == window)
			{
				obj2 = Delegate.Remove(obj2, invocationList[i]);
			}
		}
		if ((object)obj2 != obj)
		{
			eventField.SetValue(null, obj2);
		}
	}
}
