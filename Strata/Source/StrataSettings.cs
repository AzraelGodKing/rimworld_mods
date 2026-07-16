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
        public bool gasOverlayRoomLabels = false;
        public bool gasEventsEnabled = true;
        public bool raidPursuitEnabled = true;
        public bool workRelayEnabled = true;
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref smokeEnabled, "smokeEnabled", defaultValue: true);
            Scribe_Values.Look(ref smokeSeverityScale, "smokeSeverityScale", 1f);
            Scribe_Values.Look(ref breathingEnabled, "breathingEnabled", defaultValue: true);
            Scribe_Values.Look(ref gasOverlayRoomLabels, "gasOverlayRoomLabels", defaultValue: false);
            Scribe_Values.Look(ref gasEventsEnabled, "gasEventsEnabled", defaultValue: true);
            Scribe_Values.Look(ref raidPursuitEnabled, "raidPursuitEnabled", defaultValue: true);
            Scribe_Values.Look(ref workRelayEnabled, "workRelayEnabled", defaultValue: true);
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
                "Sleepy colonists on a floor without beds walk to their assigned bed "
                + "or a free bed on another linked level (Barracks role preferred).");
            listing.CheckboxLabeled("Medical relay", ref Settings.medicalRelayEnabled,
                "Patients and doctors commute to linked levels with medical beds or tending work.");
            listing.CheckboxLabeled("Joy relay", ref Settings.joyRelayEnabled,
                "Recreation-starved colonists walk to another level with food joy or recreation buildings.");
            listing.CheckboxLabeled("Caravan pull from below", ref Settings.caravanPullEnabled,
                "While forming a caravan on the surface, colonists haul high-value goods up from linked underground levels.");
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
            listing.CheckboxLabeled("Gas overlay room labels", ref Settings.gasOverlayRoomLabels,
                "While the play-settings gas overlay is on, draw color-coded percentage labels on each "
                + "enclosed room (top three gases + load). The cursor panel is always available when the overlay is on.");
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Threats & performance");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Raid pursuit", ref Settings.raidPursuitEnabled,
                "Raiders with nobody left to fight follow your colonists through unsealed stairwells.");
            listing.CheckboxLabeled("Hibernate empty levels", ref Settings.hibernateEmptyLevels,
                "Vacant A+ and B+ pocket maps run vanilla ambient sims 1 tick in 4 and Strata gas 1 cycle in 8. "
                + "Fully live on the map you are viewing or any level with pawns on it.");
            Settings.throttleVacantLevels = Settings.hibernateEmptyLevels;
            listing.CheckboxLabeled("Reduce background levels", ref Settings.reduceBackgroundLevels,
                "When colonists are on a linked level you are not viewing, Strata gas and O₂/CO₂ run 4× slower "
                + "and skip overlay rebuilds. The map you are looking at stays full speed.");
            listing.CheckboxLabeled("Show level performance in Levels tab", ref Settings.showLevelPerfInTab,
                "List pawn counts and hibernation status for each level in the Levels tab.");
            listing.CheckboxLabeled("Exploration quest sites", ref Settings.explorationSitesEnabled,
                "World incidents that reveal sunken ruins, collapsed mines, sealed vaults, and geothermal vents.");
            listing.CheckboxLabeled("Underground flood events", ref Settings.floodEventsEnabled,
                "Groundwater seeps that flood patches of excavated levels.");
            listing.CheckboxLabeled("Natural cave layout", ref Settings.nativeCavernLayoutEnabled,
                "Excavated levels (B1 and deeper) generate Strata cave networks — chambers and tunnels "
                + "carved from the solid rock instead of leaving the whole level unmined.");
            listing.CheckboxLabeled("Ancient colony stairwell", ref Settings.ancientColonyStairwellEnabled,
                "Some new colony maps spawn a pre-built ancient stairwell on the surface. "
                + "It opens the first underground level without digging-down research, but does not carry power between levels.");
            if (Settings.ancientColonyStairwellEnabled)
            {
                listing.Label("Ancient stairwell spawn chance: " + Settings.ancientColonyStairwellChance.ToStringPercent());
                Settings.ancientColonyStairwellChance = listing.Slider(Settings.ancientColonyStairwellChance, 0.05f, 1f);
            }
            if (BiomesCavernsUtility.IsActive)
            {
                listing.CheckboxLabeled("Biomes! Caverns layout", ref Settings.biomesCavernsCompatEnabled,
                    "When enabled and Biomes! Caverns is loaded, use Biomes! cavern generation instead of "
                    + "Strata's native cave layout (plants, fauna, stalagmites, and crystals).");
            }
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Appearance");
            Text.Font = GameFont.Small;
            bool previousMultiFloorStairs = Settings.multiFloorStairs;
            listing.CheckboxLabeled("Multifloor Stairs", ref Settings.multiFloorStairs,
                "Use bundled alternate art from Textures/Strata/Buildings/MultiFloors/ (handrail stairs + modern elevator). Does not require the MultiFloors mod.");
            if (Settings.multiFloorStairs)
            {
                GUI.color = Color.yellow;
                listing.Label("These Assets are from Multifloors, Telardo can ask for it to be removed.");
                GUI.color = Color.white;
            }
            if (previousMultiFloorStairs != Settings.multiFloorStairs)
            {
                StrataMultiFloorStairsUtility.Apply(Settings.multiFloorStairs);
            }
            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Interface");
            Text.Font = GameFont.Small;
            listing.CheckboxLabeled("Strata Levels on Work / Schedule", ref Settings.showLinkedColonistsInWorkScheduleTabs,
                "Work and Schedule tabs show a Strata Levels checkbox (when your colony has excavated levels). "
                + "When checked, both tabs list colonists from every linked level, not only the map you are viewing.");
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
