using UnityEngine;
using Verse;

namespace Homesteader
{
    public class HomesteaderSettings : ModSettings
    {
        /// <summary>When true, Tastes tab shows allergy names before discovery. Intended for DevMode testing.</summary>
        public bool revealAllergies = false;

        /// <summary>Optional unique art pack. Off by default; original sprites are never deleted.</summary>
        public bool useRefreshedTextures = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref revealAllergies, "revealAllergies", defaultValue: false);
            Scribe_Values.Look(ref useRefreshedTextures, "useRefreshedTextures", defaultValue: false);
        }
    }

    public class HomesteaderMod : Mod
    {
        public static HomesteaderSettings Settings;
        public static ModContentPack ContentPack;

        public HomesteaderMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            Settings = GetSettings<HomesteaderSettings>();
        }

        public override string SettingsCategory() => "Homesteader_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            bool previousRefresh = Settings.useRefreshedTextures;
            listing.CheckboxLabeled(
                "Homesteader_SettingsUseRefreshedTextures".Translate(),
                ref Settings.useRefreshedTextures,
                "Homesteader_SettingsUseRefreshedTexturesTip".Translate());
            listing.Label("Homesteader_SettingsUseRefreshedTexturesRestart".Translate());
            if (previousRefresh != Settings.useRefreshedTextures)
            {
                TextureRefresh.Apply(Settings.useRefreshedTextures);
            }

            listing.GapLine();

            if (Prefs.DevMode)
            {
                listing.Label("Homesteader_SettingsDevHeader".Translate());
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
