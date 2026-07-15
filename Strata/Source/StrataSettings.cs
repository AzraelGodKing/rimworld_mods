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
        public bool breathingEnabled = true;
        public bool gasEventsEnabled = true;
        public bool raidPursuitEnabled = true;
        public bool workRelayEnabled = true;
        public bool foodRelayEnabled = true;
        public bool restRelayEnabled = true;
        public bool throttleVacantLevels = true;
        public bool crossLevelRitualsEnabled = true;
        public bool mergedAbandonWarning = true;
        public bool cageSustainHunger = false;
        public KeyCode viewLevelUpKey = KeyCode.PageUp;
        public KeyCode viewLevelDownKey = KeyCode.PageDown;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref smokeEnabled, "smokeEnabled", defaultValue: true);
            Scribe_Values.Look(ref smokeSeverityScale, "smokeSeverityScale", 1f);
            Scribe_Values.Look(ref breathingEnabled, "breathingEnabled", defaultValue: true);
            Scribe_Values.Look(ref gasEventsEnabled, "gasEventsEnabled", defaultValue: true);
            Scribe_Values.Look(ref raidPursuitEnabled, "raidPursuitEnabled", defaultValue: true);
            Scribe_Values.Look(ref workRelayEnabled, "workRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref foodRelayEnabled, "foodRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref restRelayEnabled, "restRelayEnabled", defaultValue: true);
            Scribe_Values.Look(ref throttleVacantLevels, "throttleVacantLevels", defaultValue: true);
            Scribe_Values.Look(ref crossLevelRitualsEnabled, "crossLevelRitualsEnabled", defaultValue: true);
            Scribe_Values.Look(ref mergedAbandonWarning, "mergedAbandonWarning", defaultValue: true);
            Scribe_Values.Look(ref cageSustainHunger, "cageSustainHunger", defaultValue: false);
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
            listing.CheckboxLabeled("Cross-level rituals", ref Settings.crossLevelRitualsEnabled,
                "The ritual menu lists colonists from every linked level; those elsewhere walk to the ritual and join when they arrive.");
            listing.CheckboxLabeled("Sustain caged bird hunger", ref Settings.cageSustainHunger,
                "When off (default), canary and bird cages feed occupants from stocked hay or kibble. "
                + "When on, hunger is frozen while a bird is caged — no food storage needed.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Smoke & gas");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Smoke simulation", ref Settings.smokeEnabled,
                "Burners give off smoke that pools in unventilated rooms. Turning this off clears all existing smoke.");
            if (Settings.smokeEnabled)
            {
                listing.Label("Smoke inhalation severity: " + Settings.smokeSeverityScale.ToStringPercent()
                    + " (how fast pawns are harmed by thick smoke)");
                Settings.smokeSeverityScale = listing.Slider(Settings.smokeSeverityScale, 0f, 2f);
            }
            listing.CheckboxLabeled("Underground gas", ref Settings.gasEventsEnabled,
                "Gas hazards of the deep: excavation can breach pockets of foul deep gas, and sunken ruin sites can appear. "
                + "Sealed stairwells always contain gas either way.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Deep-level breathing");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("O₂ / CO₂ simulation", ref Settings.breathingEnabled,
                "Underground levels track oxygen and carbon dioxide: colonists breathe O₂, exhale CO₂, "
                + "O₂ rises through shafts, and CO₂ sinks. Turning this off clears both gases.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Threats & performance");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Raid pursuit", ref Settings.raidPursuitEnabled,
                "Raiders with nobody left to fight follow your colonists through unsealed stairwells.");
            listing.CheckboxLabeled("Throttle vacant levels", ref Settings.throttleVacantLevels,
                "Levels with nobody on them run their ambient simulation at reduced rate to save performance.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Interface");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Combined abandon warning", ref Settings.mergedAbandonWarning,
                "When abandoning a settlement with pawns still on levels below, show one combined warning listing everyone left behind (surface and underground). Turn off for two separate prompts.");

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
