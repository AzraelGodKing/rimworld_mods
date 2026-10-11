using System.Collections.Generic;
using RimWorld;
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

        /// <summary>When false, root cellar / icehouse / springhouse omit the passive-cooling inspect line.</summary>
        public bool showCoolingInspect = true;

        private static readonly Dictionary<string, float> OriginalThoughtMood =
            new Dictionary<string, float>();

        private static IntRange? OriginalCoopEggInterval;

        private static float? OriginalKatsChance;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref revealAllergies, "revealAllergies", defaultValue: false);
            Scribe_Values.Look(ref useRefreshedTextures, "useRefreshedTextures", defaultValue: false);
            Scribe_Values.Look(ref spoilageTriage, "spoilageTriage", defaultValue: true);
            Scribe_Values.Look(ref allergyFlareIntensity, "allergyFlareIntensity", defaultValue: 1f);
            Scribe_Values.Look(ref favoriteFoodMoodFactor, "favoriteFoodMoodFactor", defaultValue: 1f);
            Scribe_Values.Look(ref coopEggIntervalFactor, "coopEggIntervalFactor", defaultValue: 1f);
            Scribe_Values.Look(ref enableKatsEffect, "enableKatsEffect", defaultValue: true);
            Scribe_Values.Look(ref showCoolingInspect, "showCoolingInspect", defaultValue: true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
                ApplyGameplaySettings();
            }
        }

        public void Clamp()
        {
            allergyFlareIntensity = Mathf.Clamp(allergyFlareIntensity, 0f, 2f);
            favoriteFoodMoodFactor = Mathf.Clamp(favoriteFoodMoodFactor, 0f, 2f);
            coopEggIntervalFactor = Mathf.Clamp(coopEggIntervalFactor, 0.5f, 2f);
        }

        /// <summary>
        /// Push tunable values into defs that Homesteader owns (thoughts, coop spawner, Kats incident).
        /// Safe to call before defs exist — no-ops until the database is ready.
        /// </summary>
        public void ApplyGameplaySettings()
        {
            Clamp();
            ApplyThoughtMood("Homesteader_AteFavoriteFood", favoriteFoodMoodFactor);
            ApplyThoughtMood("Homesteader_AteAllergen", allergyFlareIntensity);
            ApplyCoopEggInterval();
            ApplyKatsChance();
        }

        private static void RememberThoughtMood(string defName)
        {
            if (OriginalThoughtMood.ContainsKey(defName))
            {
                return;
            }

            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (def?.stages == null || def.stages.Count == 0)
            {
                return;
            }

            OriginalThoughtMood[defName] = def.stages[0].baseMoodEffect;
        }

        private static void ApplyThoughtMood(string defName, float factor)
        {
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (def?.stages == null || def.stages.Count == 0)
            {
                return;
            }

            RememberThoughtMood(defName);
            if (!OriginalThoughtMood.TryGetValue(defName, out float original))
            {
                return;
            }

            def.stages[0].baseMoodEffect = original * factor;
        }

        private static void ApplyCoopEggInterval()
        {
            ThingDef coop = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_ChickenCoop");
            if (coop?.comps == null)
            {
                return;
            }

            for (int i = 0; i < coop.comps.Count; i++)
            {
                if (!(coop.comps[i] is CompProperties_Spawner spawner) || spawner.saveKeysPrefix != "eggs")
                {
                    continue;
                }

                if (OriginalCoopEggInterval == null)
                {
                    OriginalCoopEggInterval = spawner.spawnIntervalRange;
                }

                IntRange original = OriginalCoopEggInterval.Value;
                float factor = HomesteaderMod.Settings != null
                    ? HomesteaderMod.Settings.coopEggIntervalFactor
                    : 1f;
                spawner.spawnIntervalRange = new IntRange(
                    Mathf.Max(1, Mathf.RoundToInt(original.min * factor)),
                    Mathf.Max(1, Mathf.RoundToInt(original.max * factor)));
                return;
            }
        }

        private static void ApplyKatsChance()
        {
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("Homesteader_KatsEffect");
            if (def == null)
            {
                return;
            }

            if (OriginalKatsChance == null)
            {
                OriginalKatsChance = def.baseChance;
            }

            bool enabled = HomesteaderMod.Settings == null || HomesteaderMod.Settings.enableKatsEffect;
            def.baseChance = enabled ? OriginalKatsChance.Value : 0f;
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
            ModVersionLog.Write("[Homesteader]", content, "harvest-aging-v1");
        }

        public override string SettingsCategory() => "Homesteader_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            float prevAllergy = Settings.allergyFlareIntensity;
            float prevFavorite = Settings.favoriteFoodMoodFactor;
            float prevCoop = Settings.coopEggIntervalFactor;
            bool prevKats = Settings.enableKatsEffect;

            listing.CheckboxLabeled(
                "Homesteader_SettingsSpoilageTriage".Translate(),
                ref Settings.spoilageTriage,
                "Homesteader_SettingsSpoilageTriageTip".Translate());
            if (!PatchHealth.SpoilageBillSortHooked)
            {
                listing.Label("Homesteader_SettingsSpoilageTriageBillsInactive".Translate());
            }

            listing.GapLine();

            listing.Label("Homesteader_SettingsGameplayHeader".Translate());
            listing.Label("Homesteader_SettingsAllergyFlare".Translate() + ": " +
                          Settings.allergyFlareIntensity.ToString("F1"));
            Settings.allergyFlareIntensity = listing.Slider(Settings.allergyFlareIntensity, 0f, 2f);
            listing.Label("Homesteader_SettingsFavoriteMood".Translate() + ": " +
                          Settings.favoriteFoodMoodFactor.ToString("F1"));
            Settings.favoriteFoodMoodFactor = listing.Slider(Settings.favoriteFoodMoodFactor, 0f, 2f);
            listing.Label("Homesteader_SettingsCoopEggInterval".Translate() + ": " +
                          Settings.coopEggIntervalFactor.ToString("F1"));
            Settings.coopEggIntervalFactor = listing.Slider(Settings.coopEggIntervalFactor, 0.5f, 2f);
            listing.CheckboxLabeled(
                "Homesteader_SettingsEnableKats".Translate(),
                ref Settings.enableKatsEffect,
                "Homesteader_SettingsEnableKatsTip".Translate());
            listing.CheckboxLabeled(
                "Homesteader_SettingsShowCoolingInspect".Translate(),
                ref Settings.showCoolingInspect,
                "Homesteader_SettingsShowCoolingInspectTip".Translate());
            listing.GapLine();

            if (!TextureRefresh.PackPresent())
            {
                listing.Label("Homesteader_SettingsRefreshPackMissing".Translate());
                if (Settings.useRefreshedTextures)
                {
                    Settings.useRefreshedTextures = false;
                    TextureRefresh.Apply(false);
                }
            }
            else
            {
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
            if (prevAllergy != Settings.allergyFlareIntensity
                || prevFavorite != Settings.favoriteFoodMoodFactor
                || prevCoop != Settings.coopEggIntervalFactor
                || prevKats != Settings.enableKatsEffect)
            {
                Settings.ApplyGameplaySettings();
            }

            base.DoSettingsWindowContents(inRect);
        }
    }
}
