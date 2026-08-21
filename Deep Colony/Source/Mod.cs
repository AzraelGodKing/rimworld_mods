using UnityEngine;
using Verse;

namespace DeepColony
{
    public class DeepColonyMod : Mod
    {
        public static DeepColonySettings Settings;
        private Vector2 settingsScroll;

        public DeepColonyMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DeepColonySettings>();
            Log.Message($"[{content.Name}] loaded.");
        }

        public override string SettingsCategory() => "Deep Colony";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var settings = Settings;
            if (settings == null) return;

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, 2080f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

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

            listing.Gap(10f);
            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_Systems".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.CheckboxLabeled("DC_Settings_EnablePerks".Translate(), ref settings.enablePerks,
                "DC_Settings_EnablePerksTip".Translate());
            listing.CheckboxLabeled("DC_Settings_EnableTrauma".Translate(), ref settings.enableTrauma,
                "DC_Settings_EnableTraumaTip".Translate());
            listing.CheckboxLabeled("DC_Settings_EnableMentoring".Translate(), ref settings.enableMentoring,
                "DC_Settings_EnableMentoringTip".Translate());
            listing.CheckboxLabeled("DC_Settings_EnableInheritance".Translate(), ref settings.enableInheritance,
                "DC_Settings_EnableInheritanceTip".Translate());
            listing.CheckboxLabeled("DC_Settings_EnableFactionRep".Translate(), ref settings.enableFactionRep,
                "DC_Settings_EnableFactionRepTip".Translate());
            listing.CheckboxLabeled("DC_Settings_TraumaPenalties".Translate(), ref settings.enableTraumaPenalties,
                "DC_Settings_TraumaPenaltiesTip".Translate());
            listing.CheckboxLabeled("DC_Settings_AttitudeConsequences".Translate(), ref settings.enableAttitudeConsequences,
                "DC_Settings_AttitudeConsequencesTip".Translate());

            listing.Gap(8f);
            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_Phase5".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.CheckboxLabeled("DC_Settings_Capstones".Translate(), ref settings.enableSkill20Capstones,
                "DC_Settings_CapstonesTip".Translate());
            listing.CheckboxLabeled("DC_Settings_Branching".Translate(), ref settings.enableBranchingPerks,
                "DC_Settings_BranchingTip".Translate());
            listing.CheckboxLabeled("DC_Settings_Respec".Translate(), ref settings.enablePerkRespec,
                "DC_Settings_RespecTip".Translate());
            listing.Label("DC_Settings_RespecCooldown".Translate(settings.respecCooldownDays.ToString("F0")));
            settings.respecCooldownDays = listing.Slider(settings.respecCooldownDays, 5f, 30f);
            listing.CheckboxLabeled("DC_Settings_Archetypes".Translate(), ref settings.enableCrossSkillArchetypes,
                "DC_Settings_ArchetypesTip".Translate());
            listing.CheckboxLabeled("DC_Settings_RecruitPrePerks".Translate(), ref settings.enableRecruitPrePerks,
                "DC_Settings_RecruitPrePerksTip".Translate());
            listing.CheckboxLabeled("DC_Settings_Heirlooms".Translate(), ref settings.enableHeirlooms,
                "DC_Settings_HeirloomsTip".Translate());
            listing.CheckboxLabeled("DC_Settings_ChronicTrauma".Translate(), ref settings.enableChronicTrauma,
                "DC_Settings_ChronicTraumaTip".Translate());

            listing.Gap(8f);
            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_BatchC".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.Label("DC_Settings_ChildRaid".Translate((int)(settings.childRaidWitnessChance * 100f)));
            settings.childRaidWitnessChance = listing.Slider(settings.childRaidWitnessChance, 0f, 1f);
            listing.CheckboxLabeled("DC_Settings_PrisonerCounsel".Translate(), ref settings.enablePrisonerCounsel,
                "DC_Settings_PrisonerCounselTip".Translate());
            listing.CheckboxLabeled("DC_Settings_EnvoyVisits".Translate(), ref settings.enableEnvoyVisits,
                "DC_Settings_EnvoyVisitsTip".Translate());
            listing.CheckboxLabeled("DC_Settings_ApologyTribute".Translate(), ref settings.enableApologyTribute,
                "DC_Settings_ApologyTributeTip".Translate());

            listing.Gap(8f);
            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_FamilyJoin".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.CheckboxLabeled("DC_Settings_EnableFamilyJoin".Translate(), ref settings.enableFamilyJoin,
                "DC_Settings_EnableFamilyJoinTip".Translate());
            listing.Label("DC_Settings_RaidDefect".Translate((int)(settings.familyRaidDefectChance * 100f)));
            settings.familyRaidDefectChance = listing.Slider(settings.familyRaidDefectChance, 0f, 1f);
            listing.Label("DC_Settings_VisitJoin".Translate((int)(settings.familyVisitJoinChance * 100f)));
            settings.familyVisitJoinChance = listing.Slider(settings.familyVisitJoinChance, 0f, 1f);
            listing.CheckboxLabeled("DC_Settings_ExLoverReconcile".Translate(), ref settings.enableExLoverReconcile,
                "DC_Settings_ExLoverReconcileTip".Translate());
            listing.Label("DC_Settings_ReconcileMtb".Translate(settings.exLoverReconcileMtbDays.ToString("F0")));
            settings.exLoverReconcileMtbDays = listing.Slider(settings.exLoverReconcileMtbDays, 2f, 20f);
            listing.Label("DC_Settings_UnwaveringOpinion".Translate(settings.familyUnwaveringMinOpinion));
            settings.familyUnwaveringMinOpinion = (int)listing.Slider(settings.familyUnwaveringMinOpinion, 0f, 80f);
            listing.Label("DC_Settings_UnwaveringBreak".Translate((int)(settings.familyUnwaveringBreakChance * 100f)));
            settings.familyUnwaveringBreakChance = listing.Slider(settings.familyUnwaveringBreakChance, 0.05f, 1f);

            listing.Gap(10f);
            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("DC_Settings_Tuning".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.Label("DC_Settings_CombatShock".Translate((int)(settings.combatShockChance * 100f)));
            settings.combatShockChance = listing.Slider(settings.combatShockChance, 0f, 1f);
            listing.Gap(4f);

            listing.Label("DC_Settings_MinSkillLead".Translate(settings.minSkillLead));
            settings.minSkillLead = (int)listing.Slider(settings.minSkillLead, 1f, 8f);
            listing.Gap(4f);

            listing.Label("DC_Settings_PassiveMentor".Translate(settings.passiveMentorMultiplier.ToString("F2")));
            settings.passiveMentorMultiplier = listing.Slider(settings.passiveMentorMultiplier, 1f, 2f);
            listing.Gap(4f);

            listing.Label("DC_Settings_ActiveMentor".Translate(settings.activeMentorMultiplier.ToString("F2")));
            settings.activeMentorMultiplier = listing.Slider(settings.activeMentorMultiplier, 1f, 2.5f);
            listing.Gap(4f);

            listing.Label("DC_Settings_AllyDriftMtb".Translate(settings.allyDriftMtbDays.ToString("F1")));
            settings.allyDriftMtbDays = listing.Slider(settings.allyDriftMtbDays, 1f, 15f);
            listing.Gap(4f);

            listing.Label("DC_Settings_EnemyDriftMtb".Translate(settings.enemyDriftMtbDays.ToString("F1")));
            settings.enemyDriftMtbDays = listing.Slider(settings.enemyDriftMtbDays, 1f, 15f);
            listing.Gap(4f);

            listing.Label("DC_Settings_MassacreThreshold".Translate(settings.massacreDeathThreshold));
            settings.massacreDeathThreshold = (int)listing.Slider(settings.massacreDeathThreshold, 2f, 8f);
            listing.Gap(4f);

            listing.Label("DC_Settings_TraitInherit".Translate((int)(settings.traitInheritChance * 100f)));
            settings.traitInheritChance = listing.Slider(settings.traitInheritChance, 0f, 1f);
            listing.Gap(4f);

            listing.Label("DC_Settings_TherapyScale".Translate(settings.therapyHealScale.ToString("F2")));
            settings.therapyHealScale = listing.Slider(settings.therapyHealScale, 0.25f, 2.5f);
            listing.Gap(12f);

            if (listing.ButtonText("DC_Settings_Reset".Translate(), null, 0.35f))
                settings.ResetToDefaults();

            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
