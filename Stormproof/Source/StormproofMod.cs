using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class StormproofMod : Mod
    {
        // Defaults until the mod constructor loads the saved values, so callers
        // never need a null check.
        public static StormproofSettings Settings { get; private set; } = new StormproofSettings();
        private Vector2 settingsScroll;
        private float settingsContentHeight = 2000f;

        public StormproofMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<StormproofSettings>();
        }

        private static bool azraelResolved;
        private static MethodInfo azraelFactorFor;
        private static StorytellerDef azraelTeller;

        // The Azrael storyteller weights Stormproof picks on top of the frequency
        // slider (Azrael_SeriesIncidentWeights). Show the combined rate so the two
        // settings don't surprise anyone. Null when Azrael is not loaded.
        private static string AzraelStackLine()
        {
            if (!azraelResolved)
            {
                azraelResolved = true;
                azraelTeller = DefDatabase<StorytellerDef>.GetNamedSilentFail("Azrael");
                azraelFactorFor = GenTypes.GetTypeInAnyAssembly("Azrael.StorytellerIncidentWeights")
                    ?.GetMethod("FactorFor", new[] { typeof(StorytellerDef), typeof(IncidentDef) });
            }
            if (azraelTeller == null || azraelFactorFor == null)
            {
                return null;
            }
            var parts = new List<string>();
            foreach (IncidentDef incident in StormproofIncidentDefOf.All)
            {
                if (incident == null)
                {
                    continue;
                }
                float factor;
                try
                {
                    factor = (float)azraelFactorFor.Invoke(null, new object[] { azraelTeller, incident });
                }
                catch (Exception)
                {
                    azraelFactorFor = null;
                    return null;
                }
                if (factor != 1f)
                {
                    parts.Add(incident.label + " " + (Settings.incidentFrequency * factor).ToStringPercent());
                }
            }
            return parts.Count == 0
                ? null
                : "Stormproof_Settings_AzraelStack".Translate(string.Join(", ", parts));
        }

        public override string SettingsCategory() => "Stormproof_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            int before = Settings.ContentFingerprint();

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

            listing.CheckboxLabeled("Stormproof_Settings_WarnUndefended".Translate(),
                ref Settings.warnUndefendedGrid, "Stormproof_Settings_WarnUndefendedTip".Translate());

            listing.GapLine();
            listing.Label("Stormproof_Settings_Sky".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Almanac".Translate(),
                ref Settings.enableAlmanac, "Stormproof_Settings_AlmanacTip".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_Fulgurite".Translate(),
                ref Settings.enableFulgurite, "Stormproof_Settings_FulguriteTip".Translate());
            listing.Label("Stormproof_Settings_StormCallerCooldown".Translate(
                Settings.stormCallerCooldownDays.ToString("F1")));
            Settings.stormCallerCooldownDays = listing.Slider(Settings.stormCallerCooldownDays, 1f, 15f);
            listing.Label("Stormproof_Settings_StormCallerHours".Translate(
                Settings.stormCallerHours.ToString("F0")));
            Settings.stormCallerHours = listing.Slider(Settings.stormCallerHours, 2f, 24f);

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
            string azrael = AzraelStackLine();
            if (azrael != null)
            {
                listing.Label(azrael);
            }

            listing.GapLine();
            listing.Label("Stormproof_Settings_Visuals".Translate());
            listing.CheckboxLabeled("Stormproof_Settings_VisualEffects".Translate(),
                ref Settings.visualEffects, "Stormproof_Settings_VisualEffectsTip".Translate());
            if (Settings.visualEffects)
            {
                listing.CheckboxLabeled("Stormproof_Settings_ReducedEffects".Translate(),
                    ref Settings.reducedEffects, "Stormproof_Settings_ReducedEffectsTip".Translate());
            }

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
            if (ModsConfig.BiotechActive)
            {
                listing.CheckboxLabeled("Stormproof_Settings_DampenerMechs".Translate(),
                    ref Settings.dampenerCoversMechs, "Stormproof_Settings_DampenerMechsTip".Translate());
            }

            listing.Gap(10f);
            if (listing.ButtonText("Stormproof_Settings_Reset".Translate(), null, 0.25f))
            {
                Settings.ResetToDefaults();
            }

            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            Settings.Clamp();
            if (Settings.ContentFingerprint() != before)
            {
                Settings.ApplyIncidentChances();
                Settings.Write();
            }
        }
    }
}
