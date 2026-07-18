using UnityEngine;
using Verse;

namespace Homesteader
{
    public class HomesteaderSettings : ModSettings
    {
        /// <summary>When true, Tastes tab shows allergy names before discovery. Intended for DevMode testing.</summary>
        public bool revealAllergies = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref revealAllergies, "revealAllergies", defaultValue: false);
        }
    }

    public class HomesteaderMod : Mod
    {
        public static HomesteaderSettings Settings;

        public HomesteaderMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HomesteaderSettings>();
        }

        public override string SettingsCategory() => "Homesteader_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            if (Prefs.DevMode)
            {
                listing.Label("Homesteader_SettingsDevHeader".Translate());
                listing.GapLine();
                listing.CheckboxLabeled(
                    "Homesteader_SettingsRevealAllergies".Translate(),
                    ref Settings.revealAllergies,
                    "Homesteader_SettingsRevealAllergiesTip".Translate());
            }
            else
            {
                listing.Label("Homesteader_SettingsDevModeHint".Translate());
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
