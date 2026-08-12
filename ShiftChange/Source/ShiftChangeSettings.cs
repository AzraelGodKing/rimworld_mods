using UnityEngine;
using Verse;

namespace ShiftChange
{
    public class ShiftChangeSettings : ModSettings
    {
        public bool enabled = true;

        /// <summary>Minimum ticks between apply/restore jobs for the same pawn.</summary>
        public int swapCooldownTicks = 600;

        /// <summary>
        /// When no zone is set on a rule, prefer a stockpile whose label contains this
        /// (case-insensitive). Empty = any stockpile with apparel.
        /// </summary>
        public string defaultWardrobeLabel = "Wardrobe";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref swapCooldownTicks, "swapCooldownTicks", 600);
            Scribe_Values.Look(ref defaultWardrobeLabel, "defaultWardrobeLabel", "Wardrobe");
        }

        public void ResetToDefaults()
        {
            enabled = true;
            swapCooldownTicks = 600;
            defaultWardrobeLabel = "Wardrobe";
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
            listing.Gap(6f);

            listing.Label("ShiftChange_Settings_Cooldown".Translate(s.swapCooldownTicks));
            s.swapCooldownTicks = (int)listing.Slider(s.swapCooldownTicks, 0, 3600);
            listing.Gap(6f);

            listing.Label("ShiftChange_Settings_WardrobeLabel".Translate());
            s.defaultWardrobeLabel = listing.TextEntry(s.defaultWardrobeLabel ?? string.Empty);
            listing.Gap(10f);

            if (listing.ButtonText("ShiftChange_Settings_Reset".Translate(), null, 0.25f))
            {
                s.ResetToDefaults();
            }

            listing.End();
        }
    }
}
