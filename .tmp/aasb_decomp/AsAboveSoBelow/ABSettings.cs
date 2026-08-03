using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AsAboveSoBelow;

public class ABSettings : ModSettings
{
	public bool verboseLogging;

	public bool showLiveBelow = true;

	public bool selectBelowInPlace = true;

	public bool showCeilingHint = true;

	public bool showLevelWidget = true;

	public bool oneColonistBar = true;

	public bool cameraFollowStairs = true;

	public bool cameraLockKeybind = true;

	public float belowDim = 0.06f;

	public bool drawSlabEdge = true;

	public bool drawWallReveal = true;

	public float wallRevealWidth = 0.5f;

	public float belowThingScale = 0.85f;

	public float climbTimeMultiplier = 1f;

	public bool crossLevelWork = true;

	public bool priorityCrossLevelWork = true;

	public bool crossLevelOrders = true;

	public bool crossLevelCombat = true;

	public bool crossLevelAutoEngage = true;

	public bool idleReturnHome = true;

	public bool crossLevelHauling = true;

	public bool crossLevelSupply = true;

	public bool crossLevelNeeds = true;

	public bool crossLevelPrisoners = true;

	public bool crossLevelAnimalWander = true;

	public bool crossLevelRituals = true;

	public bool crossLevelSocial = true;

	public bool crossLevelPipes = true;

	public bool crossLevelTemperature = true;

	public bool podTransit = true;

	public bool threatBasementInfest;

	public bool threatSkyDrops;

	public float threatDivertChance = 0.25f;

	public bool columnWealth = true;

	public bool worldIntegration = true;

	public bool cavernBasements = true;

	public string cavernBiome = "Random";

	public float cavernOpenness = 0.35f;

	public bool naturalPeaks = true;

	public bool skyBiomeInherit = true;

	public float peakSoilFraction = 0.15f;

	public float peakVegetation = 1f;

	public float peakMeadowCutoff = 0.6f;

	public float peakMeadowScale = 0.024f;

	public int peakTerraceMax = 4;

	public float peakOutcropDensity = 1f;

	public float peakTarns = 1f;

	public float peakHiddenValleys = 1f;

	public float skyOreDensity = 6f;

	public float basementOreDensity = 6f;

	public float cavernChamberFreq = 0.02f;

	public float cavernFormations = 1f;

	public bool skyLandmarks = true;

	public float skyLandmarkChance = 0.3f;

	public int skyLandmarkMax = 1;

	public Dictionary<string, int> landmarkModes;

	private int curTab;

	private bool landmarksExpanded;

	private bool? landmarksExpandedPending;

	private readonly Vector2[] tabScroll = (Vector2[])(object)new Vector2[5];

	private readonly float[] tabHeight = new float[5];

	private static readonly string[] TabKeys = new string[5] { "AB_TabGeneration", "AB_TabView", "AB_TabWork", "AB_TabCombat", "AB_TabAdvanced" };

	private static readonly Color OkGreen = new Color(0.4f, 0.85f, 0.4f);

	private static readonly Color NoteDim = new Color(1f, 1f, 1f, 0.62f);

	public void DoWindowContents(Rect inRect)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Expected O, but got Unknown
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		if (landmarksExpandedPending.HasValue)
		{
			landmarksExpanded = landmarksExpandedPending.Value;
			landmarksExpandedPending = null;
		}
		Rect val = inRect;
		((Rect)(ref val)).yMin = ((Rect)(ref val)).yMin + 42f;
		int clickedTab = -1;
		List<TabRecord> list = new List<TabRecord>(TabKeys.Length);
		for (int i = 0; i < TabKeys.Length; i++)
		{
			int tabIndex = i;
			list.Add(new TabRecord(TaggedString.op_Implicit(Translator.Translate(TabKeys[i])), (Action)delegate
			{
				clickedTab = tabIndex;
			}, curTab == i));
		}
		Widgets.DrawMenuSection(val);
		TabDrawer.DrawTabs<TabRecord>(val, list, 200f);
		Rect val2 = GenUI.ContractedBy(val, 9f);
		if (curTab == 0)
		{
			float num = Mathf.Min(370f, ((Rect)(ref val2)).width * 0.45f);
			ABGenPreview.Draw(new Rect(((Rect)(ref val2)).xMax - num, ((Rect)(ref val2)).y, num, ((Rect)(ref val2)).height), this);
			((Rect)(ref val2)).width = ((Rect)(ref val2)).width - (num + 10f);
		}
		Rect val3 = default(Rect);
		((Rect)(ref val3))._002Ector(0f, 0f, ((Rect)(ref val2)).width - 16f, Mathf.Max(tabHeight[curTab], ((Rect)(ref val2)).height));
		Widgets.BeginScrollView(val2, ref tabScroll[curTab], val3, true);
		Listing_Standard val4 = new Listing_Standard
		{
			maxOneColumn = true
		};
		((Listing)val4).Begin(val3);
		switch (curTab)
		{
		case 0:
			DoGenerationTab(val4);
			break;
		case 1:
			DoViewTab(val4);
			break;
		case 2:
			DoWorkTab(val4);
			break;
		case 3:
			DoCombatTab(val4);
			break;
		default:
			DoAdvancedTab(val4);
			break;
		}
		TabResetRow(val4, curTab);
		tabHeight[curTab] = ((Listing)val4).CurHeight + 12f;
		((Listing)val4).End();
		Widgets.EndScrollView();
		if (clickedTab >= 0 && clickedTab != curTab)
		{
			curTab = clickedTab;
		}
	}

	private void DoGenerationTab(Listing_Standard listing)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
		//IL_064a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_045e: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0504: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_052d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_056f: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0706: Unknown result type (might be due to invalid IL or missing references)
		//IL_071b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0783: Unknown result type (might be due to invalid IL or missing references)
		//IL_0788: Unknown result type (might be due to invalid IL or missing references)
		//IL_08be: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0919: Unknown result type (might be due to invalid IL or missing references)
		//IL_0923: Unknown result type (might be due to invalid IL or missing references)
		//IL_0942: Unknown result type (might be due to invalid IL or missing references)
		//IL_0951: Unknown result type (might be due to invalid IL or missing references)
		//IL_095b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0983: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_099d: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_081d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0827: Expected O, but got Unknown
		//IL_085f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0882: Unknown result type (might be due to invalid IL or missing references)
		//IL_088c: Expected O, but got Unknown
		//IL_08ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b6: Expected O, but got Unknown
		bool flag = naturalPeaks;
		bool flag2 = BiomesCavernsCompat.Active && cavernBasements;
		GUI.color = NoteDim;
		listing.Label(Translator.Translate("AB_GenNote"), -1f, (string)null);
		GUI.color = Color.white;
		((Listing)listing).Gap(4f);
		listing.Label(Translator.Translate("AB_Presets"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_PresetsTip")));
		Rect rect = ((Listing)listing).GetRect(30f, 1f);
		float num = (((Rect)(ref rect)).width - 18f) / 4f;
		if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, num, 30f), TaggedString.op_Implicit(Translator.Translate("AB_PresetDefault")), true, true, true, (TextAnchor?)null))
		{
			ResetGeneration();
		}
		if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).x + num + 6f, ((Rect)(ref rect)).y, num, 30f), TaggedString.op_Implicit(Translator.Translate("AB_PresetDramatic")), true, true, true, (TextAnchor?)null))
		{
			ApplyPeakPreset(0.54f, 0.018f, 5, 1.4f, 1.3f, 0.2f, 1.25f);
		}
		if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).x + (num + 6f) * 2f, ((Rect)(ref rect)).y, num, 30f), TaggedString.op_Implicit(Translator.Translate("AB_PresetSubtle")), true, true, true, (TextAnchor?)null))
		{
			ApplyPeakPreset(0.68f, 0.03f, 2, 0.6f, 0.4f, 0.1f, 0.75f);
		}
		if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).x + (num + 6f) * 3f, ((Rect)(ref rect)).y, num, 30f), TaggedString.op_Implicit(Translator.Translate("AB_PresetLush")), true, true, true, (TextAnchor?)null))
		{
			ApplyPeakPreset(0.52f, 0.02f, 4, 0.8f, 1.6f, 0.35f, 1.6f);
		}
		((Listing)listing).GapLine(10f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_SkyBiomeInherit")), ref skyBiomeInherit, TaggedString.op_Implicit(Translator.Translate("AB_SkyBiomeInheritTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_NaturalPeaks")), ref naturalPeaks, TaggedString.op_Implicit(Translator.Translate("AB_NaturalPeaksTip")), 0f, 1f);
		if (flag)
		{
			((Listing)listing).Indent(16f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
			float num2 = Mathf.InverseLerp(0.75f, 0.45f, peakMeadowCutoff);
			listing.Label(Translator.Translate("AB_PlateauOpenness") + ": " + GenText.ToStringPercent(num2), -1f, TaggedString.op_Implicit(Translator.Translate("AB_PlateauOpennessTip")));
			peakMeadowCutoff = Mathf.Lerp(0.75f, 0.45f, listing.Slider(num2, 0f, 1f));
			float num3 = Mathf.InverseLerp(0.048f, 0.012f, peakMeadowScale);
			listing.Label(Translator.Translate("AB_MeadowScale") + ": " + GenText.ToStringPercent(num3), -1f, TaggedString.op_Implicit(Translator.Translate("AB_MeadowScaleTip")));
			peakMeadowScale = Mathf.Lerp(0.048f, 0.012f, listing.Slider(num3, 0f, 1f));
			listing.Label(TaggedString.op_Implicit(TaggedString.op_Implicit(Translator.Translate("AB_TerraceMax") + ": ") + peakTerraceMax), -1f, TaggedString.op_Implicit(Translator.Translate("AB_TerraceMaxTip")));
			peakTerraceMax = Mathf.RoundToInt(listing.Slider((float)peakTerraceMax, 1f, 6f));
			listing.Label(Translator.Translate("AB_OutcropDensity") + ": " + GenText.ToStringPercent(peakOutcropDensity), -1f, TaggedString.op_Implicit(Translator.Translate("AB_OutcropDensityTip")));
			peakOutcropDensity = listing.Slider(peakOutcropDensity, 0f, 2f);
			listing.Label(Translator.Translate("AB_Tarns") + ": " + GenText.ToStringPercent(peakTarns), -1f, TaggedString.op_Implicit(Translator.Translate("AB_TarnsTip")));
			peakTarns = listing.Slider(peakTarns, 0f, 2f);
			listing.Label(Translator.Translate("AB_HiddenValleys") + ": " + GenText.ToStringPercent(peakHiddenValleys), -1f, TaggedString.op_Implicit(Translator.Translate("AB_HiddenValleysTip")));
			peakHiddenValleys = listing.Slider(peakHiddenValleys, 0f, 1f);
			listing.Label(Translator.Translate("AB_PeakSoil") + ": " + GenText.ToStringPercent(peakSoilFraction), -1f, TaggedString.op_Implicit(Translator.Translate("AB_PeakSoilTip")));
			peakSoilFraction = listing.Slider(peakSoilFraction, 0f, 0.5f);
			listing.Label(Translator.Translate("AB_PeakVegetation") + ": " + GenText.ToStringPercent(peakVegetation), -1f, TaggedString.op_Implicit(Translator.Translate("AB_PeakVegetationTip")));
			peakVegetation = listing.Slider(peakVegetation, 0f, 2f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
			((Listing)listing).Outdent(16f);
		}
		listing.Label(Translator.Translate("AB_SkyOre") + ": " + skyOreDensity.ToString("0.0"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_SkyOreTip")));
		skyOreDensity = listing.Slider(skyOreDensity, 0f, 12f);
		((Listing)listing).GapLine(10f);
		listing.Label(Translator.Translate("AB_BasementOre") + ": " + basementOreDensity.ToString("0.0"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_BasementOreTip")));
		basementOreDensity = listing.Slider(basementOreDensity, 0f, 12f);
		if (BiomesCavernsCompat.Active)
		{
			listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CavernBasements")), ref cavernBasements, TaggedString.op_Implicit(Translator.Translate("AB_CavernBasementsTip")), 0f, 1f);
			if (flag2)
			{
				((Listing)listing).Indent(16f);
				((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
				object obj;
				if (!(cavernBiome == "Random"))
				{
					BiomeDef namedSilentFail = DefDatabase<BiomeDef>.GetNamedSilentFail(cavernBiome);
					obj = ((namedSilentFail != null) ? ((object)((Def)namedSilentFail).LabelCap/*cast due to .constrained prefix*/).ToString() : null) ?? cavernBiome;
				}
				else
				{
					obj = ((object)Translator.Translate("AB_CavernBiomeRandom")/*cast due to .constrained prefix*/).ToString();
				}
				string text = (string)obj;
				if (listing.ButtonTextLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CavernBiome")), text, (TextAnchor)0, (string)null, TaggedString.op_Implicit(Translator.Translate("AB_CavernBiomeTip"))))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>
					{
						new FloatMenuOption(TaggedString.op_Implicit(Translator.Translate("AB_CavernBiomeRandom")), (Action)delegate
						{
							cavernBiome = "Random";
						}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0)
					};
					List<BiomeDef> list2 = BiomesCavernsCompat.CavernBiomes();
					for (int num4 = 0; num4 < list2.Count; num4++)
					{
						BiomeDef b = list2[num4];
						list.Add(new FloatMenuOption(TaggedString.op_Implicit(((Def)b).LabelCap), (Action)delegate
						{
							cavernBiome = ((Def)b).defName;
						}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
					}
					Find.WindowStack.Add((Window)new FloatMenu(list));
				}
				listing.Label(Translator.Translate("AB_CavernOpenness") + ": " + GenText.ToStringPercent(cavernOpenness), -1f, TaggedString.op_Implicit(Translator.Translate("AB_CavernOpennessTip")));
				cavernOpenness = listing.Slider(cavernOpenness, 0.1f, 0.6f);
				listing.Label(Translator.Translate("AB_ChamberFreq") + ": " + (cavernChamberFreq * 100f).ToString("0.0"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_ChamberFreqTip")));
				cavernChamberFreq = listing.Slider(cavernChamberFreq, 0.01f, 0.05f);
				listing.Label(Translator.Translate("AB_CavernFormations") + ": " + GenText.ToStringPercent(cavernFormations), -1f, TaggedString.op_Implicit(Translator.Translate("AB_CavernFormationsTip")));
				cavernFormations = listing.Slider(cavernFormations, 0f, 2f);
				((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
				((Listing)listing).Outdent(16f);
			}
		}
		((Listing)listing).GapLine(10f);
		DoLandmarkSection(listing);
	}

	private void DoLandmarkSection(Listing_Standard listing)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		if (!ABSkyLandmarks.SystemActive)
		{
			GUI.color = NoteDim;
			listing.Label(Translator.Translate("AB_LandmarksNeedOdyssey"), -1f, (string)null);
			GUI.color = Color.white;
			return;
		}
		bool flag = skyLandmarks;
		bool flag2 = landmarksExpanded;
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_SkyLandmarks")), ref skyLandmarks, TaggedString.op_Implicit(Translator.Translate("AB_SkyLandmarksTip")), 0f, 1f);
		if (!flag)
		{
			return;
		}
		((Listing)listing).Indent(16f);
		((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
		listing.Label(Translator.Translate("AB_LandmarkChance") + ": " + GenText.ToStringPercent(skyLandmarkChance), -1f, TaggedString.op_Implicit(Translator.Translate("AB_LandmarkChanceTip")));
		skyLandmarkChance = listing.Slider(skyLandmarkChance, 0f, 1f);
		listing.Label(TaggedString.op_Implicit(TaggedString.op_Implicit(Translator.Translate("AB_LandmarkMax") + ": ") + skyLandmarkMax), -1f, TaggedString.op_Implicit(Translator.Translate("AB_LandmarkMaxTip")));
		skyLandmarkMax = Mathf.RoundToInt(listing.Slider((float)skyLandmarkMax, 1f, 3f));
		List<LandmarkDef> list = ABSkyLandmarks.AllLandmarks();
		if (listing.ButtonText(TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate(flag2 ? "AB_LandmarksCollapse" : "AB_LandmarksExpand", NamedArgument.op_Implicit(list.Count))), (string)null, 1f))
		{
			landmarksExpandedPending = !flag2;
		}
		if (flag2)
		{
			Rect val2 = default(Rect);
			for (int i = 0; i < list.Count; i++)
			{
				LandmarkDef val = list[i];
				Rect rect = ((Listing)listing).GetRect(28f, 1f);
				Widgets.DrawHighlightIfMouseover(rect);
				((Rect)(ref val2))._002Ector(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + 3f, 22f, 22f);
				try
				{
					if (!GenText.NullOrEmpty(val.iconTexturePath))
					{
						GUI.DrawTexture(val2, (Texture)(object)val.Texture, (ScaleMode)2);
					}
				}
				catch
				{
				}
				string text = GenText.CapitalizeFirst(ABSkyLandmarks.DisplayLabel(val));
				ModContentPack modContentPack = ((Def)val).modContentPack;
				string text2 = ((modContentPack != null) ? modContentPack.Name : null);
				Widgets.Label(new Rect(((Rect)(ref rect)).x + 28f, ((Rect)(ref rect)).y + 4f, ((Rect)(ref rect)).width - 28f - 116f, 22f), text);
				if (!GenText.NullOrEmpty(text2))
				{
					GUI.color = NoteDim;
					Text.Font = (GameFont)1;
					Vector2 val3 = Text.CalcSize(text);
					float num = ((Rect)(ref rect)).x + 28f + Mathf.Min(val3.x, ((Rect)(ref rect)).width - 28f - 200f) + 8f;
					Widgets.Label(new Rect(num, ((Rect)(ref rect)).y + 4f, Mathf.Max(0f, ((Rect)(ref rect)).xMax - 116f - num), 22f), text2);
					GUI.color = Color.white;
				}
				string text3 = ABSkyLandmarks.DescriptionFor(val);
				if (!GenText.NullOrEmpty(text3))
				{
					TooltipHandler.TipRegion(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width - 116f, ((Rect)(ref rect)).height), TipSignal.op_Implicit(text3));
				}
				int num2 = ABSkyLandmarks.ModeFor(this, val);
				if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).xMax - 112f, ((Rect)(ref rect)).y + 2f, 110f, 24f), ABSkyLandmarks.ModeLabel(num2), true, true, true, (TextAnchor?)null))
				{
					ABSkyLandmarks.SetMode(this, val, (num2 + 1) % 4);
				}
			}
		}
		((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
		((Listing)listing).Outdent(16f);
	}

	private void ApplyPeakPreset(float cutoff, float scale, int terrace, float outcrops, float tarns, float soil, float vegetation)
	{
		naturalPeaks = true;
		peakMeadowCutoff = cutoff;
		peakMeadowScale = scale;
		peakTerraceMax = terrace;
		peakOutcropDensity = outcrops;
		peakTarns = tarns;
		peakSoilFraction = soil;
		peakVegetation = vegetation;
	}

	private void DoViewTab(Listing_Standard listing)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		bool flag = drawWallReveal;
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ShowLiveBelow")), ref showLiveBelow, TaggedString.op_Implicit(Translator.Translate("AB_ShowLiveBelowTip")), 0f, 1f);
		listing.Label(Translator.Translate("AB_BelowDim") + ": " + GenText.ToStringPercent(belowDim), -1f, TaggedString.op_Implicit(Translator.Translate("AB_BelowDimTip")));
		belowDim = listing.Slider(belowDim, 0f, 0.8f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_SlabEdge")), ref drawSlabEdge, TaggedString.op_Implicit(Translator.Translate("AB_SlabEdgeTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_WallReveal")), ref drawWallReveal, TaggedString.op_Implicit(Translator.Translate("AB_WallRevealTip")), 0f, 1f);
		if (flag)
		{
			((Listing)listing).Indent(16f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
			listing.Label(Translator.Translate("AB_WallRevealWidth") + ": " + wallRevealWidth.ToString("0.00"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_WallRevealWidthTip")));
			float num = listing.Slider(wallRevealWidth, 0.25f, 0.6f);
			if (Mathf.Abs(num - wallRevealWidth) > 0.0005f)
			{
				DirtyBelowThingsLayers();
			}
			wallRevealWidth = num;
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
			((Listing)listing).Outdent(16f);
		}
		listing.Label(Translator.Translate("AB_BelowScale") + ": " + GenText.ToStringPercent(belowThingScale), -1f, TaggedString.op_Implicit(Translator.Translate("AB_BelowScaleTip")));
		float num2 = listing.Slider(belowThingScale, 0.7f, 1f);
		if (Mathf.Abs(num2 - belowThingScale) > 0.0005f)
		{
			DirtyBelowThingsLayers();
		}
		belowThingScale = num2;
		((Listing)listing).GapLine(10f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_SelectBelowInPlace")), ref selectBelowInPlace, TaggedString.op_Implicit(Translator.Translate("AB_SelectBelowInPlaceTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ShowCeilingHint")), ref showCeilingHint, TaggedString.op_Implicit(Translator.Translate("AB_ShowCeilingHintTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ShowLevelWidget")), ref showLevelWidget, TaggedString.op_Implicit(Translator.Translate("AB_ShowLevelWidgetTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_OneColonistBar")), ref oneColonistBar, TaggedString.op_Implicit(Translator.Translate("AB_OneColonistBarTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CameraFollowStairs")), ref cameraFollowStairs, TaggedString.op_Implicit(Translator.Translate("AB_CameraFollowStairsTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CameraLockKeybind")), ref cameraLockKeybind, TaggedString.op_Implicit(Translator.Translate("AB_CameraLockKeybindTip")), 0f, 1f);
	}

	private void DoWorkTab(Listing_Standard listing)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		bool flag = crossLevelWork;
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelWork")), ref crossLevelWork, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelWorkTip")), 0f, 1f);
		if (flag)
		{
			((Listing)listing).Indent(16f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
			listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_PriorityCrossLevelWork")), ref priorityCrossLevelWork, TaggedString.op_Implicit(Translator.Translate("AB_PriorityCrossLevelWorkTip")), 0f, 1f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
			((Listing)listing).Outdent(16f);
		}
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelOrders")), ref crossLevelOrders, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelOrdersTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelHauling")), ref crossLevelHauling, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelHaulingTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelSupply")), ref crossLevelSupply, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelSupplyTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelNeeds")), ref crossLevelNeeds, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelNeedsTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelPrisoners")), ref crossLevelPrisoners, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelPrisonersTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelSocial")), ref crossLevelSocial, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelSocialTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelRituals")), ref crossLevelRituals, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelRitualsTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_AnimalWander")), ref crossLevelAnimalWander, TaggedString.op_Implicit(Translator.Translate("AB_AnimalWanderTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_IdleReturnHome")), ref idleReturnHome, TaggedString.op_Implicit(Translator.Translate("AB_IdleReturnHomeTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelPipes")), ref crossLevelPipes, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelPipesTip")), 0f, 1f);
		((Listing)listing).GapLine(10f);
		listing.Label(Translator.Translate("AB_ClimbTime") + ": " + GenText.ToStringPercent(climbTimeMultiplier), -1f, TaggedString.op_Implicit(Translator.Translate("AB_ClimbTimeTip")));
		climbTimeMultiplier = listing.Slider(climbTimeMultiplier, 0.25f, 3f);
	}

	private void DoCombatTab(Listing_Standard listing)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = crossLevelCombat;
		bool flag2 = threatBasementInfest || threatSkyDrops;
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelCombat")), ref crossLevelCombat, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelCombatTip")), 0f, 1f);
		if (flag)
		{
			((Listing)listing).Indent(16f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
			listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelAutoEngage")), ref crossLevelAutoEngage, TaggedString.op_Implicit(Translator.Translate("AB_CrossLevelAutoEngageTip")), 0f, 1f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
			((Listing)listing).Outdent(16f);
		}
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_PodTransit")), ref podTransit, TaggedString.op_Implicit(Translator.Translate("AB_PodTransitTip")), 0f, 1f);
		((Listing)listing).GapLine(10f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ThreatBasementInfest")), ref threatBasementInfest, TaggedString.op_Implicit(Translator.Translate("AB_ThreatBasementInfestTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ThreatSkyDrops")), ref threatSkyDrops, TaggedString.op_Implicit(Translator.Translate("AB_ThreatSkyDropsTip")), 0f, 1f);
		if (flag2)
		{
			((Listing)listing).Indent(16f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth - 16f;
			listing.Label(Translator.Translate("AB_ThreatDivertChance") + ": " + GenText.ToStringPercent(threatDivertChance), -1f, TaggedString.op_Implicit(Translator.Translate("AB_ThreatDivertChanceTip")));
			threatDivertChance = listing.Slider(threatDivertChance, 0.05f, 1f);
			((Listing)listing).ColumnWidth = ((Listing)listing).ColumnWidth + 16f;
			((Listing)listing).Outdent(16f);
		}
	}

	private void DoAdvancedTab(Listing_Standard listing)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_ColumnWealth")), ref columnWealth, TaggedString.op_Implicit(Translator.Translate("AB_ColumnWealthTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_WorldIntegration")), ref worldIntegration, TaggedString.op_Implicit(Translator.Translate("AB_WorldIntegrationTip")), 0f, 1f);
		listing.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("AB_VerboseLogging")), ref verboseLogging, TaggedString.op_Implicit(Translator.Translate("AB_VerboseLoggingTip")), 0f, 1f);
		((Listing)listing).GapLine(10f);
		listing.Label(Translator.Translate("AB_GuardPanel"), -1f, TaggedString.op_Implicit(Translator.Translate("AB_GuardPanelTip")));
		ABGuardSwitch[] allSwitches = ABGuard.AllSwitches;
		int num = 0;
		foreach (ABGuardSwitch aBGuardSwitch in allSwitches)
		{
			if (!aBGuardSwitch.IsOn)
			{
				num++;
				Rect rect = ((Listing)listing).GetRect(28f, 1f);
				GUI.color = ColorLibrary.RedReadable;
				Widgets.Label(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + 3f, ((Rect)(ref rect)).width - 100f, 24f), TranslatorFormattedStringExtensions.Translate("AB_GuardTripped", NamedArgument.op_Implicit(aBGuardSwitch.Name), NamedArgument.op_Implicit(aBGuardSwitch.LastContext ?? "?")));
				GUI.color = Color.white;
				if (Widgets.ButtonText(new Rect(((Rect)(ref rect)).xMax - 94f, ((Rect)(ref rect)).y + 1f, 92f, 26f), TaggedString.op_Implicit(Translator.Translate("AB_ReArm")), true, true, true, (TextAnchor?)null))
				{
					ABGuard.ReArm(aBGuardSwitch);
				}
			}
		}
		if (num == 0)
		{
			GUI.color = OkGreen;
			listing.Label(TranslatorFormattedStringExtensions.Translate("AB_GuardAllGreen", NamedArgument.op_Implicit(allSwitches.Length)), -1f, (string)null);
			GUI.color = Color.white;
		}
		((Listing)listing).GapLine(10f);
		if (listing.ButtonText(TaggedString.op_Implicit(Translator.Translate("AB_ResetAll")), (string)null, 1f))
		{
			Find.WindowStack.Add((Window)(object)Dialog_MessageBox.CreateConfirmation(Translator.Translate("AB_ResetAllConfirm"), (Action)ResetAll, true, (string)null, (WindowLayer)1));
		}
	}

	private void TabResetRow(Listing_Standard listing, int tab)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((Listing)listing).Gap(14f);
		if (listing.ButtonText(TaggedString.op_Implicit(Translator.Translate("AB_ResetTab")), (string)null, 1f))
		{
			ResetTab(tab);
		}
	}

	private void ResetTab(int tab)
	{
		switch (tab)
		{
		case 0:
			ResetGeneration();
			break;
		case 1:
			ResetView();
			break;
		case 2:
			ResetWork();
			break;
		case 3:
			ResetCombat();
			break;
		default:
			ResetAdvanced();
			break;
		}
	}

	private void ResetGeneration()
	{
		skyLandmarks = true;
		skyLandmarkChance = 0.3f;
		skyLandmarkMax = 1;
		landmarkModes?.Clear();
		skyBiomeInherit = true;
		naturalPeaks = true;
		peakMeadowCutoff = 0.6f;
		peakMeadowScale = 0.024f;
		peakTerraceMax = 4;
		peakOutcropDensity = 1f;
		peakTarns = 1f;
		peakHiddenValleys = 1f;
		peakSoilFraction = 0.15f;
		peakVegetation = 1f;
		skyOreDensity = 6f;
		basementOreDensity = 6f;
		cavernBasements = true;
		cavernBiome = "Random";
		cavernOpenness = 0.35f;
		cavernChamberFreq = 0.02f;
		cavernFormations = 1f;
	}

	private void ResetView()
	{
		showLiveBelow = true;
		belowDim = 0.06f;
		drawSlabEdge = true;
		drawWallReveal = true;
		wallRevealWidth = 0.5f;
		belowThingScale = 0.85f;
		selectBelowInPlace = true;
		showCeilingHint = true;
		showLevelWidget = true;
		oneColonistBar = true;
		cameraFollowStairs = true;
		cameraLockKeybind = true;
		DirtyBelowThingsLayers();
	}

	private void ResetWork()
	{
		crossLevelWork = true;
		priorityCrossLevelWork = true;
		crossLevelOrders = true;
		crossLevelHauling = true;
		crossLevelSupply = true;
		crossLevelNeeds = true;
		crossLevelPrisoners = true;
		crossLevelSocial = true;
		crossLevelRituals = true;
		crossLevelAnimalWander = true;
		idleReturnHome = true;
		crossLevelPipes = true;
		climbTimeMultiplier = 1f;
	}

	private void ResetCombat()
	{
		crossLevelCombat = true;
		crossLevelAutoEngage = true;
		podTransit = true;
		threatBasementInfest = false;
		threatSkyDrops = false;
		threatDivertChance = 0.25f;
	}

	private void ResetAdvanced()
	{
		columnWealth = true;
		worldIntegration = true;
		verboseLogging = false;
	}

	private void ResetAll()
	{
		ResetGeneration();
		ResetView();
		ResetWork();
		ResetCombat();
		ResetAdvanced();
	}

	private static void DirtyBelowThingsLayers()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)Current.ProgramState != 2)
		{
			return;
		}
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			LevelComp levelComp = maps[i].Levels();
			if (levelComp != null && levelComp.level > 0)
			{
				maps[i].mapDrawer.WholeMapChanged(MapMeshFlagDef.op_Implicit(ABDefOf.AB_BelowThings));
			}
		}
	}

	public override void ExposeData()
	{
		((ModSettings)this).ExposeData();
		Scribe_Values.Look<bool>(ref verboseLogging, "verboseLogging", false, false);
		Scribe_Values.Look<bool>(ref showLiveBelow, "showLiveBelow", true, false);
		Scribe_Values.Look<bool>(ref selectBelowInPlace, "selectBelowInPlace", true, false);
		Scribe_Values.Look<bool>(ref showCeilingHint, "showCeilingHint", true, false);
		Scribe_Values.Look<bool>(ref showLevelWidget, "showLevelWidget", true, false);
		Scribe_Values.Look<bool>(ref oneColonistBar, "oneColonistBar", true, false);
		Scribe_Values.Look<bool>(ref cameraFollowStairs, "cameraFollowStairs", true, false);
		Scribe_Values.Look<bool>(ref cameraLockKeybind, "cameraLockKeybind", true, false);
		Scribe_Values.Look<float>(ref climbTimeMultiplier, "climbTimeMultiplier", 1f, false);
		Scribe_Values.Look<float>(ref belowDim, "belowDimHeight", 0.06f, false);
		Scribe_Values.Look<bool>(ref drawSlabEdge, "drawSlabEdge", true, false);
		Scribe_Values.Look<bool>(ref drawWallReveal, "drawWallReveal", true, false);
		Scribe_Values.Look<float>(ref wallRevealWidth, "wallRevealWidth", 0.5f, false);
		Scribe_Values.Look<float>(ref belowThingScale, "belowThingScale", 0.85f, false);
		Scribe_Values.Look<bool>(ref crossLevelWork, "crossLevelWork", true, false);
		Scribe_Values.Look<bool>(ref priorityCrossLevelWork, "priorityCrossLevelWork", true, false);
		Scribe_Values.Look<bool>(ref crossLevelOrders, "crossLevelOrders", true, false);
		Scribe_Values.Look<bool>(ref crossLevelCombat, "crossLevelCombat", true, false);
		Scribe_Values.Look<bool>(ref crossLevelAutoEngage, "crossLevelAutoEngage", true, false);
		Scribe_Values.Look<bool>(ref idleReturnHome, "idleReturnHome", true, false);
		Scribe_Values.Look<bool>(ref crossLevelHauling, "crossLevelHauling", true, false);
		Scribe_Values.Look<bool>(ref crossLevelSupply, "crossLevelSupply", true, false);
		Scribe_Values.Look<bool>(ref crossLevelNeeds, "crossLevelNeeds", true, false);
		Scribe_Values.Look<bool>(ref crossLevelPrisoners, "crossLevelPrisoners", true, false);
		Scribe_Values.Look<bool>(ref crossLevelAnimalWander, "crossLevelAnimalWander", true, false);
		Scribe_Values.Look<bool>(ref crossLevelRituals, "crossLevelRituals", true, false);
		Scribe_Values.Look<bool>(ref crossLevelSocial, "crossLevelSocial", true, false);
		Scribe_Values.Look<bool>(ref crossLevelPipes, "crossLevelPipes", true, false);
		Scribe_Values.Look<bool>(ref podTransit, "podTransit", true, false);
		Scribe_Values.Look<bool>(ref crossLevelTemperature, "crossLevelTemperature", true, false);
		Scribe_Values.Look<bool>(ref threatBasementInfest, "threatBasementInfest", false, false);
		Scribe_Values.Look<bool>(ref threatSkyDrops, "threatSkyDrops", false, false);
		Scribe_Values.Look<float>(ref threatDivertChance, "threatDivertChance", 0.25f, false);
		Scribe_Values.Look<bool>(ref columnWealth, "columnWealth", true, false);
		Scribe_Values.Look<bool>(ref worldIntegration, "worldIntegration", true, false);
		Scribe_Values.Look<bool>(ref cavernBasements, "cavernBasements", true, false);
		Scribe_Values.Look<string>(ref cavernBiome, "cavernBiome", "Random", false);
		Scribe_Values.Look<float>(ref cavernOpenness, "cavernOpenness", 0.35f, false);
		Scribe_Values.Look<bool>(ref naturalPeaks, "naturalPeaks", true, false);
		Scribe_Values.Look<bool>(ref skyBiomeInherit, "skyBiomeInherit", true, false);
		Scribe_Values.Look<float>(ref peakSoilFraction, "peakSoilFraction", 0.15f, false);
		Scribe_Values.Look<float>(ref peakVegetation, "peakVegetation", 1f, false);
		Scribe_Values.Look<float>(ref peakMeadowCutoff, "peakMeadowCutoff", 0.6f, false);
		Scribe_Values.Look<float>(ref peakMeadowScale, "peakMeadowScale", 0.024f, false);
		Scribe_Values.Look<int>(ref peakTerraceMax, "peakTerraceMax", 4, false);
		Scribe_Values.Look<float>(ref peakOutcropDensity, "peakOutcropDensity", 1f, false);
		Scribe_Values.Look<float>(ref peakTarns, "peakTarns", 1f, false);
		Scribe_Values.Look<float>(ref peakHiddenValleys, "peakHiddenValleys", 1f, false);
		Scribe_Values.Look<float>(ref skyOreDensity, "skyOreDensity", 6f, false);
		Scribe_Values.Look<float>(ref basementOreDensity, "basementOreDensity", 6f, false);
		Scribe_Values.Look<float>(ref cavernChamberFreq, "cavernChamberFreq", 0.02f, false);
		Scribe_Values.Look<float>(ref cavernFormations, "cavernFormations", 1f, false);
		Scribe_Values.Look<bool>(ref skyLandmarks, "skyLandmarks", true, false);
		Scribe_Values.Look<float>(ref skyLandmarkChance, "skyLandmarkChance", 0.3f, false);
		Scribe_Values.Look<int>(ref skyLandmarkMax, "skyLandmarkMax", 1, false);
		Scribe_Collections.Look<string, int>(ref landmarkModes, "landmarkModes", (LookMode)1, (LookMode)1);
	}
}
