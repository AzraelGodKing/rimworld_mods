using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public static class ABTheme
{
	public static readonly Color PanelBG = Color32.op_Implicit(new Color32((byte)27, (byte)31, (byte)35, byte.MaxValue));

	public static readonly Color BGL = Color32.op_Implicit(new Color32((byte)47, (byte)51, (byte)55, byte.MaxValue));

	public static readonly Color BGD = Color32.op_Implicit(new Color32((byte)14, (byte)16, (byte)19, byte.MaxValue));

	public static readonly Color TextDim = new Color(0.62f, 0.65f, 0.7f);

	private static readonly Color AccentFallback = new Color(0.45f, 0.75f, 1f);

	private static PropertyInfo accentProp;

	private static FieldInfo accentField;

	private static bool accentResolved;

	private static int accentFrame = -1;

	private static Color accentCached = new Color(0.45f, 0.75f, 1f);

	public static Color Accent
	{
		get
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			int frameCount = Time.frameCount;
			if (frameCount == accentFrame)
			{
				return accentCached;
			}
			accentFrame = frameCount;
			accentCached = ResolveAccent();
			return accentCached;
		}
	}

	private static Color ResolveAccent()
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!accentResolved)
			{
				accentResolved = true;
				Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly("ModernNotifications.Theme", (string)null);
				if (typeInAnyAssembly != null)
				{
					accentProp = typeInAnyAssembly.GetProperty("Accent", BindingFlags.Static | BindingFlags.Public);
					if (accentProp == null)
					{
						accentField = typeInAnyAssembly.GetField("Accent", BindingFlags.Static | BindingFlags.Public);
					}
				}
			}
			if (accentProp != null)
			{
				return (Color)accentProp.GetValue(null);
			}
			if (accentField != null)
			{
				return (Color)accentField.GetValue(null);
			}
		}
		catch (Exception)
		{
			accentProp = null;
			accentField = null;
		}
		return AccentFallback;
	}
}
