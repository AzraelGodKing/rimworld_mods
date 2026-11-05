using UnityEngine;
using Verse;

namespace Strata
{
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
        public const int CurrentSettingsVersion = 4;

        public int settingsVersion = CurrentSettingsVersion;
        public bool workRelayEnabled = true;

        // 0 disables leaving a local job for better work on another floor.
        public int priorityWorkMaxDepth = 2;

        public bool priorityWorkEmergency = true;
        public bool haulAcrossLevelsEnabled = true;
        public bool robotSoftCompatEnabled = true;
        public bool robotWorkRelayEnabled = false;
        public bool shaftFluidEnabled = true;
        public bool performanceModeEnabled = false;
        public bool offThreadAtmosphere = true;
        public bool offThreadWorkRelay = true;
        public AtmosphereQualityLevel atmosphereQuality = AtmosphereQualityLevel.Medium;
        public bool logRobotDiagnostics = false;
        public bool verboseLogging = false;
        // Off skips C# auto-tag; XML exhaust patches still apply.
        public bool autoTagExhaust = true;
        public bool foodRelayEnabled = true;
        public bool restRelayEnabled = true;
        public bool medicalRelayEnabled = true;
        public bool joyRelayEnabled = true;
        // Prototype: vanilla job scan on linked levels replaces work/food/joy/patient relays.
        public bool genericCrossFloorScan = false;
        public bool caravanPullEnabled = true;
        public bool hibernateEmptyLevels = true;
        public bool reduceBackgroundLevels = true;
        public bool lightweightOccupiedLevels = true;
        public bool showLevelPerfInTab = false;
        public bool explorationSitesEnabled = true;
        public bool floodEventsEnabled = true;
        public bool b1InfestationsEnabled = true;
        public bool noiseInfestationEnabled = true;
        public float noiseInfestationScale = 1f;
        public float noiseHalfLifeHours = 4f;
        public bool crossLevelRitualsEnabled = true;
        public bool crossLevelCaravansEnabled = true;
        public bool mergedAbandonWarning = true;
        public bool showLinkedColonistsInWorkScheduleTabs = true;
        public bool nativeCavernLayoutEnabled = true;
        public bool biomesCavernsCompatEnabled = true;
        public bool ancientColonyStairwellEnabled = true;
        public float ancientColonyStairwellChance = 0.35f;
        public bool cageSustainHunger = false;
        public bool depthDimEnabled = false;
        public float depthDimStrength = 0.35f;
        public bool seeBelowEnabled = true;
        public bool seeBelowLive = true;
        public float belowDim = 0.12f;
        public float belowThingScale = 0.85f;
        public bool combinedLevelResources = false;
        public bool crossLevelCombatEnabled = true;
        public bool crossLevelAutoEngage = true;
        public bool gravshipLifeSupportEnabled = true;
        // Default off: new digs/towers stop at ±2 from the stack root. Existing floors keep working.
        public bool unlimitedLevelsEnabled = false;

        // The magma layer is the bottom of the world (B3, or this depth with unlimited levels).
        public bool magmaLayerEnabled = true;

        public int magmaLayerDepth = MagmaLayer.DefaultUnlimitedDepth;

        // Rare special levels (flooded lake, lost floor) when a new level is dug.
        public bool specialLevelsEnabled = true;
        // New B levels as a share of the home map (100 = same size, the old behaviour).
        public int belowLevelSizePercent = 100;
        public bool askBelowLevelSize = false;
        // New A+ decks sized to the roofed area below instead of the whole map.
        public bool compactRoofDecks = false;
        // Other mods' portal maps as linked floors for relays. Default off.
        public bool foreignPortalLevelsEnabled = false;
        public KeyCode viewLevelUpKey = KeyCode.PageUp;
        public KeyCode viewLevelDownKey = KeyCode.PageDown;

        public bool WorkRelayActive => workRelayEnabled && !performanceModeEnabled
            && !StrataFaultGuard.IsTripped(StrataFaultGuard.System.WorkRelay);

        public bool RobotSoftCompatActive => robotSoftCompatEnabled && !performanceModeEnabled
            && !StrataFaultGuard.IsTripped(StrataFaultGuard.System.RobotSoftCompat);

        public bool OffThreadWorkRelayActive =>
            offThreadWorkRelay && WorkRelayActive;

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
            bool legacyThrottleVacantLevels = true;
            Scribe_Values.Look(ref legacySmokeEnabled, "smokeEnabled", defaultValue: true);
            Scribe_Values.Look(ref legacyBreathingEnabled, "breathingEnabled", defaultValue: true);
            Scribe_Values.Look(ref legacyThrottleVacantLevels, "throttleVacantLevels", defaultValue: true);
            Scribe_Values.Look(ref gasOverlayRoomLabels, "gasOverlayRoomLabels", defaultValue: false);
            Scribe_Values.Look(ref gasEventsEnabled, "gasEventsEnabled", defaultValue: false);
            Scribe_Values.Look(ref raidPursuitEnabled, "raidPursuitEnabled", defaultValue: true);
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", defaultValue: 0);
            Scribe_Values.Look(ref workRelayEnabled, "workRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref priorityWorkMaxDepth, "priorityWorkMaxDepth", defaultValue: 2);
            Scribe_Values.Look(ref priorityWorkEmergency, "priorityWorkEmergency", defaultValue: true);
            Scribe_Values.Look(ref haulAcrossLevelsEnabled, "haulAcrossLevelsEnabled", defaultValue: true);
            Scribe_Values.Look(ref robotSoftCompatEnabled, "robotSoftCompatEnabled", defaultValue: true);
            Scribe_Values.Look(ref robotWorkRelayEnabled, "robotWorkRelayEnabled", defaultValue: false);
            Scribe_Values.Look(ref shaftFluidEnabled, "shaftFluidEnabled", defaultValue: true);
            Scribe_Values.Look(ref performanceModeEnabled, "performanceModeEnabled", defaultValue: false);
            Scribe_Values.Look(ref offThreadAtmosphere, "offThreadAtmosphere", defaultValue: true);
            Scribe_Values.Look(ref offThreadWorkRelay, "offThreadWorkRelay", defaultValue: true);
            Scribe_Values.Look(ref atmosphereQuality, "atmosphereQuality", AtmosphereQualityLevel.Medium);
            Scribe_Values.Look(ref logRobotDiagnostics, "logRobotDiagnostics", defaultValue: false);
            Scribe_Values.Look(ref verboseLogging, "verboseLogging", defaultValue: false);
            Scribe_Values.Look(ref autoTagExhaust, "autoTagExhaust", defaultValue: true);
            Scribe_Values.Look(ref foodRelayEnabled, "foodRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref restRelayEnabled, "restRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref medicalRelayEnabled, "medicalRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref joyRelayEnabled, "joyRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref genericCrossFloorScan, "genericCrossFloorScan", defaultValue: false);
            Scribe_Values.Look(ref caravanPullEnabled, "caravanPullEnabled", defaultValue: true);
            Scribe_Values.Look(ref hibernateEmptyLevels, "hibernateEmptyLevels", defaultValue: true);
            Scribe_Values.Look(ref reduceBackgroundLevels, "reduceBackgroundLevels", defaultValue: true);
            Scribe_Values.Look(ref lightweightOccupiedLevels, "lightweightOccupiedLevels", defaultValue: true);
            Scribe_Values.Look(ref showLevelPerfInTab, "showLevelPerfInTab", defaultValue: false);
            Scribe_Values.Look(ref explorationSitesEnabled, "explorationSitesEnabled", defaultValue: true);
            Scribe_Values.Look(ref floodEventsEnabled, "floodEventsEnabled", defaultValue: true);
            Scribe_Values.Look(ref b1InfestationsEnabled, "b1InfestationsEnabled", defaultValue: true);
            Scribe_Values.Look(ref noiseInfestationEnabled, "noiseInfestationEnabled", defaultValue: true);
            Scribe_Values.Look(ref noiseInfestationScale, "noiseInfestationScale", 1f);
            Scribe_Values.Look(ref noiseHalfLifeHours, "noiseHalfLifeHours", 4f);
            Scribe_Values.Look(ref crossLevelRitualsEnabled, "crossLevelRitualsEnabled", defaultValue: true);
            Scribe_Values.Look(ref crossLevelCaravansEnabled, "crossLevelCaravansEnabled", defaultValue: true);
            Scribe_Values.Look(ref mergedAbandonWarning, "mergedAbandonWarning", defaultValue: true);
            Scribe_Values.Look(ref showLinkedColonistsInWorkScheduleTabs, "showLinkedColonistsInWorkScheduleTabs", defaultValue: true);
            Scribe_Values.Look(ref nativeCavernLayoutEnabled, "nativeCavernLayoutEnabled", defaultValue: true);
            Scribe_Values.Look(ref biomesCavernsCompatEnabled, "biomesCavernsCompatEnabled", defaultValue: true);
            Scribe_Values.Look(ref ancientColonyStairwellEnabled, "ancientColonyStairwellEnabled", defaultValue: true);
            Scribe_Values.Look(ref ancientColonyStairwellChance, "ancientColonyStairwellChance", 0.35f);
            Scribe_Values.Look(ref cageSustainHunger, "cageSustainHunger", defaultValue: false);
            Scribe_Values.Look(ref depthDimEnabled, "depthDimEnabled", defaultValue: false);
            Scribe_Values.Look(ref depthDimStrength, "depthDimStrength", 0.35f);
            Scribe_Values.Look(ref seeBelowEnabled, "seeBelowEnabled", defaultValue: true);
            Scribe_Values.Look(ref seeBelowLive, "seeBelowLive", defaultValue: true);
            Scribe_Values.Look(ref belowDim, "belowDim", 0.12f);
            Scribe_Values.Look(ref belowThingScale, "belowThingScale", 0.85f);
            Scribe_Values.Look(ref combinedLevelResources, "combinedLevelResources", defaultValue: false);
            Scribe_Values.Look(ref crossLevelCombatEnabled, "crossLevelCombatEnabled", defaultValue: true);
            Scribe_Values.Look(ref crossLevelAutoEngage, "crossLevelAutoEngage", defaultValue: true);
            Scribe_Values.Look(ref gravshipLifeSupportEnabled, "gravshipLifeSupportEnabled", defaultValue: true);
            Scribe_Values.Look(ref unlimitedLevelsEnabled, "unlimitedLevelsEnabled", defaultValue: false);
            Scribe_Values.Look(ref magmaLayerEnabled, "magmaLayerEnabled", defaultValue: true);
            Scribe_Values.Look(ref magmaLayerDepth, "magmaLayerDepth", defaultValue: MagmaLayer.DefaultUnlimitedDepth);
            Scribe_Values.Look(ref specialLevelsEnabled, "specialLevelsEnabled", defaultValue: true);
            Scribe_Values.Look(ref belowLevelSizePercent, "belowLevelSizePercent", 100);
            Scribe_Values.Look(ref askBelowLevelSize, "askBelowLevelSize", defaultValue: false);
            Scribe_Values.Look(ref compactRoofDecks, "compactRoofDecks", defaultValue: false);
            Scribe_Values.Look(ref foreignPortalLevelsEnabled, "foreignPortalLevelsEnabled", defaultValue: false);
            Scribe_Values.Look(ref viewLevelUpKey, "viewLevelUpKey", KeyCode.PageUp);
            Scribe_Values.Look(ref viewLevelDownKey, "viewLevelDownKey", KeyCode.PageDown);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                MigrateSettings(legacySmokeEnabled, legacyBreathingEnabled);
                StrataResources.SyncFromSettings();
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
                    StrataLog.Verbose("[Strata] Settings migration v1"
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
                StrataLog.Verbose("[Strata] Settings migration: gas systems disabled by default (can cause lag).");
            }

            if (settingsVersion < 4)
            {
                workRelayEnabled = true;
                StrataLog.Verbose("[Strata] Settings migration v4: colonist work relay enabled by default.");
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

        private static int settingsTab;

        private static string DiagnosticsTabLabel()
        {
            int tripped = StrataDiagnosticsTab.TrippedCount();
            return tripped > 0
                ? "Strata_Settings_TabDiagnosticsTripped".Translate(tripped).Resolve()
                : "Strata_Settings_TabDiagnostics".Translate().Resolve();
        }

        // Which key picker is waiting for a keypress ("up", "down", or null).
        private static string listeningFor;

        // Scroll state for the settings window (content overflows a fixed-height rect).
        private static Vector2 settingsScroll;
        // Seed tall so the first open does not multi-column-clip into an empty pane.
        private static float settingsContentHeight = 4000f;

        public StrataMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<StrataSettings>();
            StrataResources.SyncFromSettings();
            // Must hook before defs load so auto pipe-net junction comps resolve with the rest.
            VefPipeNetDiscovery.PatchDefInjection(new HarmonyLib.Harmony("azraelgodking.strata.early"));
        }

        public override string SettingsCategory()
        {
            // Never throw — a Translate failure would hide Strata from Mod Options entirely.
            try
            {
                return "Strata_SettingsCategory".Translate().Resolve();
            }
            catch
            {
                return "Strata";
            }
        }

        public override void WriteSettings()
        {
            bool wasForeignPortals = Settings.foreignPortalLevelsEnabled;
            base.WriteSettings();
            if (wasForeignPortals != Settings.foreignPortalLevelsEnabled)
            {
                LevelGraph.InvalidateCache();
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            try
            {
                DrawSettingsWindowContents(inRect);
            }
            catch (System.Exception e)
            {
                Log.Error("[Strata] Mod options UI failed: " + e);
                Widgets.Label(inRect, "Strata settings error — see Player.log.\n" + e.Message);
            }
        }

        private void DrawSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
            {
                Settings = GetSettings<StrataSettings>();
            }

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

            Rect body = new Rect(inRect.x, inRect.y + TabDrawer.TabHeight + 4f, inRect.width,
                inRect.height - TabDrawer.TabHeight - 4f);
            var tabs = new System.Collections.Generic.List<TabRecord>(OptionTabs.Length + 1);
            for (int i = 0; i < OptionTabs.Length; i++)
            {
                int index = i;
                tabs.Add(new TabRecord(OptionTabs[i].Translate(), () => SelectTab(index), settingsTab == index));
            }
            int diagnosticsTab = OptionTabs.Length;
            tabs.Add(new TabRecord(DiagnosticsTabLabel(), () => SelectTab(diagnosticsTab), settingsTab == diagnosticsTab));
            Widgets.DrawMenuSection(body);
            TabDrawer.DrawTabs(body, tabs);
            body = body.ContractedBy(6f);
            if (settingsTab == diagnosticsTab)
            {
                StrataDiagnosticsTab.Draw(body);
                return;
            }

            Rect toolbar = new Rect(body.x, body.y, body.width, 30f);
            DrawPresetRow(toolbar.LeftPart(0.62f));
            Rect searchRect = toolbar.RightPart(0.36f);
            string typed = Widgets.TextField(searchRect, searchFilter ?? "");
            if (typed != searchFilter)
            {
                searchFilter = typed;
                settingsScroll = Vector2.zero;
            }
            if (searchFilter.NullOrEmpty())
            {
                GUI.color = Color.gray;
                Widgets.Label(searchRect.ContractedBy(4f, 2f), "Strata_Settings_Search".Translate());
                GUI.color = Color.white;
            }
            inRect = new Rect(body.x, toolbar.yMax + 6f, body.width, body.height - toolbar.height - 6f);

            // Listing defaults to multi-column when content exceeds listingRect.height.
            // A short scroll viewRect then parks every option after the first row in a
            // clipped second column — looks like "only View level above" remains.
            var listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            var viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            // Tall listing rect so NewColumnIfNeeded never fires even if maxOneColumn
            // is somehow cleared; scroll view still clips to viewRect.
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));
            try
            {
                shownRows = 0;
                if (Searching)
                {
                    for (int i = 0; i < OptionTabs.Length; i++)
                    {
                        DrawTab(listing, i);
                    }
                    if (shownRows == 0)
                    {
                        listing.Label("Strata_Settings_SearchNoResults".Translate());
                    }
                }
                else
                {
                    DrawTab(listing, settingsTab);
                }
            }
            finally
            {
                settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
                listing.End();
                Widgets.EndScrollView();
            }
        }

        private static readonly string[] OptionTabs =
        {
            "Strata_Settings_TabLevels",
            "Strata_Settings_TabCommuting",
            "Strata_Settings_TabAtmosphere",
            "Strata_Settings_TabThreats",
            "Strata_Settings_TabPerformance",
            "Strata_Settings_TabVisuals",
            "Strata_Settings_TabAdvanced",
        };

        private static string searchFilter = "";
        private static int shownRows;

        private static bool Searching => !searchFilter.NullOrEmpty();

        private static void SelectTab(int index)
        {
            settingsTab = index;
            settingsScroll = Vector2.zero;
        }

        private void DrawTab(Listing_Standard listing, int tab)
        {
            switch (tab)
            {
                case 0:
                    DrawLevelsTab(listing);
                    break;
                case 1:
                    DrawCommutingTab(listing);
                    break;
                case 2:
                    DrawAtmosphereTab(listing);
                    break;
                case 3:
                    DrawThreatsTab(listing);
                    break;
                case 4:
                    DrawPerformanceTab(listing);
                    break;
                case 5:
                    DrawVisualsTab(listing);
                    break;
                case 6:
                    DrawAdvancedTab(listing);
                    break;
            }
        }

        // True when the row should draw: always outside a search, otherwise
        // when its label or tooltip contains the search text.
        private static bool Matches(string label, string desc = null)
        {
            if (!Searching)
            {
                return true;
            }
            bool hit = (label != null && label.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (desc != null && desc.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (hit)
            {
                shownRows++;
            }
            return hit;
        }

        private static void Header(Listing_Standard listing, string key)
        {
            if (Searching)
            {
                return;
            }
            Text.Font = GameFont.Medium;
            listing.Label(key.Translate());
            Text.Font = GameFont.Small;
        }

        private static void Gap(Listing_Standard listing)
        {
            if (!Searching)
            {
                listing.Gap();
            }
        }

        private static void Check(Listing_Standard listing, string key, ref bool value, string descKey = null, params NamedArgument[] descArgs)
        {
            string label = key.Translate();
            string desc = descKey == null ? null : (string)descKey.Translate(descArgs);
            if (Matches(label, desc))
            {
                listing.CheckboxLabeled(label, ref value, desc);
            }
        }

        private static float Slider(Listing_Standard listing, string label, float value, float min, float max)
        {
            if (!Matches(label))
            {
                return value;
            }
            listing.Label(label);
            return listing.Slider(value, min, max);
        }

        private void DrawPresetRow(Rect rect)
        {
            StrataSettingsPreset current = StrataSettingsPresets.Current(Settings);
            Widgets.Label(rect.LeftPart(0.4f), "Strata_Settings_Preset".Translate(StrataSettingsPresets.Label(current)));
            TooltipHandler.TipRegion(rect.LeftPart(0.4f), "Strata_Settings_PresetDesc".Translate());
            float x = rect.x + rect.width * 0.4f;
            float w = rect.width * 0.6f / 3f;
            StrataSettingsPreset[] choices =
            {
                StrataSettingsPreset.Performance,
                StrataSettingsPreset.Balanced,
                StrataSettingsPreset.Immersive,
            };
            for (int i = 0; i < choices.Length; i++)
            {
                Rect button = new Rect(x + i * w, rect.y, w - 4f, 28f);
                StrataSettingsPreset preset = choices[i];
                TooltipHandler.TipRegion(button, StrataSettingsPresets.Description(preset));
                if (Widgets.ButtonText(button, StrataSettingsPresets.Label(preset), active: current != preset))
                {
                    StrataSettingsPresets.Apply(Settings, preset);
                }
            }
        }

        private void DrawLevelsTab(Listing_Standard listing)
        {
            Header(listing, "Strata_Settings_LevelHotkeys");
            KeyPickerRow(listing, "Strata_Settings_ViewLevelAbove".Translate(), ref Settings.viewLevelUpKey, "up", KeyCode.PageUp);
            KeyPickerRow(listing, "Strata_Settings_ViewLevelBelow".Translate(), ref Settings.viewLevelDownKey, "down", KeyCode.PageDown);
            Gap(listing);

            Header(listing, "Strata_Settings_NewLevels");
            Check(listing, "Strata_Settings_UnlimitedLevels", ref Settings.unlimitedLevelsEnabled,
                "Strata_Settings_UnlimitedLevelsDesc", StrataLevelCap.HardMaxOffset);
            Check(listing, "Strata_Settings_MagmaLayer", ref Settings.magmaLayerEnabled,
                "Strata_Settings_MagmaLayerDesc", StrataLevelCap.HardMaxOffset + 1);
            if (Settings.magmaLayerEnabled && Settings.unlimitedLevelsEnabled)
            {
                Settings.magmaLayerDepth = Mathf.RoundToInt(Slider(listing,
                    "Strata_Settings_MagmaLayerDepth".Translate(Settings.magmaLayerDepth),
                    Settings.magmaLayerDepth, StrataLevelCap.HardMaxOffset + 1, MagmaLayer.MaxSettingDepth));
            }
            Check(listing, "Strata_Settings_SpecialLevels", ref Settings.specialLevelsEnabled,
                "Strata_Settings_SpecialLevelsDesc", FloodedLevel.Chance.ToStringPercent(), FloodedLevel.MinDepth,
                LostFloor.Chance.ToStringPercent());
            DrawLevelSizeSelector(listing);
            Check(listing, "Strata_Settings_AskBelowLevelSize", ref Settings.askBelowLevelSize, "Strata_Settings_AskBelowLevelSizeDesc");
            Check(listing, "Strata_Settings_CompactRoofDecks", ref Settings.compactRoofDecks,
                "Strata_Settings_CompactRoofDecksDesc", StrataLevelCoords.DeckMargin);
            Check(listing, "Strata_Settings_NativeCavern", ref Settings.nativeCavernLayoutEnabled, "Strata_Settings_NativeCavernDesc");
            Check(listing, "Strata_Settings_AncientStairwell", ref Settings.ancientColonyStairwellEnabled, "Strata_Settings_AncientStairwellDesc");
            if (Settings.ancientColonyStairwellEnabled)
            {
                Settings.ancientColonyStairwellChance = Slider(listing,
                    "Strata_Settings_AncientStairwellChance".Translate(Settings.ancientColonyStairwellChance.ToStringPercent()),
                    Settings.ancientColonyStairwellChance, 0.05f, 1f);
            }
            if (BiomesCavernsUtility.IsActive)
            {
                Check(listing, "Strata_Settings_BiomesCaverns", ref Settings.biomesCavernsCompatEnabled, "Strata_Settings_BiomesCavernsDesc");
            }
            Check(listing, "Strata_Settings_ExplorationSites", ref Settings.explorationSitesEnabled, "Strata_Settings_ExplorationSitesDesc");
        }

        private void DrawCommutingTab(Listing_Standard listing)
        {
            Header(listing, "Strata_Settings_ColonistRelays");
            Check(listing, "Strata_Settings_WorkRelay", ref Settings.workRelayEnabled, "Strata_Settings_WorkRelayDesc");
            Check(listing, "Strata_Settings_PriorityWorkEmergency", ref Settings.priorityWorkEmergency, "Strata_Settings_PriorityWorkEmergencyDesc");
            Settings.priorityWorkMaxDepth = Mathf.RoundToInt(Slider(listing,
                "Strata_Settings_PriorityWorkDepth".Translate(Settings.priorityWorkMaxDepth),
                Settings.priorityWorkMaxDepth, 0f, 8f));
            Check(listing, "Strata_Settings_OffThreadWorkRelay", ref Settings.offThreadWorkRelay, "Strata_Settings_OffThreadWorkRelayDesc");
            Check(listing, "Strata_Settings_HaulAcrossLevels", ref Settings.haulAcrossLevelsEnabled, "Strata_Settings_HaulAcrossLevelsDesc");
            Check(listing, "Strata_Settings_RobotSoftCompat", ref Settings.robotSoftCompatEnabled, "Strata_Settings_RobotSoftCompatDesc");
            Check(listing, "Strata_Settings_RobotWorkRelay", ref Settings.robotWorkRelayEnabled, "Strata_Settings_RobotWorkRelayDesc");
            Check(listing, "Strata_Settings_ShaftFluid", ref Settings.shaftFluidEnabled, "Strata_Settings_ShaftFluidDesc");
            Check(listing, "Strata_Settings_FoodRelay", ref Settings.foodRelayEnabled, "Strata_Settings_FoodRelayDesc");
            Check(listing, "Strata_Settings_RestRelay", ref Settings.restRelayEnabled, "Strata_Settings_RestRelayDesc");
            Check(listing, "Strata_Settings_MedicalRelay", ref Settings.medicalRelayEnabled, "Strata_Settings_MedicalRelayDesc");
            Check(listing, "Strata_Settings_JoyRelay", ref Settings.joyRelayEnabled, "Strata_Settings_JoyRelayDesc");
            Check(listing, "Strata_Settings_GenericScan", ref Settings.genericCrossFloorScan, "Strata_Settings_GenericScanDesc");
            Check(listing, "Strata_Settings_CaravanPull", ref Settings.caravanPullEnabled, "Strata_Settings_CaravanPullDesc");
            Check(listing, "Strata_Settings_CrossLevelRituals", ref Settings.crossLevelRitualsEnabled, "Strata_Settings_CrossLevelRitualsDesc");
            Check(listing, "Strata_Settings_CrossLevelCaravans", ref Settings.crossLevelCaravansEnabled, "Strata_Settings_CrossLevelCaravansDesc");
            Check(listing, "Strata_Settings_CageSustainHunger", ref Settings.cageSustainHunger, "Strata_Settings_CageSustainHungerDesc");
            Check(listing, "Strata_Settings_ForeignPortals", ref Settings.foreignPortalLevelsEnabled, "Strata_Settings_ForeignPortalsDesc");
        }

        private void DrawAtmosphereTab(Listing_Standard listing)
        {
            Header(listing, "Strata_Settings_SmokeGas");
            if (!Searching)
            {
                GUI.color = Color.yellow;
                listing.Label("Strata_Settings_GasSystemWarning".Translate());
                GUI.color = Color.white;
            }
            Check(listing, "Strata_Settings_PollutantGases", ref Settings.pollutantGasesEnabled, "Strata_Settings_PollutantGasesDesc");
            Check(listing, "Strata_Settings_AutoTagExhaust", ref Settings.autoTagExhaust, "Strata_Settings_AutoTagExhaustDesc");
            if (Settings.pollutantGasesEnabled)
            {
                Settings.smokeSeverityScale = Slider(listing,
                    "Strata_Settings_SmokeSeverity".Translate(Settings.smokeSeverityScale.ToStringPercent()),
                    Settings.smokeSeverityScale, 0f, 2f);
            }
            Check(listing, "Strata_Settings_UndergroundGas", ref Settings.gasEventsEnabled, "Strata_Settings_UndergroundGasDesc");
            Gap(listing);

            Header(listing, "Strata_Settings_DeepBreathing");
            Check(listing, "Strata_Settings_NaturalGases", ref Settings.naturalGasesEnabled, "Strata_Settings_NaturalGasesDesc");
            Check(listing, "Strata_Settings_GravshipLifeSupport", ref Settings.gravshipLifeSupportEnabled, "Strata_Settings_GravshipLifeSupportDesc");
            Check(listing, "Strata_Settings_GasOverlayLabels", ref Settings.gasOverlayRoomLabels, "Strata_Settings_GasOverlayLabelsDesc");
        }

        private void DrawThreatsTab(Listing_Standard listing)
        {
            Check(listing, "Strata_Settings_RaidPursuit", ref Settings.raidPursuitEnabled, "Strata_Settings_RaidPursuitDesc");
            Check(listing, "Strata_Settings_B1Infestations", ref Settings.b1InfestationsEnabled, "Strata_Settings_B1InfestationsDesc");
            Check(listing, "Strata_Settings_NoiseInfestation", ref Settings.noiseInfestationEnabled, "Strata_Settings_NoiseInfestationDesc");
            if (Settings.noiseInfestationEnabled)
            {
                Settings.noiseInfestationScale = Slider(listing,
                    "Strata_Settings_NoiseInfestationScale".Translate(Settings.noiseInfestationScale.ToStringPercent()),
                    Settings.noiseInfestationScale, 0f, 2f);
                Settings.noiseHalfLifeHours = Slider(listing,
                    "Strata_Settings_NoiseHalfLife".Translate(Settings.noiseHalfLifeHours.ToString("0.#")),
                    Settings.noiseHalfLifeHours, 1f, 12f);
            }
            Check(listing, "Strata_Settings_FloodEvents", ref Settings.floodEventsEnabled, "Strata_Settings_FloodEventsDesc");
            Check(listing, "Strata_Settings_CrossLevelCombat", ref Settings.crossLevelCombatEnabled, "Strata_Settings_CrossLevelCombatDesc");
            if (Settings.crossLevelCombatEnabled)
            {
                Check(listing, "Strata_Settings_CrossLevelAutoEngage", ref Settings.crossLevelAutoEngage, "Strata_Settings_CrossLevelAutoEngageDesc");
            }
        }

        private void DrawPerformanceTab(Listing_Standard listing)
        {
            Check(listing, "Strata_Settings_HibernateEmpty", ref Settings.hibernateEmptyLevels, "Strata_Settings_HibernateEmptyDesc");
            Check(listing, "Strata_Settings_ReduceBackground", ref Settings.reduceBackgroundLevels, "Strata_Settings_ReduceBackgroundDesc");
            Check(listing, "Strata_Settings_LightweightLevels", ref Settings.lightweightOccupiedLevels, "Strata_Settings_LightweightLevelsDesc");
            Check(listing, "Strata_Settings_PerformanceMode", ref Settings.performanceModeEnabled, "Strata_Settings_PerformanceModeDesc");
            if (Settings.performanceModeEnabled && !Searching)
            {
                GUI.color = Color.yellow;
                listing.Label("Strata_Settings_PerformanceModeActive".Translate());
                GUI.color = Color.white;
            }
            Check(listing, "Strata_Settings_OffThreadAtmosphere", ref Settings.offThreadAtmosphere, "Strata_Settings_OffThreadAtmosphereDesc");
            if (Matches("Strata_Settings_AtmosphereQuality".Translate(), "Strata_Settings_AtmosphereQualityDesc".Translate()))
            {
                listing.Label("Strata_Settings_AtmosphereQuality".Translate());
                DrawAtmosphereQualitySelector(listing);
            }
            Check(listing, "Strata_Settings_ShowLevelPerf", ref Settings.showLevelPerfInTab, "Strata_Settings_ShowLevelPerfDesc");
        }

        private void DrawVisualsTab(Listing_Standard listing)
        {
            Header(listing, "Strata_Settings_Appearance");
            Check(listing, "Strata_Settings_SeeBelow", ref Settings.seeBelowEnabled, "Strata_Settings_SeeBelowDesc");
            StrataBelowDrawPosPatcher.UnpatchIfDisabled();
            if (Settings.seeBelowEnabled)
            {
                Check(listing, "Strata_Settings_SeeBelowLive", ref Settings.seeBelowLive, "Strata_Settings_SeeBelowLiveDesc");
                Settings.belowDim = Slider(listing,
                    "Strata_Settings_BelowDim".Translate(Settings.belowDim.ToStringPercent()),
                    Settings.belowDim, 0f, 0.6f);
                Settings.belowThingScale = Slider(listing,
                    "Strata_Settings_BelowThingScale".Translate(Settings.belowThingScale.ToStringPercent()),
                    Settings.belowThingScale, 0.5f, 1f);
            }
            Check(listing, "Strata_Settings_DepthDim", ref Settings.depthDimEnabled, "Strata_Settings_DepthDimDesc");
            if (Settings.depthDimEnabled)
            {
                Settings.depthDimStrength = Slider(listing,
                    "Strata_Settings_DepthDimStrength".Translate(Settings.depthDimStrength.ToStringPercent()),
                    Settings.depthDimStrength, 0f, 1f);
            }
            Gap(listing);

            Header(listing, "Strata_Settings_Interface");
            Check(listing, "Strata_Settings_LinkedColonistsTabs", ref Settings.showLinkedColonistsInWorkScheduleTabs, "Strata_Settings_LinkedColonistsTabsDesc");
            Check(listing, "Strata_Settings_MergedAbandonWarning", ref Settings.mergedAbandonWarning, "Strata_Settings_MergedAbandonWarningDesc");
        }

        private void DrawAdvancedTab(Listing_Standard listing)
        {
            Check(listing, "Strata_Settings_VerboseLogging", ref Settings.verboseLogging, "Strata_Settings_VerboseLoggingDesc");
            Gap(listing);
            Header(listing, "Strata_Settings_RobotDiagnostics");
            Check(listing, "Strata_Settings_LogRobotDiagnostics", ref Settings.logRobotDiagnostics, "Strata_Settings_LogRobotDiagnosticsDesc");
            if (!Searching)
            {
                DrawRobotDiagnostics(listing);
            }
        }

        private static void KeyPickerRow(Listing_Standard listing, string label, ref KeyCode key, string id, KeyCode defaultKey)
        {
            if (!Matches(label))
            {
                return;
            }
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

        public static readonly int[] LevelSizeChoices = { 100, 75, 50 };

        private static void DrawLevelSizeSelector(Listing_Standard listing)
        {
            if (!Matches("Strata_Settings_BelowLevelSize".Translate(), "Strata_Settings_BelowLevelSizeDesc".Translate()))
            {
                return;
            }
            Rect rect = listing.GetRect(30f);
            Widgets.Label(rect.LeftPart(0.6f), "Strata_Settings_BelowLevelSize".Translate());
            TooltipHandler.TipRegion(rect, "Strata_Settings_BelowLevelSizeDesc".Translate());
            Rect button = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, 28f);
            if (Widgets.ButtonText(button, Settings.belowLevelSizePercent + "%"))
            {
                var options = new System.Collections.Generic.List<FloatMenuOption>();
                foreach (int pct in LevelSizeChoices)
                {
                    int chosen = pct;
                    options.Add(new FloatMenuOption(chosen + "%", () => Settings.belowLevelSizePercent = chosen));
                }
                Find.WindowStack.Add(new FloatMenu(options));
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
