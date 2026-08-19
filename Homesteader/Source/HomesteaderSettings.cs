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

        /// <summary>0 = off, 1 = default, 2 = strong. Scales allergy hediff apply + allergen mood.</summary>
        public float allergyFlareIntensity = 1f;

        /// <summary>0 = off, 1 = default, 2 = strong. Scales favorite-food mood.</summary>
        public float favoriteFoodMood = 1f;

        /// <summary>1 = default coop interval. Higher = slower eggs. 0.5–2.</summary>
        public float coopEggIntervalFactor = 1f;

        public bool katsEnabled = true;

        /// <summary>0 = hide cooling inspect, 1 = one line, 2 = extra cell count.</summary>
        public int coolingTooltipVerbosity = 1;

        public bool larderMoodEnabled = true;
        public bool harvestFestivalEnabled = true;
        public bool agingEnabled = true;
        public bool livingWorldFlavor = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref revealAllergies, "revealAllergies", defaultValue: false);
            Scribe_Values.Look(ref useRefreshedTextures, "useRefreshedTextures", defaultValue: false);
            Scribe_Values.Look(ref allergyFlareIntensity, "allergyFlareIntensity", defaultValue: 1f);
            Scribe_Values.Look(ref favoriteFoodMood, "favoriteFoodMood", defaultValue: 1f);
            Scribe_Values.Look(ref coopEggIntervalFactor, "coopEggIntervalFactor", defaultValue: 1f);
            Scribe_Values.Look(ref katsEnabled, "katsEnabled", defaultValue: true);
            Scribe_Values.Look(ref coolingTooltipVerbosity, "coolingTooltipVerbosity", defaultValue: 1);
            Scribe_Values.Look(ref larderMoodEnabled, "larderMoodEnabled", defaultValue: true);
            Scribe_Values.Look(ref harvestFestivalEnabled, "harvestFestivalEnabled", defaultValue: true);
            Scribe_Values.Look(ref agingEnabled, "agingEnabled", defaultValue: true);
            Scribe_Values.Look(ref livingWorldFlavor, "livingWorldFlavor", defaultValue: true);
        }
    }

    public class HomesteaderMod : Mod
    {
        public static HomesteaderSettings Settings;
        public static ModContentPack ContentPack;
        private Vector2 settingsScroll;
        private float settingsContentHeight;

        public HomesteaderMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            Settings = GetSettings<HomesteaderSettings>();
        }

        public override string SettingsCategory() => "Homesteader_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));

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
            listing.Label("Homesteader_SettingsPackHeader".Translate());

            listing.Label("Homesteader_SettingsAllergyFlare".Translate(Settings.allergyFlareIntensity.ToString("F1")));
            Settings.allergyFlareIntensity = listing.Slider(Settings.allergyFlareIntensity, 0f, 2f);

            listing.Label("Homesteader_SettingsFavoriteMood".Translate(Settings.favoriteFoodMood.ToString("F1")));
            Settings.favoriteFoodMood = listing.Slider(Settings.favoriteFoodMood, 0f, 2f);

            listing.Label("Homesteader_SettingsCoopInterval".Translate(Settings.coopEggIntervalFactor.ToString("F1")));
            Settings.coopEggIntervalFactor = listing.Slider(Settings.coopEggIntervalFactor, 0.5f, 2f);

            listing.CheckboxLabeled(
                "Homesteader_SettingsKats".Translate(),
                ref Settings.katsEnabled,
                "Homesteader_SettingsKatsTip".Translate());

            listing.Label("Homesteader_SettingsCoolingVerbosity".Translate(Settings.coolingTooltipVerbosity));
            Settings.coolingTooltipVerbosity = Mathf.RoundToInt(listing.Slider(Settings.coolingTooltipVerbosity, 0f, 2f));

            listing.CheckboxLabeled("Homesteader_SettingsLarderMood".Translate(), ref Settings.larderMoodEnabled);
            listing.CheckboxLabeled("Homesteader_SettingsFestival".Translate(), ref Settings.harvestFestivalEnabled);
            listing.CheckboxLabeled("Homesteader_SettingsAging".Translate(), ref Settings.agingEnabled);
            listing.CheckboxLabeled("Homesteader_SettingsLwFlavor".Translate(), ref Settings.livingWorldFlavor);

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
            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
