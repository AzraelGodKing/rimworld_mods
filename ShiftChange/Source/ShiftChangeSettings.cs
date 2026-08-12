using UnityEngine;
using Verse;

namespace ShiftChange
{
    public class ShiftChangeSettings : ModSettings
    {
        public bool enabled = true;
        public bool workTriggersEnabled = true;
        public bool ritualTriggersEnabled = true;

        /// <summary>Minimum ticks between apply/restore jobs for the same pawn.</summary>
        public int swapCooldownTicks = 250;

        /// <summary>
        /// After a work/ritual trigger clears, wait this long before restoring
        /// so brief job gaps do not thrash outfits.
        /// </summary>
        public int hysteresisTicks = 400;

        /// <summary>
        /// When no zone is set on a rule, prefer a stockpile whose label contains this
        /// (case-insensitive). Empty = any stockpile with apparel.
        /// </summary>
        public string defaultWardrobeLabel = "Wardrobe";

        /// <summary>When replacing layers, stash removed apparel in inventory if it fits.</summary>
        public bool preferInventoryForRemoved = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref workTriggersEnabled, "workTriggersEnabled", true);
            Scribe_Values.Look(ref ritualTriggersEnabled, "ritualTriggersEnabled", true);
            Scribe_Values.Look(ref swapCooldownTicks, "swapCooldownTicks", 250);
            Scribe_Values.Look(ref hysteresisTicks, "hysteresisTicks", 400);
            Scribe_Values.Look(ref defaultWardrobeLabel, "defaultWardrobeLabel", "Wardrobe");
            Scribe_Values.Look(ref preferInventoryForRemoved, "preferInventoryForRemoved", true);
        }

        public void ResetToDefaults()
        {
            enabled = true;
            workTriggersEnabled = true;
            ritualTriggersEnabled = true;
            swapCooldownTicks = 250;
            hysteresisTicks = 400;
            defaultWardrobeLabel = "Wardrobe";
            preferInventoryForRemoved = true;
        }

        public static void DoSettingsWindow(Rect inRect)
        {
            ShiftChangeSettings s = ShiftChangeMod.Settings;
            if (s == null)
            {
                return;
            }

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("ShiftChange_Settings_Intro".Translate());
            listing.Gap(8f);

            listing.CheckboxLabeled(
                "ShiftChange_Settings_Enabled".Translate(),
                ref s.enabled,
                "ShiftChange_Settings_EnabledTip".Translate());
            listing.Gap(4f);
            listing.CheckboxLabeled(
                "ShiftChange_Settings_WorkTriggers".Translate(),
                ref s.workTriggersEnabled,
                "ShiftChange_Settings_WorkTriggersTip".Translate());
            listing.Gap(4f);
            listing.CheckboxLabeled(
                "ShiftChange_Settings_RitualTriggers".Translate(),
                ref s.ritualTriggersEnabled,
                "ShiftChange_Settings_RitualTriggersTip".Translate());
            listing.Gap(6f);

            listing.Label("ShiftChange_Settings_Cooldown".Translate(s.swapCooldownTicks));
            s.swapCooldownTicks = (int)listing.Slider(s.swapCooldownTicks, 0, 3600);
            listing.Gap(4f);
            listing.Label("ShiftChange_Settings_Hysteresis".Translate(s.hysteresisTicks));
            s.hysteresisTicks = (int)listing.Slider(s.hysteresisTicks, 0, 3600);
            listing.Gap(6f);

            listing.Label("ShiftChange_Settings_WardrobeLabel".Translate());
            s.defaultWardrobeLabel = listing.TextEntry(s.defaultWardrobeLabel ?? string.Empty);
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "ShiftChange_Settings_PreferInventory".Translate(),
                ref s.preferInventoryForRemoved,
                "ShiftChange_Settings_PreferInventoryTip".Translate());
            listing.Gap(10f);

            if (listing.ButtonText("ShiftChange_Settings_Reset".Translate(), null, 0.25f))
            {
                s.ResetToDefaults();
            }

            listing.End();
        }
    }
}
