using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AsAboveSoBelow;

[StaticConstructorOnStartup]
public static class HarmonyBoot
{
	public static readonly Harmony Harmony;

	static HarmonyBoot()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		Harmony = new Harmony("astryl.asabovesobelow");
		Type[] types;
		try
		{
			types = typeof(HarmonyBoot).Assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = ex.Types;
			string text = ((ex.LoaderExceptions != null && ex.LoaderExceptions.Length != 0) ? ex.LoaderExceptions[0].Message : "unknown");
			Log.Warning("[As above, So below] Some types failed to load; patching the rest. First loader error: " + text);
		}
		Type[] array = types;
		foreach (Type type in array)
		{
			if (!(type == null) && type.IsDefined(typeof(HarmonyPatch), inherit: false))
			{
				try
				{
					Harmony.CreateClassProcessor(type).Patch();
				}
				catch (Exception ex2)
				{
					Log.Warning("[As above, So below] Skipped patch class " + type.Name + ": " + ex2.Message);
				}
			}
		}
	}
}
