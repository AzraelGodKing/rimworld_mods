using UnityEngine;
using Verse;

namespace Stormproof
{
    public class StormproofMod : Mod
    {
        public static StormproofSettings Settings;
        private Vector2 settingsScroll;
        private float settingsContentHeight = 2000f;

        public StormproofMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<StormproofSettings>();
        }

        public override string SettingsCategory() => "Stormproof_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
            {
                return;
            }

            Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));

            listing.Label("Stormproof_Settings_Intro".Translate());
            listing.Gap(6f);

            Rect presetRow = listing.GetRect(28f);
            float bw = (presetRow.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(presetRow.x, presetRow.y, bw, 28f),
                    "Stormproof_Settings_PresetSoft".Translate()))
            {
                Settings.ApplySoft();
            }
            if (Widgets.ButtonText(new Rect(presetRow.x + bw + 8f, presetRow.y, bw, 28f),
                    "Stormproof_Settings_PresetDefault".Translate()))
            {
                Settings.ResetToDefaults();
            }
            if (Widgets.ButtonText(new Rect(presetRow.x + 2f * (bw + 8f), presetRow.y, bw, 28f),
                    "Stormproof_Settings_PresetHard".Translate()))
            {
                Settings.ApplyHard();
            }

            listing.GapLine();
            listing.Label("Stormproof_Settings_Grid".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Brownout".Translate(),
                ref Settings.enableBrownout, "Stormproof_Settings_BrownoutTip".Translate());
            listing.Label("Stormproof_Settings_BrownoutSeverity".Translate(
                Settings.brownoutSeverity.ToString("F2")));
            Settings.brownoutSeverity = listing.Slider(Settings.brownoutSeverity, 0.25f, 2f);
            listing.CheckboxLabeled("Stormproof_Settings_Wear".Translate(),
                ref Settings.enableStormWear, "Stormproof_Settings_WearTip".Translate());
            listing.Label("Stormproof_Settings_Zzzt".Translate(
                Settings.zzztChanceFactor.ToStringPercent()));
            Settings.zzztChanceFactor = listing.Slider(Settings.zzztChanceFactor, 0f, 3f);

            listing.GapLine();
            listing.Label("Stormproof_Settings_Sky".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Almanac".Translate(),
                ref Settings.enableAlmanac, "Stormproof_Settings_AlmanacTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Fulgurite".Translate(),
                ref Settings.enableFulgurite, "Stormproof_Settings_FulguriteTip".Translate());

            listing.GapLine();
            listing.Label("Stormproof_Settings_Incidents".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_IonStorm".Translate(), ref Settings.incidentIonStorm);
            listing.CheckboxLabeled("Stormproof_Settings_HeatDome".Translate(), ref Settings.incidentHeatDome);
            listing.CheckboxLabeled("Stormproof_Settings_PolarFront".Translate(), ref Settings.incidentPolarFront);
            listing.CheckboxLabeled("Stormproof_Settings_ToxicSurge".Translate(), ref Settings.incidentToxicSurge);
            listing.CheckboxLabeled("Stormproof_Settings_DryLightning".Translate(), ref Settings.incidentDryLightning);
            listing.Label("Stormproof_Settings_IncidentFreq".Translate(
                Settings.incidentFrequency.ToStringPercent()));
            Settings.incidentFrequency = listing.Slider(Settings.incidentFrequency, 0f, 2.5f);

            listing.GapLine();
            listing.Label("Stormproof_Settings_Suppressors".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Barrier".Translate(),
                ref Settings.allowAtmosphericBarrier, "Stormproof_Settings_SuppressorTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Stabilizer".Translate(),
                ref Settings.allowClimateStabilizer, "Stormproof_Settings_SuppressorTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_SkyRestorer".Translate(),
                ref Settings.allowSkyRestorer, "Stormproof_Settings_SuppressorTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_FireSuppressor".Translate(),
                ref Settings.allowFireSuppressor, "Stormproof_Settings_SuppressorTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Drought".Translate(),
                ref Settings.allowDroughtCondenser, "Stormproof_Settings_SuppressorTip".Translate());

            listing.Gap(10f);
            if (listing.ButtonText("Stormproof_Settings_Reset".Translate(), null, 0.25f))
            {
                Settings.ResetToDefaults();
            }

            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            Settings.Clamp();
            Settings.ApplyIncidentChances();
            Settings.Write();
        }
    }
}
