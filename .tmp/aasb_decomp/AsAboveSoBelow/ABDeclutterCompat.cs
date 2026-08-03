using System;
using System.Reflection;
using HarmonyLib;

namespace AsAboveSoBelow;

internal static class ABDeclutterCompat
{
	private static bool resolved;

	private static FieldInfo suppressField;

	private static void Resolve()
	{
		resolved = true;
		try
		{
			if (ABDetect.Active("sk.dmbb"))
			{
				Type type = AccessTools.TypeByName("MapControlsTableContext");
				FieldInfo fieldInfo = ((type != null) ? AccessTools.Field(type, "SuppressExternal") : null);
				if (fieldInfo != null && fieldInfo.IsStatic && fieldInfo.FieldType == typeof(bool))
				{
					suppressField = fieldInfo;
					ABLog.Dev("Declutter UI detected, level buttons will bypass its widget suppression.");
				}
				else
				{
					ABLog.Dev("Declutter UI present but MapControlsTableContext.SuppressExternal was not found; its play settings options may hide the level buttons.");
				}
			}
		}
		catch (Exception ex)
		{
			ABLog.Dev("Declutter UI compat resolve failed: " + ex.Message);
		}
	}

	public static bool PushUnsuppressed()
	{
		if (!resolved)
		{
			Resolve();
		}
		if (suppressField == null)
		{
			return false;
		}
		try
		{
			if ((bool)suppressField.GetValue(null))
			{
				suppressField.SetValue(null, false);
				return true;
			}
		}
		catch (Exception)
		{
			suppressField = null;
		}
		return false;
	}

	public static void Pop(bool pushed)
	{
		if (!pushed || suppressField == null)
		{
			return;
		}
		try
		{
			suppressField.SetValue(null, true);
		}
		catch (Exception)
		{
			suppressField = null;
		}
	}
}
