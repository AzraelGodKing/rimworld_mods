using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AsAboveSoBelow;

internal static class ABGenPreview
{
	private const int W = 130;

	private const int H = 88;

	private static int seed = 762195842;

	private static Texture2D upperTex;

	private static Texture2D lowerTex;

	private static int builtHash = -1;

	private static int pendingHash = -1;

	private static float pendingAt;

	private const float DebounceSeconds = 0.25f;

	private const byte KOut = 0;

	private const byte KLedge = 1;

	private const byte KWall = 2;

	private const byte KPlateau = 3;

	private static Color GrassColor => new Color(0.36f, 0.44f, 0.26f);

	private static Color MeadowColor => ABTrueColors.Of(TerrainDefOf.Soil, GenColor.FromHex("6D5B49"));

	private static Color GravelColor => ABTrueColors.Of(TerrainDefOf.Gravel, GenColor.FromHex("6D5B49"));

	private static Color RockColor => new Color(0.27f, 0.26f, 0.25f);

	private static Color RockDeepColor => new Color(0.16f, 0.155f, 0.15f);

	private static Color LedgeColor => ABTrueColors.Of(ABDefOf.AB_MountainTop, new Color(0.33f, 0.32f, 0.31f));

	private static Color WaterColor => ABTrueColors.Of(TerrainDefOf.WaterShallow, GenColor.FromHex("434F50"));

	private static Color WaterDeepColor => ABTrueColors.Of(TerrainDefOf.WaterDeep, GenColor.FromHex("3A434D"));

	private static Color HiddenColor => new Color(0.13f, 0.15f, 0.11f);

	private static Color AirColor => new Color(0.1f, 0.11f, 0.13f);

	private static int SettingsHash(ABSettings s)
	{
		int num = seed;
		num = num * 31 + (s.naturalPeaks ? 1 : 0);
		num = num * 31 + Mathf.RoundToInt(s.peakMeadowCutoff * 1000f);
		num = num * 31 + Mathf.RoundToInt(s.peakMeadowScale * 10000f);
		num = num * 31 + s.peakTerraceMax;
		num = num * 31 + Mathf.RoundToInt(s.peakOutcropDensity * 100f);
		num = num * 31 + Mathf.RoundToInt(s.peakTarns * 100f);
		num = num * 31 + Mathf.RoundToInt(s.peakHiddenValleys * 100f);
		num = num * 31 + Mathf.RoundToInt(s.peakSoilFraction * 100f);
		return num * 31 + Mathf.RoundToInt(s.peakVegetation * 100f);
	}

	internal static void Reroll()
	{
		seed = Rand.Int;
		builtHash = -1;
	}

	internal static void Draw(Rect rect, ABSettings s)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		Widgets.DrawMenuSection(rect);
		Rect val = GenUI.ContractedBy(rect, 10f);
		int num = SettingsHash(s);
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (num != builtHash)
		{
			if (num != pendingHash)
			{
				pendingHash = num;
				pendingAt = realtimeSinceStartup;
			}
			if ((Object)(object)upperTex == (Object)null || realtimeSinceStartup - pendingAt >= 0.25f)
			{
				try
				{
					Rebuild(s);
				}
				catch (Exception e)
				{
					ABGuard.Disable(ABGuard.Ui, e, "generation preview");
					return;
				}
				builtHash = num;
			}
		}
		float y = ((Rect)(ref val)).y;
		Text.Font = (GameFont)1;
		Widgets.Label(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 22f), Translator.Translate("AB_PreviewTitle"));
		y += 24f;
		float num2 = 164f;
		float num3 = Mathf.Max(120f, (((Rect)(ref val)).height - num2) / 2f);
		DrawPanel(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 20f + num3), TaggedString.op_Implicit(Translator.Translate("AB_PreviewUpper")), upperTex);
		y += 20f + num3 + 6f;
		DrawPanel(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 20f + num3), TaggedString.op_Implicit(Translator.Translate("AB_PreviewLower")), lowerTex);
		y += 20f + num3 + 8f;
		DrawLegend(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 34f));
		y += 36f;
		if (Widgets.ButtonText(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 28f), TaggedString.op_Implicit(Translator.Translate("AB_PreviewReroll")), true, true, true, (TextAnchor?)null))
		{
			Reroll();
		}
		y += 30f;
		GUI.color = new Color(1f, 1f, 1f, 0.55f);
		Text.Font = (GameFont)1;
		Widgets.Label(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 16f), Translator.Translate("AB_PreviewNote"));
		GUI.color = Color.white;
	}

	private static void DrawPanel(Rect rect, string caption, Texture2D tex)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		GUI.color = new Color(1f, 1f, 1f, 0.75f);
		Widgets.Label(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width, 18f), caption);
		GUI.color = Color.white;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + 20f, ((Rect)(ref rect)).width, ((Rect)(ref rect)).height - 20f);
		Widgets.DrawBoxSolid(val, new Color(0.06f, 0.06f, 0.07f));
		if ((Object)(object)tex != (Object)null)
		{
			GUI.DrawTexture(GenUI.ContractedBy(val, 1f), (Texture)(object)tex, (ScaleMode)0);
		}
		Widgets.DrawBox(val, 1, (Texture2D)null);
	}

	private static void DrawLegend(Rect rect)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		(Color, string)[] array = new(Color, string)[6]
		{
			(MeadowColor, "AB_LegendMeadow"),
			(RockColor, "AB_LegendRock"),
			(LedgeColor, "AB_LegendLedge"),
			(WaterColor, "AB_LegendTarn"),
			(HiddenColor, "AB_LegendHidden"),
			(AirColor, "AB_LegendAir")
		};
		float num = ((Rect)(ref rect)).width / 3f;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = ((Rect)(ref rect)).x + (float)(i % 3) * num;
			float num3 = ((Rect)(ref rect)).y + (float)(i / 3) * 17f;
			Widgets.DrawBoxSolid(new Rect(num2, num3 + 3f, 10f, 10f), array[i].Item1);
			GUI.color = new Color(1f, 1f, 1f, 0.75f);
			Widgets.Label(new Rect(num2 + 14f, num3 - 3f, num - 16f, 20f), Translator.Translate(array[i].Item2));
			GUI.color = Color.white;
		}
	}

	private static void Rebuild(ABSettings s)
	{
		Rand.PushState(seed);
		try
		{
			BuildTextures(s);
		}
		finally
		{
			Rand.PopState();
		}
	}

	private static float Noise01(Perlin p, int x, int z)
	{
		return Mathf.Clamp01((float)(((ModuleBase)p).GetValue((double)x, 0.0, (double)z) + 1.0) * 0.5f);
	}

	private static void BuildTextures(ABSettings s)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Expected O, but got Unknown
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Expected O, but got Unknown
		//IL_08ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d1: Expected O, but got Unknown
		//IL_099a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0980: Unknown result type (might be due to invalid IL or missing references)
		//IL_0993: Unknown result type (might be due to invalid IL or missing references)
		//IL_0956: Unknown result type (might be due to invalid IL or missing references)
		//IL_0960: Unknown result type (might be due to invalid IL or missing references)
		//IL_099f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0965: Unknown result type (might be due to invalid IL or missing references)
		//IL_094f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_09dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_09df: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a19: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0abc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a98: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0c: Unknown result type (might be due to invalid IL or missing references)
		int num = 11440;
		Perlin p = new Perlin(0.055, 2.0, 0.5, 5, Rand.Int, (QualityMode)1);
		Perlin p2 = new Perlin(0.07, 2.0, 0.5, 4, Rand.Int, (QualityMode)1);
		bool[] array = new bool[num];
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(65f, 44f);
		float num2 = (float)Mathf.Min(130, 88) * 0.52f;
		for (int i = 0; i < 88; i++)
		{
			for (int j = 0; j < 130; j++)
			{
				float num3 = Vector2.Distance(new Vector2((float)j, (float)i), val) / num2;
				array[i * 130 + j] = Noise01(p, j, i) * 0.65f + (1f - num3) * 0.55f > 0.62f;
			}
		}
		int[] array2 = new int[num];
		Queue<int> queue = new Queue<int>();
		for (int k = 0; k < num; k++)
		{
			array2[k] = (array[k] ? int.MaxValue : 0);
			if (!array[k])
			{
				queue.Enqueue(k);
			}
		}
		while (queue.Count > 0)
		{
			int num4 = queue.Dequeue();
			int num5 = num4 % 130;
			int num6 = num4 / 130;
			for (int l = -1; l <= 1; l++)
			{
				for (int m = -1; m <= 1; m++)
				{
					int num7 = num5 + m;
					int num8 = num6 + l;
					if (num7 >= 0 && num8 >= 0 && num7 < 130 && num8 < 88)
					{
						int num9 = num8 * 130 + num7;
						if (array2[num9] > array2[num4] + 1)
						{
							array2[num9] = array2[num4] + 1;
							queue.Enqueue(num9);
						}
					}
				}
			}
		}
		float num10 = Mathf.Clamp(s.peakMeadowCutoff, 0.45f, 0.75f);
		float num11 = Mathf.Clamp(s.peakMeadowScale, 0.012f, 0.048f);
		int num12 = Mathf.Clamp(s.peakTerraceMax, 1, 6);
		Perlin p3 = new Perlin((double)num11 * 2.2, 2.0, 0.5, 5, Rand.Int, (QualityMode)1);
		Perlin p4 = new Perlin(0.077, 2.0, 0.5, 4, Rand.Int, (QualityMode)1);
		byte[] kind = new byte[num];
		List<int> list = new List<int>();
		bool naturalPeaks = s.naturalPeaks;
		for (int n = 0; n < num; n++)
		{
			if (!array[n])
			{
				kind[n] = 0;
				continue;
			}
			int x = n % 130;
			int z = n / 130;
			if (naturalPeaks && Noise01(p3, x, z) > num10)
			{
				if (array2[n] <= 1)
				{
					kind[n] = 1;
					continue;
				}
				kind[n] = 3;
				list.Add(n);
				continue;
			}
			float num13 = Noise01(p4, x, z);
			int num14 = 1;
			if (num13 >= 0.5f && num12 > 1)
			{
				num14 = Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, num13), 1.6f) * (float)num12), 1, num12);
			}
			kind[n] = (byte)((array2[n] <= num14) ? 1 : 2);
		}
		if (naturalPeaks && list.Count >= 60 && s.peakOutcropDensity > 0.001f)
		{
			int num15 = Mathf.Max(1, Mathf.RoundToInt((float)list.Count / 900f * Mathf.Clamp(s.peakOutcropDensity, 0f, 2f)));
			for (int num16 = 0; num16 < num15; num16++)
			{
				Lump(kind, list[Rand.Range(0, list.Count)], Rand.RangeInclusive(9, 42), 2, 3);
			}
			list.RemoveAll((int num47) => kind[num47] != 3);
		}
		bool[] array3 = new bool[num];
		for (int num17 = 0; num17 < num; num17++)
		{
			if (kind[num17] == 1)
			{
				array3[num17] = true;
				queue.Enqueue(num17);
			}
		}
		while (queue.Count > 0)
		{
			int num18 = queue.Dequeue();
			int num19 = num18 % 130;
			int num20 = num18 / 130;
			for (int num21 = -1; num21 <= 1; num21++)
			{
				for (int num22 = -1; num22 <= 1; num22++)
				{
					int num23 = num19 + num22;
					int num24 = num20 + num21;
					if (num23 >= 0 && num24 >= 0 && num23 < 130 && num24 < 88)
					{
						int num25 = num24 * 130 + num23;
						if (!array3[num25] && (kind[num25] == 3 || kind[num25] == 1))
						{
							array3[num25] = true;
							queue.Enqueue(num25);
						}
					}
				}
			}
		}
		bool[] array4 = new bool[num];
		float num26 = Mathf.Clamp(s.peakHiddenValleys, 0f, 1f);
		bool[] array5 = new bool[num];
		for (int num27 = 0; num27 < num; num27++)
		{
			if (kind[num27] != 3 || array3[num27] || array5[num27])
			{
				continue;
			}
			List<int> list2 = new List<int>();
			Queue<int> queue2 = new Queue<int>();
			queue2.Enqueue(num27);
			array5[num27] = true;
			while (queue2.Count > 0)
			{
				int num28 = queue2.Dequeue();
				list2.Add(num28);
				int num29 = num28 % 130;
				int num30 = num28 / 130;
				for (int num31 = -1; num31 <= 1; num31++)
				{
					for (int num32 = -1; num32 <= 1; num32++)
					{
						int num33 = num29 + num32;
						int num34 = num30 + num31;
						if (num33 >= 0 && num34 >= 0 && num33 < 130 && num34 < 88)
						{
							int num35 = num34 * 130 + num33;
							if (!array5[num35] && !array3[num35] && kind[num35] == 3)
							{
								array5[num35] = true;
								queue2.Enqueue(num35);
							}
						}
					}
				}
			}
			bool flag = Rand.Chance(num26);
			for (int num36 = 0; num36 < list2.Count; num36++)
			{
				array4[list2[num36]] = flag;
			}
		}
		bool[] array6 = new bool[num];
		bool[] array7 = new bool[num];
		float num37 = Mathf.Clamp(s.peakTarns, 0f, 2f);
		if (naturalPeaks && num37 > 0.001f && list.Count >= 120)
		{
			float num38 = (float)list.Count / 1400f * num37;
			int num39 = Mathf.FloorToInt(num38) + (Rand.Chance(num38 - (float)Mathf.FloorToInt(num38)) ? 1 : 0);
			for (int num40 = 0; num40 < num39; num40++)
			{
				int seedCell = list[Rand.Range(0, list.Count)];
				int num41 = LumpMark(array6, kind, seedCell, Rand.RangeInclusive(8, 26), 3);
				if (num41 >= 14)
				{
					LumpMark(array7, kind, seedCell, Mathf.Max(4, num41 / 4), 3);
				}
			}
		}
		Perlin p5 = new Perlin(0.12, 2.0, 0.5, 4, Rand.Int, (QualityMode)1);
		float num42 = Mathf.Clamp(s.peakSoilFraction, 0f, 0.5f);
		float num43 = Mathf.Clamp(s.peakVegetation, 0f, 2f);
		Color32[] array8 = (Color32[])(object)new Color32[num];
		Color32[] array9 = (Color32[])(object)new Color32[num];
		for (int num44 = 0; num44 < num; num44++)
		{
			int x2 = num44 % 130;
			int z2 = num44 / 130;
			Color val2;
			if (array[num44])
			{
				val2 = ((array2[num44] <= 1) ? (RockColor * 1.25f) : ((array2[num44] >= 4) ? RockDeepColor : RockColor));
			}
			else
			{
				float num45 = Noise01(p2, x2, z2);
				val2 = ((num45 > 0.72f) ? MeadowColor : (GrassColor * (0.9f + num45 * 0.25f)));
				if (Rand.Chance(0.012f * num43))
				{
					((Color)(ref val2))._002Ector(0.18f, 0.3f, 0.14f);
				}
			}
			val2.a = 1f;
			array8[num44] = Color32.op_Implicit(val2);
			Color val3;
			switch (kind[num44])
			{
			case 0:
				val3 = val2 * 0.5f;
				break;
			case 1:
				val3 = LedgeColor;
				break;
			case 2:
				val3 = ((array2[num44] >= 4) ? RockDeepColor : RockColor);
				break;
			default:
				if (array6[num44])
				{
					val3 = (array7[num44] ? WaterDeepColor : WaterColor);
				}
				else
				{
					float num46 = Noise01(p5, x2, z2);
					val3 = ((num46 > 1f - num42) ? MeadowColor : ((num46 < 0.22f) ? (GravelColor * 0.85f) : GravelColor));
					val3 = Color.Lerp(val3, GrassColor, 0.45f);
					if (Rand.Chance(0.03f * num43))
					{
						((Color)(ref val3))._002Ector(0.2f, 0.33f, 0.16f);
					}
				}
				if (array4[num44])
				{
					val3 = Color.Lerp(val3, HiddenColor, 0.75f);
				}
				break;
			}
			val3.a = 1f;
			array9[num44] = Color32.op_Implicit(val3);
		}
		Upload(ref lowerTex, array8);
		Upload(ref upperTex, array9);
	}

	private static void Lump(byte[] kind, int seedCell, int size, byte toKind, byte fromKind)
	{
		int num = seedCell % 130;
		int num2 = seedCell / 130;
		for (int i = 0; i < size; i++)
		{
			int num3 = num + Rand.RangeInclusive(-3, 3);
			int num4 = num2 + Rand.RangeInclusive(-3, 3);
			if (num3 >= 0 && num4 >= 0 && num3 < 130 && num4 < 88)
			{
				int num5 = num4 * 130 + num3;
				if (kind[num5] == fromKind)
				{
					kind[num5] = toKind;
				}
			}
		}
	}

	private static int LumpMark(bool[] mark, byte[] kind, int seedCell, int size, byte requireKind)
	{
		int num = seedCell % 130;
		int num2 = seedCell / 130;
		int num3 = 0;
		for (int i = 0; i < size; i++)
		{
			int num4 = num + Rand.RangeInclusive(-3, 3);
			int num5 = num2 + Rand.RangeInclusive(-3, 3);
			if (num4 >= 0 && num5 >= 0 && num4 < 130 && num5 < 88)
			{
				int num6 = num5 * 130 + num4;
				if (kind[num6] == requireKind && !mark[num6])
				{
					mark[num6] = true;
					num3++;
				}
			}
		}
		return num3;
	}

	private static void Upload(ref Texture2D tex, Color32[] pixels)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if ((Object)(object)tex == (Object)null)
		{
			tex = new Texture2D(130, 88, (TextureFormat)4, false)
			{
				filterMode = (FilterMode)0,
				wrapMode = (TextureWrapMode)1,
				name = "AB_GenPreview"
			};
		}
		tex.SetPixels32(pixels);
		tex.Apply(false);
	}
}
