using UnityEngine;
using Verse;

namespace DeepColony
{
    public class DeepColonyMod : Mod
    {
        public static DeepColonySettings Settings;
        private Vector2 settingsScroll;
        private float settingsContentHeight = 4000f;

        public DeepColonyMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DeepColonySettings>();
        }

        public override string SettingsCategory() => "Deep Colony";

        private static void Section(Listing_Standard listing, string key)
        {
            listing.Gap(10f);
            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(key.Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
        }

        // Every checkbox has a "<key>Tip" tooltip.
        private static void Check(Listing_Standard listing, string key, ref bool value)
        {
            listing.CheckboxLabeled(key.Translate(), ref value, (key + "Tip").Translate());
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var settings = Settings;
            if (settings == null) return;

            var listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            var viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));

            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_Presets".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.Label("DC_Settings_PresetsTip".Translate());
            listing.Gap(4f);

            float presetH = 30f;
            Rect presetRow = listing.GetRect(presetH);
            float bw = (presetRow.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(presetRow.x, presetRow.y, bw, presetH),
                    "DC_Settings_PresetSoft".Translate()))
                settings.ApplyPreset(DeepColonySettings.Preset.Soft);
            if (Widgets.ButtonText(new Rect(presetRow.x + bw + 8f, presetRow.y, bw, presetH),
                    "DC_Settings_PresetDefault".Translate()))
                settings.ApplyPreset(DeepColonySettings.Preset.Default);
            if (Widgets.ButtonText(new Rect(presetRow.x + 2f * (bw + 8f), presetRow.y, bw, presetH),
                    "DC_Settings_PresetHard".Translate()))
                settings.ApplyPreset(DeepColonySettings.Preset.Hard);

            // Grouped by system (AZR-622). The Soft/Default/Hard presets set the gameplay
            // values; the Display and Safety switches are left alone.
            Section(listing, "DC_Settings_Section_Perks");
            Check(listing, "DC_Settings_EnablePerks", ref settings.enablePerks);
            Check(listing, "DC_Settings_Capstones", ref settings.enableSkill20Capstones);
            Check(listing, "DC_Settings_Branching", ref settings.enableBranchingPerks);
            Check(listing, "DC_Settings_Respec", ref settings.enablePerkRespec);
            listing.Label("DC_Settings_RespecCooldown".Translate(settings.respecCooldownDays.ToString("F0")));
            settings.respecCooldownDays = listing.Slider(settings.respecCooldownDays, 5f, 30f);
            Check(listing, "DC_Settings_Archetypes", ref settings.enableCrossSkillArchetypes);
            Check(listing, "DC_Settings_NpcPerks", ref settings.npcPerks);

            Section(listing, "DC_Settings_Section_Trauma");
            Check(listing, "DC_Settings_EnableTrauma", ref settings.enableTrauma);
            Check(listing, "DC_Settings_TraumaPenalties", ref settings.enableTraumaPenalties);
            Check(listing, "DC_Settings_ChronicTrauma", ref settings.enableChronicTrauma);
            Check(listing, "DC_Settings_PrisonerCounsel", ref settings.enablePrisonerCounsel);
            listing.Label("DC_Settings_CombatShock".Translate((int)(settings.combatShockChance * 100f)));
            settings.combatShockChance = listing.Slider(settings.combatShockChance, 0f, 1f);
            listing.Label("DC_Settings_ChildRaid".Translate((int)(settings.childRaidWitnessChance * 100f)));
            settings.childRaidWitnessChance = listing.Slider(settings.childRaidWitnessChance, 0f, 1f);
            listing.Label("DC_Settings_MassacreThreshold".Translate(settings.massacreDeathThreshold));
            settings.massacreDeathThreshold = (int)listing.Slider(settings.massacreDeathThreshold, 2f, 8f);
            listing.Label("DC_Settings_TherapyScale".Translate(settings.therapyHealScale.ToString("F2")));
            settings.therapyHealScale = listing.Slider(settings.therapyHealScale, 0.25f, 2.5f);

            Section(listing, "DC_Settings_Section_Mentoring");
            Check(listing, "DC_Settings_EnableMentoring", ref settings.enableMentoring);
            Check(listing, "DC_Settings_EnablePerkRetrain", ref settings.enablePerkRetrain);
            listing.Label("DC_Settings_MinSkillLead".Translate(settings.minSkillLead));
            settings.minSkillLead = (int)listing.Slider(settings.minSkillLead, 1f, 8f);
            listing.Label("DC_Settings_PassiveMentor".Translate(settings.passiveMentorMultiplier.ToString("F2")));
            settings.passiveMentorMultiplier = listing.Slider(settings.passiveMentorMultiplier, 1f, 2f);
            listing.Label("DC_Settings_ActiveMentor".Translate(settings.activeMentorMultiplier.ToString("F2")));
            settings.activeMentorMultiplier = listing.Slider(settings.activeMentorMultiplier, 1f, 2.5f);
            Check(listing, "DC_Settings_EnableQuietHours", ref settings.enableQuietHours);
            listing.Label("DC_Settings_QuietHoursIntensity".Translate(
                settings.quietHoursIntensity.ToString("F2")));
            settings.quietHoursIntensity = listing.Slider(settings.quietHoursIntensity, 0.35f, 2f);

            Section(listing, "DC_Settings_Section_Family");
            Check(listing, "DC_Settings_EnableInheritance", ref settings.enableInheritance);
            listing.Label("DC_Settings_TraitInherit".Translate((int)(settings.traitInheritChance * 100f)));
            settings.traitInheritChance = listing.Slider(settings.traitInheritChance, 0f, 1f);
            Check(listing, "DC_Settings_EnableEstate", ref settings.enableEstate);
            Check(listing, "DC_Settings_Heirlooms", ref settings.enableHeirlooms);
            Check(listing, "DC_Settings_EnableFamilyJoin", ref settings.enableFamilyJoin);
            listing.Label("DC_Settings_RaidDefect".Translate((int)(settings.familyRaidDefectChance * 100f)));
            settings.familyRaidDefectChance = listing.Slider(settings.familyRaidDefectChance, 0f, 1f);
            listing.Label("DC_Settings_VisitJoin".Translate((int)(settings.familyVisitJoinChance * 100f)));
            settings.familyVisitJoinChance = listing.Slider(settings.familyVisitJoinChance, 0f, 1f);
            listing.Label("DC_Settings_UnwaveringOpinion".Translate(settings.familyUnwaveringMinOpinion));
            settings.familyUnwaveringMinOpinion = (int)listing.Slider(settings.familyUnwaveringMinOpinion, 0f, 80f);
            listing.Label("DC_Settings_UnwaveringBreak".Translate((int)(settings.familyUnwaveringBreakChance * 100f)));
            settings.familyUnwaveringBreakChance = listing.Slider(settings.familyUnwaveringBreakChance, 0.05f, 1f);
            Check(listing, "DC_Settings_ExLoverReconcile", ref settings.enableExLoverReconcile);
            listing.Label("DC_Settings_ReconcileMtb".Translate(settings.exLoverReconcileMtbDays.ToString("F0")));
            settings.exLoverReconcileMtbDays = listing.Slider(settings.exLoverReconcileMtbDays, 2f, 20f);
            Check(listing, "DC_Settings_EnableTouchAverse", ref settings.enableTouchAverse);
            listing.Label("DC_Settings_TouchComfortDays".Translate(settings.touchComfortDays.ToString("F1")));
            settings.touchComfortDays = listing.Slider(settings.touchComfortDays, 1f, 10f);
            listing.Label("DC_Settings_TouchComfortThreshold".Translate((int)(settings.touchComfortThreshold * 100f)));
            settings.touchComfortThreshold = listing.Slider(settings.touchComfortThreshold, 0.35f, 0.9f);

            Section(listing, "DC_Settings_Section_Reputation");
            Check(listing, "DC_Settings_EnableFactionRep", ref settings.enableFactionRep);
            Check(listing, "DC_Settings_AttitudeConsequences", ref settings.enableAttitudeConsequences);
            Check(listing, "DC_Settings_EnvoyVisits", ref settings.enableEnvoyVisits);
            Check(listing, "DC_Settings_ApologyTribute", ref settings.enableApologyTribute);
            listing.Label("DC_Settings_AllyDriftMtb".Translate(settings.allyDriftMtbDays.ToString("F1")));
            settings.allyDriftMtbDays = listing.Slider(settings.allyDriftMtbDays, 1f, 15f);
            listing.Label("DC_Settings_EnemyDriftMtb".Translate(settings.enemyDriftMtbDays.ToString("F1")));
            settings.enemyDriftMtbDays = listing.Slider(settings.enemyDriftMtbDays, 1f, 15f);
            Check(listing, "DC_Settings_DiplomacyCompat", ref settings.enableDiplomacyCompat);

            Section(listing, "DC_Settings_Section_BodyCodex");
            Check(listing, "DC_Settings_EnableBody", ref settings.enableBody);
            Check(listing, "DC_Settings_EnableCodex", ref settings.enableCodex);

            Section(listing, "DC_Settings_Section_Safety");
            Check(listing, "DC_Settings_EnableBirthSafetyNet", ref settings.enableBirthSafetyNet);

            Section(listing, "DC_Settings_Section_Display");
            Check(listing, "DC_Settings_ShowPerkHediffs", ref settings.showPerkHediffs);
            Check(listing, "DC_Settings_AnnounceVisitorPerks", ref settings.announceVisitorPerkUnlocks);
            Check(listing, "DC_Settings_FamilyTreePedigree", ref settings.familyTreePedigreeStyle);
            listing.Gap(12f);

            if (listing.ButtonText("DC_Settings_Reset".Translate(), null, 0.35f))
                settings.ResetToDefaults();

            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
