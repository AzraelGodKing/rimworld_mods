#nullable enable
using System.Collections.Generic;
using AzraelCommon;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Settings that change the simulation. Each save keeps its own copy
    /// (<see cref="GameComponent_HomesteaderGameplay"/>), so every Multiplayer client
    /// runs the host's values (AZR-527). Mod settings hold the defaults for new games
    /// under the same keys as 1.x, so existing settings files keep their values.
    /// </summary>
    public sealed class HomesteaderGameplaySettings : IExposable
    {
        /// <summary>Prefer closest-to-rot stacks when eating and when preserving.</summary>
        public bool spoilageTriage = true;

        /// <summary>0 = no allergy flares; 1 = default mood/hediff flare.</summary>
        public float allergyFlareIntensity = 1f;

        /// <summary>0 = no favorite-food mood; 1 = default thought strength.</summary>
        public float favoriteFoodMoodFactor = 1f;

        /// <summary>Multiplies chicken-coop egg spawn interval (higher = fewer eggs). 1 = default.</summary>
        public float coopEggIntervalFactor = 1f;

        /// <summary>When false, Homesteader_KatsEffect never fires (baseChance forced to 0).</summary>
        public bool enableKatsEffect = true;

        /// <summary>Well-stocked larder mood from distinct preserves on the map (HS-A02).</summary>
        public bool larderMood = true;

        /// <summary>Rain barrels and cisterns fill faster in rain and stop in a drought (HS-A13).</summary>
        public bool rainAwareWater = true;

        /// <summary>Beehives need flowering plants nearby (HS-A08).</summary>
        public bool beesNeedBloom = true;

        public void ExposeData()
        {
            Scribe_Values.Look(ref spoilageTriage, "spoilageTriage", defaultValue: true);
            Scribe_Values.Look(ref allergyFlareIntensity, "allergyFlareIntensity", defaultValue: 1f);
            Scribe_Values.Look(ref favoriteFoodMoodFactor, "favoriteFoodMoodFactor", defaultValue: 1f);
            Scribe_Values.Look(ref coopEggIntervalFactor, "coopEggIntervalFactor", defaultValue: 1f);
            Scribe_Values.Look(ref enableKatsEffect, "enableKatsEffect", defaultValue: true);
            Scribe_Values.Look(ref larderMood, "larderMood", defaultValue: true);
            Scribe_Values.Look(ref rainAwareWater, "rainAwareWater", defaultValue: true);
            Scribe_Values.Look(ref beesNeedBloom, "beesNeedBloom", defaultValue: true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
            }
        }

        public void Clamp()
        {
            allergyFlareIntensity = Mathf.Clamp(allergyFlareIntensity, 0f, 2f);
            favoriteFoodMoodFactor = Mathf.Clamp(favoriteFoodMoodFactor, 0f, 2f);
            coopEggIntervalFactor = Mathf.Clamp(coopEggIntervalFactor, 0.5f, 2f);
        }

        public HomesteaderGameplaySettings Copy()
        {
            return (HomesteaderGameplaySettings)MemberwiseClone();
        }

        public bool SameAs(HomesteaderGameplaySettings o)
        {
            return spoilageTriage == o.spoilageTriage
                && allergyFlareIntensity == o.allergyFlareIntensity
                && favoriteFoodMoodFactor == o.favoriteFoodMoodFactor
                && coopEggIntervalFactor == o.coopEggIntervalFactor
                && enableKatsEffect == o.enableKatsEffect
                && larderMood == o.larderMood
                && rainAwareWater == o.rainAwareWater
                && beesNeedBloom == o.beesNeedBloom;
        }
    }

    public class HomesteaderSettings : ModSettings
    {
        /// <summary>When true, Tastes tab shows allergy names before discovery. Intended for DevMode testing.</summary>
        public bool revealAllergies = false;

        /// <summary>Optional unique art pack. Off by default; original sprites are never deleted.</summary>
        public bool useRefreshedTextures = false;

        /// <summary>When false, root cellar / icehouse / springhouse omit the passive-cooling inspect line.</summary>
        public bool showCoolingInspect = true;

        /// <summary>Selected root cellar, icehouse or springhouse outlines the cooled cells (AZR-577).</summary>
        public bool showCoolingOverlay = true;

        /// <summary>Heat-dome shimmer and fountain water (AZR-577). Draw-only.</summary>
        public bool showEffects = true;

        /// <summary>Pantry stock list order, kept between sessions (AZR-696).</summary>
        public int pantrySortMode = 0;

        /// <summary>Defaults for new games and for a pre-2.0 save's first load.</summary>
        public HomesteaderGameplaySettings gameplay = new HomesteaderGameplaySettings();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref revealAllergies, "revealAllergies", defaultValue: false);
            Scribe_Values.Look(ref useRefreshedTextures, "useRefreshedTextures", defaultValue: false);
            Scribe_Values.Look(ref showCoolingInspect, "showCoolingInspect", defaultValue: true);
            Scribe_Values.Look(ref showCoolingOverlay, "showCoolingOverlay", defaultValue: true);
            Scribe_Values.Look(ref showEffects, "showEffects", defaultValue: true);
            Scribe_Values.Look(ref pantrySortMode, "pantrySortMode", defaultValue: 0);
            // Same node and keys as 1.x, which kept these flat in the settings file.
            gameplay.ExposeData();
        }
    }

    public class HomesteaderMod : Mod
    {
        /// <summary>Never null: defaults until the mod constructor loads the saved settings.</summary>
        public static HomesteaderSettings Settings = new HomesteaderSettings();
        public static ModContentPack? ContentPack;

        // Edited by the window while a game runs; committed to the save when it closes.
        private HomesteaderGameplaySettings? pendingSave;

        public HomesteaderMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            Settings = GetSettings<HomesteaderSettings>();
            Settings.gameplay.Clamp();
            ModVersionLog.Write("[Homesteader]", content, HomesteaderBuildInfo.BuildStamp);
        }

        public override string SettingsCategory() => "Homesteader_SettingsCategory".Translate();

        private Vector2 settingsScroll;
        private float settingsHeight = 900f;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Scrolls so long translations fit at 1280x720.
            Rect view = new Rect(0f, 0f, inRect.width - 16f, settingsHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            GameComponent_HomesteaderGameplay? save = GameComponent_HomesteaderGameplay.Instance;
            HomesteaderGameplaySettings edit;
            if (save != null)
            {
                pendingSave ??= save.Values.Copy();
                edit = pendingSave;
                listing.Label("Homesteader_SettingsGameplayHeaderSave".Translate());
            }
            else
            {
                pendingSave = null;
                edit = Settings.gameplay;
                listing.Label("Homesteader_SettingsGameplayHeaderNewGames".Translate());
            }

            listing.CheckboxLabeled(
                "Homesteader_SettingsSpoilageTriage".Translate(),
                ref edit.spoilageTriage,
                "Homesteader_SettingsSpoilageTriageTip".Translate());
            if (!PatchHealth.SpoilageBillSortHooked)
            {
                listing.Label("Homesteader_SettingsSpoilageTriageBillsInactive".Translate());
            }

            listing.Label("Homesteader_SettingsAllergyFlare".Translate() + ": " +
                          edit.allergyFlareIntensity.ToString("F1"));
            edit.allergyFlareIntensity = listing.Slider(edit.allergyFlareIntensity, 0f, 2f);
            listing.Label("Homesteader_SettingsFavoriteMood".Translate() + ": " +
                          edit.favoriteFoodMoodFactor.ToString("F1"));
            edit.favoriteFoodMoodFactor = listing.Slider(edit.favoriteFoodMoodFactor, 0f, 2f);
            listing.Label("Homesteader_SettingsCoopEggInterval".Translate() + ": " +
                          edit.coopEggIntervalFactor.ToString("F1"));
            edit.coopEggIntervalFactor = listing.Slider(edit.coopEggIntervalFactor, 0.5f, 2f);
            listing.CheckboxLabeled(
                "Homesteader_SettingsEnableKats".Translate(),
                ref edit.enableKatsEffect,
                "Homesteader_SettingsEnableKatsTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsLarderMood".Translate(),
                ref edit.larderMood,
                "Homesteader_SettingsLarderMoodTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsRainAwareWater".Translate(),
                ref edit.rainAwareWater,
                "Homesteader_SettingsRainAwareWaterTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsBeesNeedBloom".Translate(),
                ref edit.beesNeedBloom,
                "Homesteader_SettingsBeesNeedBloomTip".Translate());
            listing.GapLine();

            listing.Label("Homesteader_SettingsDisplayHeader".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsShowCoolingInspect".Translate(),
                ref Settings.showCoolingInspect,
                "Homesteader_SettingsShowCoolingInspectTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsShowCoolingOverlay".Translate(),
                ref Settings.showCoolingOverlay,
                "Homesteader_SettingsShowCoolingOverlayTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsShowEffects".Translate(),
                ref Settings.showEffects,
                "Homesteader_SettingsShowEffectsTip".Translate());

            if (!TextureRefresh.PackPresent())
            {
                listing.Label("Homesteader_SettingsRefreshPackMissing".Translate());
                Settings.useRefreshedTextures = false;
            }
            else
            {
                listing.CheckboxLabeled(
                    "Homesteader_SettingsUseRefreshedTextures".Translate(),
                    ref Settings.useRefreshedTextures,
                    "Homesteader_SettingsUseRefreshedTexturesTip".Translate());
                listing.Label("Homesteader_SettingsUseRefreshedTexturesRestart".Translate());
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

            settingsHeight = listing.CurHeight + 12f;
            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>Called when the settings window closes.</summary>
        public override void WriteSettings()
        {
            Settings.gameplay.Clamp();
            GameComponent_HomesteaderGameplay? save = GameComponent_HomesteaderGameplay.Instance;
            if (save != null && pendingSave != null)
            {
                pendingSave.Clamp();
                if (!pendingSave.SameAs(save.Values))
                {
                    // Your latest in-game choice also becomes the default for new games.
                    Settings.gameplay = pendingSave.Copy();
                    HomesteaderSync.SetGameplay(
                        pendingSave.spoilageTriage,
                        pendingSave.allergyFlareIntensity,
                        pendingSave.favoriteFoodMoodFactor,
                        pendingSave.coopEggIntervalFactor,
                        pendingSave.enableKatsEffect,
                        pendingSave.larderMood,
                        pendingSave.rainAwareWater,
                        pendingSave.beesNeedBloom);
                }
            }
            else
            {
                HomesteaderGameplayDefs.Apply(HomesteaderGameplay.Active);
            }

            pendingSave = null;
            base.WriteSettings();
        }
    }
}
