using UnityEngine;
using Verse;

namespace Strata
{
    // Mod options. Read as statics from hot paths, so everything is a plain
    // field with a cheap default.
    public class StrataSettings : ModSettings
    {
        public bool smokeEnabled = true;
        public float smokeSeverityScale = 1f;
        public bool raidPursuitEnabled = true;
        public bool workRelayEnabled = true;
        public bool foodRelayEnabled = true;
        public bool restRelayEnabled = true;
        public bool throttleVacantLevels = true;
        public KeyCode viewLevelUpKey = KeyCode.PageUp;
        public KeyCode viewLevelDownKey = KeyCode.PageDown;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref smokeEnabled, "smokeEnabled", defaultValue: true);
            Scribe_Values.Look(ref smokeSeverityScale, "smokeSeverityScale", 1f);
            Scribe_Values.Look(ref raidPursuitEnabled, "raidPursuitEnabled", defaultValue: true);
            Scribe_Values.Look(ref workRelayEnabled, "workRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref foodRelayEnabled, "foodRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref restRelayEnabled, "restRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref throttleVacantLevels, "throttleVacantLevels", defaultValue: true);
            Scribe_Values.Look(ref viewLevelUpKey, "viewLevelUpKey", KeyCode.PageUp);
            Scribe_Values.Look(ref viewLevelDownKey, "viewLevelDownKey", KeyCode.PageDown);
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

        public override string SettingsCategory() => "Strata";

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
            listing.Label("Level view hotkeys");
            Text.Font = GameFont.Small;
            KeyPickerRow(listing, "View level above", ref Settings.viewLevelUpKey, "up", KeyCode.PageUp);
            KeyPickerRow(listing, "View level below", ref Settings.viewLevelDownKey, "down", KeyCode.PageDown);
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Colonist relays");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Work relay", ref Settings.workRelayEnabled,
                "Idle colonists commute to other levels that have work.");
            listing.CheckboxLabeled("Food relay", ref Settings.foodRelayEnabled,
                "Hungry colonists go find a meal on another level.");
            listing.CheckboxLabeled("Rest relay", ref Settings.restRelayEnabled,
                "Sleepy colonists walk home to their bed on another level.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Combustion smoke");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Smoke simulation", ref Settings.smokeEnabled,
                "Burners give off smoke that pools in unventilated rooms. Turning this off clears all existing smoke.");
            if (Settings.smokeEnabled)
            {
                listing.Label("Smoke inhalation severity: " + Settings.smokeSeverityScale.ToStringPercent()
                    + " (how fast pawns are harmed by thick smoke)");
                Settings.smokeSeverityScale = listing.Slider(Settings.smokeSeverityScale, 0f, 2f);
            }
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Threats & performance");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Raid pursuit", ref Settings.raidPursuitEnabled,
                "Raiders with nobody left to fight follow your colonists through unsealed stairwells.");
            listing.CheckboxLabeled("Throttle vacant levels", ref Settings.throttleVacantLevels,
                "Levels with nobody on them run their ambient simulation at reduced rate to save performance.");

            listing.End();
        }

        private static void KeyPickerRow(Listing_Standard listing, string label, ref KeyCode key, string id, KeyCode defaultKey)
        {
            Rect rect = listing.GetRect(30f);
            Widgets.Label(rect.LeftPart(0.5f), label);
            Rect button = new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.3f, 28f);
            string text = listeningFor == id ? "Press a key..." : key.ToString();
            if (Widgets.ButtonText(button, text))
            {
                listeningFor = listeningFor == id ? null : id;
            }
            Rect reset = new Rect(button.xMax + 6f, rect.y, rect.width * 0.2f - 6f, 28f);
            if (key != defaultKey && Widgets.ButtonText(reset, "Reset"))
            {
                key = defaultKey;
                listeningFor = null;
            }
            listing.Gap(2f);
        }
    }
}
