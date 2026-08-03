using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class LivingWorldSettings : ModSettings
    {
        public bool enabled = true;
        public bool morphEnabled = true;
        public bool chronicleEnabled = true;
        public bool diplomacyEnabled = true;
        public bool falloutEnabled = true;
        public bool tradeBlackoutEnabled = true;
        public bool warSitesEnabled = true;
        public bool warbandFalloutEnabled = false;

        /// <summary>0 = major only, 1 = medium, 2 = high.</summary>
        public int newsVerbosity = 1;

        public int tickInterval = 10000;
        public int resolutionsPerPulse = 1;
        public int maxLettersPerQuadrum = 8;
        public int maxMorphsPerYear = 6;
        public int proximityTiles = 30;
        public int maxConcurrentWars = 2;

        /// <summary>0.25–2 multiplier on escalate / battle / betrayal chances.</summary>
        public float warRate = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref morphEnabled, "morphEnabled", true);
            Scribe_Values.Look(ref chronicleEnabled, "chronicleEnabled", true);
            Scribe_Values.Look(ref diplomacyEnabled, "diplomacyEnabled", true);
            Scribe_Values.Look(ref falloutEnabled, "falloutEnabled", true);
            Scribe_Values.Look(ref tradeBlackoutEnabled, "tradeBlackoutEnabled", true);
            Scribe_Values.Look(ref warSitesEnabled, "warSitesEnabled", true);
            Scribe_Values.Look(ref warbandFalloutEnabled, "warbandFalloutEnabled", false);
            Scribe_Values.Look(ref newsVerbosity, "newsVerbosity", 1);
            Scribe_Values.Look(ref tickInterval, "tickInterval", 10000);
            Scribe_Values.Look(ref resolutionsPerPulse, "resolutionsPerPulse", 1);
            Scribe_Values.Look(ref maxLettersPerQuadrum, "maxLettersPerQuadrum", 8);
            Scribe_Values.Look(ref maxMorphsPerYear, "maxMorphsPerYear", 6);
            Scribe_Values.Look(ref proximityTiles, "proximityTiles", 30);
            Scribe_Values.Look(ref maxConcurrentWars, "maxConcurrentWars", 2);
            Scribe_Values.Look(ref warRate, "warRate", 1f);
        }

        public void ResetToDefaults()
        {
            enabled = true;
            morphEnabled = true;
            chronicleEnabled = true;
            diplomacyEnabled = true;
            falloutEnabled = true;
            tradeBlackoutEnabled = true;
            warSitesEnabled = true;
            warbandFalloutEnabled = false;
            newsVerbosity = 1;
            tickInterval = 10000;
            resolutionsPerPulse = 1;
            maxLettersPerQuadrum = 8;
            maxMorphsPerYear = 6;
            proximityTiles = 30;
            maxConcurrentWars = 2;
            warRate = 1f;
        }

        public static void DoSettingsWindow(Rect inRect)
        {
            LivingWorldSettings s = LivingWorldMod.Settings;
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("LivingWorld_Settings_Enabled".Translate(), ref s.enabled,
                "LivingWorld_Settings_EnabledDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Chronicle".Translate(), ref s.chronicleEnabled,
                "LivingWorld_Settings_ChronicleDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Morph".Translate(), ref s.morphEnabled,
                "LivingWorld_Settings_MorphDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Diplomacy".Translate(), ref s.diplomacyEnabled,
                "LivingWorld_Settings_DiplomacyDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Fallout".Translate(), ref s.falloutEnabled,
                "LivingWorld_Settings_FalloutDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_TradeBlackout".Translate(), ref s.tradeBlackoutEnabled,
                "LivingWorld_Settings_TradeBlackoutDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_WarSites".Translate(), ref s.warSitesEnabled,
                "LivingWorld_Settings_WarSitesDesc".Translate());
            listing.CheckboxLabeled("LivingWorld_Settings_Warband".Translate(), ref s.warbandFalloutEnabled,
                "LivingWorld_Settings_WarbandDesc".Translate());
            listing.GapLine();

            listing.Label("LivingWorld_Settings_Verbosity".Translate() + ": "
                + VerbosityLabel(s.newsVerbosity));
            s.newsVerbosity = Mathf.RoundToInt(listing.Slider(s.newsVerbosity, 0f, 2f));
            listing.Label("LivingWorld_Settings_TickInterval".Translate(s.tickInterval));
            s.tickInterval = Mathf.RoundToInt(listing.Slider(s.tickInterval, 5000f, 30000f) / 1000f) * 1000;
            listing.Label("LivingWorld_Settings_Resolutions".Translate(s.resolutionsPerPulse));
            s.resolutionsPerPulse = Mathf.RoundToInt(listing.Slider(s.resolutionsPerPulse, 1f, 3f));
            listing.Label("LivingWorld_Settings_MaxLetters".Translate(s.maxLettersPerQuadrum));
            s.maxLettersPerQuadrum = Mathf.RoundToInt(listing.Slider(s.maxLettersPerQuadrum, 2f, 20f));
            listing.Label("LivingWorld_Settings_MaxMorphs".Translate(s.maxMorphsPerYear));
            s.maxMorphsPerYear = Mathf.RoundToInt(listing.Slider(s.maxMorphsPerYear, 1f, 20f));
            listing.Label("LivingWorld_Settings_Proximity".Translate(s.proximityTiles));
            s.proximityTiles = Mathf.RoundToInt(listing.Slider(s.proximityTiles, 10f, 80f));
            listing.Label("LivingWorld_Settings_MaxWars".Translate(s.maxConcurrentWars));
            s.maxConcurrentWars = Mathf.RoundToInt(listing.Slider(s.maxConcurrentWars, 1f, 5f));
            listing.Label("LivingWorld_Settings_WarRate".Translate(s.warRate.ToString("F2")));
            s.warRate = listing.Slider(s.warRate, 0.25f, 2f);

            listing.Gap();
            if (listing.ButtonText("LivingWorld_Settings_Reset".Translate(), null, 0.25f))
            {
                s.ResetToDefaults();
            }

            listing.End();
        }

        private static string VerbosityLabel(int level)
        {
            switch (level)
            {
                case 0: return "LivingWorld_Verbosity_Low".Translate();
                case 2: return "LivingWorld_Verbosity_High".Translate();
                default: return "LivingWorld_Verbosity_Medium".Translate();
            }
        }
    }
}
