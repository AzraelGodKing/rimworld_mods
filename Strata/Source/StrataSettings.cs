using UnityEngine;
using Verse;

namespace Strata
{
    // Atmosphere sim fidelity vs CPU. Low spreads work and throttles background
    // levels; High keeps the viewed level snappy.
    public enum AtmosphereQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    // Mod options. Read as statics from hot paths, so everything is a plain
    // field with a cheap default.
    public class StrataSettings : ModSettings
    {
        public bool naturalGasesEnabled = false;
        public bool pollutantGasesEnabled = false;
        public float smokeSeverityScale = 1f;
        public bool gasOverlayRoomLabels = false;
        public bool gasEventsEnabled = false;
        public bool raidPursuitEnabled = true;
        public const int CurrentSettingsVersion = 3;

        public int settingsVersion = CurrentSettingsVersion;
        public bool workRelayEnabled = false;
        public bool robotSoftCompatEnabled = true;
        public bool robotWorkRelayEnabled = false;
        public bool performanceModeEnabled = false;
        public bool offThreadAtmosphere = true;
        public AtmosphereQualityLevel atmosphereQuality = AtmosphereQualityLevel.Medium;
        public bool logRobotDiagnostics = false;
        public bool foodRelayEnabled = true;
        public bool restRelayEnabled = true;
        public bool medicalRelayEnabled = true;
        public bool joyRelayEnabled = true;
        public bool caravanPullEnabled = true;
        public bool throttleVacantLevels = true;
        public bool hibernateEmptyLevels = true;
        public bool reduceBackgroundLevels = true;
        public bool showLevelPerfInTab = false;
        public bool explorationSitesEnabled = true;
        public bool floodEventsEnabled = true;
        public bool crossLevelRitualsEnabled = true;
        public bool crossLevelCaravansEnabled = true;
        public bool mergedAbandonWarning = true;
        public bool showLinkedColonistsInWorkScheduleTabs = true;
        public bool nativeCavernLayoutEnabled = true;
        public bool biomesCavernsCompatEnabled = true;
        public bool ancientColonyStairwellEnabled = true;
        public float ancientColonyStairwellChance = 0.35f;
        public bool cageSustainHunger = false;
        public bool multiFloorStairs = false;
        public KeyCode viewLevelUpKey = KeyCode.PageUp;
        public KeyCode viewLevelDownKey = KeyCode.PageDown;

        public bool WorkRelayActive => workRelayEnabled && !performanceModeEnabled;

        public bool RobotSoftCompatActive => robotSoftCompatEnabled && !performanceModeEnabled;

        public bool NaturalGasesActive => naturalGasesEnabled;

        public bool PollutantGasesActive => pollutantGasesEnabled;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref naturalGasesEnabled, "naturalGasesEnabled", defaultValue: false);
            Scribe_Values.Look(ref pollutantGasesEnabled, "pollutantGasesEnabled", defaultValue: false);
            Scribe_Values.Look(ref smokeSeverityScale, "smokeSeverityScale", 1f);
            bool legacySmokeEnabled = true;
            bool legacyBreathingEnabled = true;
            Scribe_Values.Look(ref legacySmokeEnabled, "smokeEnabled", defaultValue: true);
            Scribe_Values.Look(ref legacyBreathingEnabled, "breathingEnabled", defaultValue: true);
            Scribe_Values.Look(ref gasOverlayRoomLabels, "gasOverlayRoomLabels", defaultValue: false);
            Scribe_Values.Look(ref gasEventsEnabled, "gasEventsEnabled", defaultValue: false);
            Scribe_Values.Look(ref raidPursuitEnabled, "raidPursuitEnabled", defaultValue: true);
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", defaultValue: 0);
            Scribe_Values.Look(ref workRelayEnabled, "workRelayEnabled", defaultValue: false);
            Scribe_Values.Look(ref robotSoftCompatEnabled, "robotSoftCompatEnabled", defaultValue: true);
            Scribe_Values.Look(ref robotWorkRelayEnabled, "robotWorkRelayEnabled", defaultValue: false);
            Scribe_Values.Look(ref performanceModeEnabled, "performanceModeEnabled", defaultValue: false);
            Scribe_Values.Look(ref offThreadAtmosphere, "offThreadAtmosphere", defaultValue: true);
            Scribe_Values.Look(ref atmosphereQuality, "atmosphereQuality", AtmosphereQualityLevel.Medium);
            Scribe_Values.Look(ref logRobotDiagnostics, "logRobotDiagnostics", defaultValue: false);
            Scribe_Values.Look(ref foodRelayEnabled, "foodRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref restRelayEnabled, "restRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref medicalRelayEnabled, "medicalRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref joyRelayEnabled, "joyRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref caravanPullEnabled, "caravanPullEnabled", defaultValue: true);
            Scribe_Values.Look(ref throttleVacantLevels, "throttleVacantLevels", defaultValue: true);
            Scribe_Values.Look(ref hibernateEmptyLevels, "hibernateEmptyLevels", defaultValue: true);
            Scribe_Values.Look(ref reduceBackgroundLevels, "reduceBackgroundLevels", defaultValue: true);
            Scribe_Values.Look(ref showLevelPerfInTab, "showLevelPerfInTab", defaultValue: false);
            Scribe_Values.Look(ref explorationSitesEnabled, "explorationSitesEnabled", defaultValue: true);
            Scribe_Values.Look(ref floodEventsEnabled, "floodEventsEnabled", defaultValue: true);
            Scribe_Values.Look(ref crossLevelRitualsEnabled, "crossLevelRitualsEnabled", defaultValue: true);
            Scribe_Values.Look(ref crossLevelCaravansEnabled, "crossLevelCaravansEnabled", defaultValue: true);
            Scribe_Values.Look(ref mergedAbandonWarning, "mergedAbandonWarning", defaultValue: true);
            Scribe_Values.Look(ref showLinkedColonistsInWorkScheduleTabs, "showLinkedColonistsInWorkScheduleTabs", defaultValue: true);
            Scribe_Values.Look(ref nativeCavernLayoutEnabled, "nativeCavernLayoutEnabled", defaultValue: true);
            Scribe_Values.Look(ref biomesCavernsCompatEnabled, "biomesCavernsCompatEnabled", defaultValue: true);
            Scribe_Values.Look(ref ancientColonyStairwellEnabled, "ancientColonyStairwellEnabled", defaultValue: true);
            Scribe_Values.Look(ref ancientColonyStairwellChance, "ancientColonyStairwellChance", 0.35f);
            Scribe_Values.Look(ref cageSustainHunger, "cageSustainHunger", defaultValue: false);
            Scribe_Values.Look(ref multiFloorStairs, "multiFloorStairs", defaultValue: false);
            Scribe_Values.Look(ref viewLevelUpKey, "viewLevelUpKey", KeyCode.PageUp);
            Scribe_Values.Look(ref viewLevelDownKey, "viewLevelDownKey", KeyCode.PageDown);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                MigrateSettings(legacySmokeEnabled, legacyBreathingEnabled);
            }
        }

        private void MigrateSettings(bool legacySmokeEnabled, bool legacyBreathingEnabled)
        {
            if (settingsVersion < 1)
            {
                bool hadWorkRelay = workRelayEnabled;
                bool hadRobotWorkRelay = robotWorkRelayEnabled;
                workRelayEnabled = false;
                robotWorkRelayEnabled = false;

                if (hadWorkRelay || hadRobotWorkRelay)
                {
                    Log.Message("[Strata] Settings migration v1"
                        + ": disabled colonist work relay and Misc. Robots work relay"
                        + " (old profiles kept them on; return/recharge soft-compat stays "
                        + (robotSoftCompatEnabled ? "enabled" : "disabled") + ").");
                }
            }

            if (settingsVersion < 2)
            {
                naturalGasesEnabled = legacyBreathingEnabled;
                pollutantGasesEnabled = legacySmokeEnabled;
            }

            if (settingsVersion < 3)
            {
                naturalGasesEnabled = false;
                pollutantGasesEnabled = false;
                Log.Message("[Strata] Settings migration: gas systems disabled by default (can cause lag).");
            }

            if (settingsVersion >= CurrentSettingsVersion)
            {
                return;
            }

            settingsVersion = CurrentSettingsVersion;
        }
    }

    public class StrataMod : Mod
    {
        public static StrataSettings Settings;

        // Which key picker is waiting for a keypress ("up", "down", or null).
        private static string listeningFor;

        public StrataMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<StrataSettings>();
        }

        public override string SettingsCategory() => "Strata_SettingsCategory".Translate();

        public override void WriteSettings()
        {
            bool wasMultiFloor = Settings.multiFloorStairs;
            base.WriteSettings();
            if (wasMultiFloor != Settings.multiFloorStairs)
            {
                StrataMultiFloorStairsUtility.Apply(Settings.multiFloorStairs);
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Capture the next keypress for whichever picker is listening.
            if (listeningFor != null && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode != KeyCode.Escape && Event.current.keyCode != KeyCode.None)
                {
                    if (listeningFor == "up")
                    {
                        Settings.viewLevelUpKey = Event.current.keyCode;
                    }
                    else
                    {
                        Settings.viewLevelDownKey = Event.current.keyCode;
                    }
                }
                listeningFor = null;
                Event.current.Use();
            }

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_LevelHotkeys".Translate());
            Text.Font = GameFont.Small;
            KeyPickerRow(listing, "Strata_Settings_ViewLevelAbove".Translate(), ref Settings.viewLevelUpKey, "up", KeyCode.PageUp);
            KeyPickerRow(listing, "Strata_Settings_ViewLevelBelow".Translate(), ref Settings.viewLevelDownKey, "down", KeyCode.PageDown);
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_ColonistRelays".Translate());
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata_Settings_WorkRelay".Translate(), ref Settings.workRelayEnabled,
                "Strata_Settings_WorkRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_RobotSoftCompat".Translate(), ref Settings.robotSoftCompatEnabled,
                "Strata_Settings_RobotSoftCompatDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_RobotWorkRelay".Translate(), ref Settings.robotWorkRelayEnabled,
                "Strata_Settings_RobotWorkRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_FoodRelay".Translate(), ref Settings.foodRelayEnabled,
                "Strata_Settings_FoodRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_RestRelay".Translate(), ref Settings.restRelayEnabled,
                "Strata_Settings_RestRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_MedicalRelay".Translate(), ref Settings.medicalRelayEnabled,
                "Strata_Settings_MedicalRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_JoyRelay".Translate(), ref Settings.joyRelayEnabled,
                "Strata_Settings_JoyRelayDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_CaravanPull".Translate(), ref Settings.caravanPullEnabled,
                "Strata_Settings_CaravanPullDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_CrossLevelRituals".Translate(), ref Settings.crossLevelRitualsEnabled,
                "Strata_Settings_CrossLevelRitualsDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_CrossLevelCaravans".Translate(), ref Settings.crossLevelCaravansEnabled,
                "Strata_Settings_CrossLevelCaravansDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_CageSustainHunger".Translate(), ref Settings.cageSustainHunger,
                "Strata_Settings_CageSustainHungerDesc".Translate());
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_SmokeGas".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            listing.Label("Strata_Settings_GasSystemWarning".Translate());
            GUI.color = Color.white;
            listing.CheckboxLabeled("Strata_Settings_PollutantGases".Translate(), ref Settings.pollutantGasesEnabled,
                "Strata_Settings_PollutantGasesDesc".Translate());
            if (Settings.pollutantGasesEnabled)
            {
                listing.Label("Strata_Settings_SmokeSeverity".Translate(Settings.smokeSeverityScale.ToStringPercent()));
                Settings.smokeSeverityScale = listing.Slider(Settings.smokeSeverityScale, 0f, 2f);
            }
            listing.CheckboxLabeled("Strata_Settings_UndergroundGas".Translate(), ref Settings.gasEventsEnabled,
                "Strata_Settings_UndergroundGasDesc".Translate());
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_DeepBreathing".Translate());
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata_Settings_NaturalGases".Translate(), ref Settings.naturalGasesEnabled,
                "Strata_Settings_NaturalGasesDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_GasOverlayLabels".Translate(), ref Settings.gasOverlayRoomLabels,
                "Strata_Settings_GasOverlayLabelsDesc".Translate());
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_ThreatsPerf".Translate());
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata_Settings_RaidPursuit".Translate(), ref Settings.raidPursuitEnabled,
                "Strata_Settings_RaidPursuitDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_HibernateEmpty".Translate(), ref Settings.hibernateEmptyLevels,
                "Strata_Settings_HibernateEmptyDesc".Translate());
            Settings.throttleVacantLevels = Settings.hibernateEmptyLevels;
            listing.CheckboxLabeled("Strata_Settings_ReduceBackground".Translate(), ref Settings.reduceBackgroundLevels,
                "Strata_Settings_ReduceBackgroundDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_PerformanceMode".Translate(), ref Settings.performanceModeEnabled,
                "Strata_Settings_PerformanceModeDesc".Translate());
            if (Settings.performanceModeEnabled)
            {
                GUI.color = Color.yellow;
                listing.Label("Strata_Settings_PerformanceModeActive".Translate());
                GUI.color = Color.white;
            }
            listing.CheckboxLabeled("Strata_Settings_OffThreadAtmosphere".Translate(), ref Settings.offThreadAtmosphere,
                "Strata_Settings_OffThreadAtmosphereDesc".Translate());
            listing.Label("Strata_Settings_AtmosphereQuality".Translate());
            DrawAtmosphereQualitySelector(listing);
            listing.GapLine();
            listing.CheckboxLabeled("Strata_Settings_ShowLevelPerf".Translate(), ref Settings.showLevelPerfInTab,
                "Strata_Settings_ShowLevelPerfDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_ExplorationSites".Translate(), ref Settings.explorationSitesEnabled,
                "Strata_Settings_ExplorationSitesDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_FloodEvents".Translate(), ref Settings.floodEventsEnabled,
                "Strata_Settings_FloodEventsDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_NativeCavern".Translate(), ref Settings.nativeCavernLayoutEnabled,
                "Strata_Settings_NativeCavernDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_AncientStairwell".Translate(), ref Settings.ancientColonyStairwellEnabled,
                "Strata_Settings_AncientStairwellDesc".Translate());
            if (Settings.ancientColonyStairwellEnabled)
            {
                listing.Label("Strata_Settings_AncientStairwellChance".Translate(Settings.ancientColonyStairwellChance.ToStringPercent()));
                Settings.ancientColonyStairwellChance = listing.Slider(Settings.ancientColonyStairwellChance, 0.05f, 1f);
            }
            if (BiomesCavernsUtility.IsActive)
            {
                listing.CheckboxLabeled("Strata_Settings_BiomesCaverns".Translate(), ref Settings.biomesCavernsCompatEnabled,
                    "Strata_Settings_BiomesCavernsDesc".Translate());
            }
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_RobotDiagnostics".Translate());
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata_Settings_LogRobotDiagnostics".Translate(), ref Settings.logRobotDiagnostics,
                "Strata_Settings_LogRobotDiagnosticsDesc".Translate());
            DrawRobotDiagnostics(listing);
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_Appearance".Translate());
            Text.Font = GameFont.Small;
            bool previousMultiFloorStairs = Settings.multiFloorStairs;
            listing.CheckboxLabeled("Strata_Settings_MultiFloorStairs".Translate(), ref Settings.multiFloorStairs,
                "Strata_Settings_MultiFloorStairsDesc".Translate());
            if (Settings.multiFloorStairs)
            {
                GUI.color = Color.yellow;
                listing.Label("Strata_Settings_MultiFloorCredit".Translate());
                GUI.color = Color.white;
            }
            if (previousMultiFloorStairs != Settings.multiFloorStairs)
            {
                StrataMultiFloorStairsUtility.Apply(Settings.multiFloorStairs);
            }
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Strata_Settings_Interface".Translate());
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata_Settings_LinkedColonistsTabs".Translate(), ref Settings.showLinkedColonistsInWorkScheduleTabs,
                "Strata_Settings_LinkedColonistsTabsDesc".Translate());
            listing.CheckboxLabeled("Strata_Settings_MergedAbandonWarning".Translate(), ref Settings.mergedAbandonWarning,
                "Strata_Settings_MergedAbandonWarningDesc".Translate());

            listing.End();
        }

        private static void KeyPickerRow(Listing_Standard listing, string label, ref KeyCode key, string id, KeyCode defaultKey)
        {
            Rect rect = listing.GetRect(30f);
            Widgets.Label(rect.LeftPart(0.5f), label);
            Rect button = new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.3f, 28f);
            string text = listeningFor == id ? "Strata_Settings_PressKey".Translate() : key.ToString();
            if (Widgets.ButtonText(button, text))
            {
                listeningFor = listeningFor == id ? null : id;
            }
            Rect reset = new Rect(button.xMax + 6f, rect.y, rect.width * 0.2f - 6f, 28f);
            if (key != defaultKey && Widgets.ButtonText(reset, "Strata_Settings_Reset".Translate()))
            {
                key = defaultKey;
                listeningFor = null;
            }
            listing.Gap(2f);
        }

        private static void DrawAtmosphereQualitySelector(Listing_Standard listing)
        {
            listing.Label("Strata_Settings_AtmosphereQualityDesc".Translate());
            if (listing.RadioButton("Strata_Settings_AtmosphereQualityLow".Translate(),
                    Settings.atmosphereQuality == AtmosphereQualityLevel.Low, tooltip: "Strata_Settings_AtmosphereQualityLowDesc".Translate()))
            {
                Settings.atmosphereQuality = AtmosphereQualityLevel.Low;
            }
            if (listing.RadioButton("Strata_Settings_AtmosphereQualityMedium".Translate(),
                    Settings.atmosphereQuality == AtmosphereQualityLevel.Medium, tooltip: "Strata_Settings_AtmosphereQualityMediumDesc".Translate()))
            {
                Settings.atmosphereQuality = AtmosphereQualityLevel.Medium;
            }
            if (listing.RadioButton("Strata_Settings_AtmosphereQualityHigh".Translate(),
                    Settings.atmosphereQuality == AtmosphereQualityLevel.High, tooltip: "Strata_Settings_AtmosphereQualityHighDesc".Translate()))
            {
                Settings.atmosphereQuality = AtmosphereQualityLevel.High;
            }
        }

        private static void DrawRobotDiagnostics(Listing_Standard listing)
        {
            foreach (StrataRobotDiagnostics.Counter counter in System.Enum.GetValues(typeof(StrataRobotDiagnostics.Counter)))
            {
                listing.Label("Strata_Settings_RobotDiagLine".Translate(
                    StrataRobotDiagnostics.Label(counter),
                    StrataRobotDiagnostics.Total(counter).ToString(),
                    StrataRobotDiagnostics.RatePer60Seconds(counter).ToString()));
            }
            if (listing.ButtonText("Strata_Settings_RobotDiagReset".Translate()))
            {
                StrataRobotDiagnostics.ResetCounters();
            }
        }
    }
}
