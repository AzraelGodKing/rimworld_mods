using HarmonyLib;
using UnityEngine;
using Verse;

namespace DateNight
{
    public class DateNightMod : Mod
    {
        public static DateNightSettings Settings;

        public DateNightMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DateNightSettings>();
            // PatchAll runs after defs load — Harmony compiling TimeAssignmentSelector
            // patches otherwise touches TimeAssignmentDefOf before DefOfs exist.
        }

        public override string SettingsCategory() => "DateNight_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("DateNight_Settings_Intro".Translate());
            listing.Gap(8f);

            listing.CheckboxLabeled(
                "DateNight_Settings_PregnancySafe".Translate(),
                ref Settings.pregnancySafeCooldown,
                "DateNight_Settings_PregnancySafeTip".Translate());
            if (Settings.pregnancySafeCooldown)
            {
                Settings.eagerCooldown = false;
            }
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "DateNight_Settings_Eager".Translate(),
                ref Settings.eagerCooldown,
                "DateNight_Settings_EagerTip".Translate());
            if (Settings.eagerCooldown)
            {
                Settings.pregnancySafeCooldown = false;
            }
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "DateNight_Settings_SelfLovin".Translate(),
                ref Settings.allowSelfLovin,
                "DateNight_Settings_SelfLovinTip".Translate());
            listing.Gap(10f);

            if (listing.ButtonText("DateNight_Settings_Reset".Translate(), null, 0.25f))
            {
                Settings.ResetToDefaults();
            }

            listing.End();
        }
    }

    [StaticConstructorOnStartup]
    public static class DateNightInit
    {
        static DateNightInit()
        {
            new Harmony("azraelgodking.datenight").PatchAll();
        }
    }
}
