using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
internal static class DrawPosOffsetPatcher
{
	static DrawPosOffsetPatcher()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		HarmonyMethod val = new HarmonyMethod(typeof(DrawPosOffsetPatcher), "OffsetPostfix", (Type[])null)
		{
			priority = 0
		};
		int num = 0;
		List<Type> list = new List<Type> { typeof(Thing) };
		list.AddRange(GenTypes.AllSubclasses(typeof(Thing)));
		foreach (Type item in list)
		{
			MethodInfo methodInfo = null;
			try
			{
				methodInfo = item.GetProperty("DrawPos", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true);
			}
			catch (Exception)
			{
				continue;
			}
			if (!(methodInfo == null) && !methodInfo.IsAbstract)
			{
				try
				{
					HarmonyBoot.Harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null);
					num++;
				}
				catch (Exception ex2)
				{
					Log.Warning("[As above, So below] Could not patch DrawPos on " + item.Name + ": " + ex2.Message);
				}
			}
		}
		ABLog.Dev("Patched DrawPos on " + num + " types for below-level rendering.");
	}

	private static void OffsetPostfix(ref Vector3 __result)
	{
		if (LevelRenderer.OffsetActive)
		{
			LevelRenderer.ApplyDrawShift(ref __result);
		}
	}
}
