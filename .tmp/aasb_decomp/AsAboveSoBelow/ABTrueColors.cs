using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

internal static class ABTrueColors
{
	private static readonly Dictionary<TerrainDef, Color> cache = new Dictionary<TerrainDef, Color>();

	internal static Color Of(TerrainDef def, Color fallback)
	{
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		if (def == null)
		{
			return fallback;
		}
		if (cache.TryGetValue(def, out var value))
		{
			return value;
		}
		Color val = fallback;
		try
		{
			Graphic graphic = ((BuildableDef)def).graphic;
			object obj;
			if (graphic == null)
			{
				obj = null;
			}
			else
			{
				Material matSingle = graphic.MatSingle;
				obj = ((matSingle != null) ? matSingle.mainTexture : null);
			}
			Texture2D val2 = (Texture2D)((obj is Texture2D) ? obj : null);
			if ((Object)(object)val2 != (Object)null)
			{
				RenderTexture temporary = RenderTexture.GetTemporary(1, 1, 0, (RenderTextureFormat)0, (RenderTextureReadWrite)0);
				Texture2D val3 = new Texture2D(1, 1, (TextureFormat)4, false);
				RenderTexture active = RenderTexture.active;
				RenderTexture.active = temporary;
				Graphics.Blit((Texture)(object)val2, temporary);
				val3.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0);
				val3.Apply(false);
				Color pixel = val3.GetPixel(0, 0);
				RenderTexture.active = active;
				RenderTexture.ReleaseTemporary(temporary);
				Object.Destroy((Object)(object)val3);
				val = pixel * ((BuildableDef)def).graphic.MatSingle.color * ((BuildableDef)def).graphic.color;
				val.a = 1f;
			}
		}
		catch
		{
			val = fallback;
		}
		cache[def] = val;
		return val;
	}
}
