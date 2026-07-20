using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class NemesisMod : Mod
    {
        public static NemesisSettings Settings;
        private Vector2 _settingsScroll;
        private float _settingsViewHeight;

        public NemesisMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NemesisSettings>();
            new Harmony("azraelgodking.nemesis").PatchAll();
        }

        public override string SettingsCategory() => "Nemesis";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect view = new Rect(0f, 0f, inRect.width - 16f, _settingsViewHeight > 0f ? _settingsViewHeight : 1200f);
            Widgets.BeginScrollView(inRect, ref _settingsScroll, view);
            var listing = new Listing_Standard();
            listing.Begin(view);

            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp?.Data != null && (comp.Data.active || comp.Data.truceUntilTick > 0))
            {
                listing.Label("Nemesis_Settings_Status".Translate(
                    comp.Data.nemesisName ?? "?",
                    comp.Data.PhaseLabelKeyed(),
                    comp.Data.EffectiveAggression.ToString("F1")));
                listing.Gap(8f);
            }

            listing.Label("Nemesis_Settings_Triggers".Translate());
            listing.Gap(4f);

            listing.Label("Nemesis_Settings_KilledAlly".Translate((int)(Settings.killedAllyChance * 100f)));
            Settings.killedAllyChance = listing.Slider(Settings.killedAllyChance, 0f, 1f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_PrisonerEscaped".Translate((int)(Settings.prisonerEscapedChance * 100f)));
            Settings.prisonerEscapedChance = listing.Slider(Settings.prisonerEscapedChance, 0f, 1f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_SlaveEscaped".Translate((int)(Settings.slaveEscapedChance * 100f)));
            Settings.slaveEscapedChance = listing.Slider(Settings.slaveEscapedChance, 0f, 1f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_Fixation".Translate((int)(Settings.fixationChance * 100f)));
            Settings.fixationChance = listing.Slider(Settings.fixationChance, 0f, 1f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_WoundedEscape".Translate((int)(Settings.woundedEscapeChance * 100f)));
            Settings.woundedEscapeChance = listing.Slider(Settings.woundedEscapeChance, 0f, 1f);
            listing.Gap(8f);

            listing.Label("Nemesis_Settings_TruceDays".Translate(Settings.truceDurationDays));
            Settings.truceDurationDays = (int)listing.Slider(Settings.truceDurationDays, 1f, 120f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_MaxEscapes".Translate(Settings.maxEscapes));
            Settings.maxEscapes = (int)listing.Slider(Settings.maxEscapes, 1f, 12f);
            listing.Gap(10f);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Settings_Pacing".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.Label("Nemesis_Settings_MaxAggression".Translate((int)(Settings.maxAggressionCap * 100f)));
            Settings.maxAggressionCap = listing.Slider(Settings.maxAggressionCap, 0.1f, 1f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_Escalation".Translate(Settings.escalationRatePerDay.ToString("F2")));
            Settings.escalationRatePerDay = listing.Slider(Settings.escalationRatePerDay, 0f, 0.2f);
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_MinCooldown".Translate((Settings.minActionCooldownTicks / 60000f).ToString("F2")));
            Settings.minActionCooldownTicks = (int)(Mathf.Round(
                listing.Slider(Settings.minActionCooldownTicks, 10000f, 100000f) / 5000f) * 5000f);
            listing.Gap(10f);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Settings_ActionMix".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.Label("Nemesis_Settings_WeightTaunt".Translate(Settings.actionWeightTaunt.ToString("F2")));
            Settings.actionWeightTaunt = listing.Slider(Settings.actionWeightTaunt, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightRaid".Translate(Settings.actionWeightRaid.ToString("F2")));
            Settings.actionWeightRaid = listing.Slider(Settings.actionWeightRaid, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightAssault".Translate(Settings.actionWeightAssault.ToString("F2")));
            Settings.actionWeightAssault = listing.Slider(Settings.actionWeightAssault, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightWaste".Translate(Settings.actionWeightWaste.ToString("F2")));
            Settings.actionWeightWaste = listing.Slider(Settings.actionWeightWaste, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightFakeSignal".Translate(Settings.actionWeightFakeSignal.ToString("F2")));
            Settings.actionWeightFakeSignal = listing.Slider(Settings.actionWeightFakeSignal, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightCaravan".Translate(Settings.actionWeightCaravan.ToString("F2")));
            Settings.actionWeightCaravan = listing.Slider(Settings.actionWeightCaravan, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightSabotage".Translate(Settings.actionWeightSabotage.ToString("F2")));
            Settings.actionWeightSabotage = listing.Slider(Settings.actionWeightSabotage, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightFood".Translate(Settings.actionWeightFood.ToString("F2")));
            Settings.actionWeightFood = listing.Slider(Settings.actionWeightFood, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightKidnap".Translate(Settings.actionWeightKidnap.ToString("F2")));
            Settings.actionWeightKidnap = listing.Slider(Settings.actionWeightKidnap, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightSniper".Translate(Settings.actionWeightSniper.ToString("F2")));
            Settings.actionWeightSniper = listing.Slider(Settings.actionWeightSniper, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightGrave".Translate(Settings.actionWeightGrave.ToString("F2")));
            Settings.actionWeightGrave = listing.Slider(Settings.actionWeightGrave, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightFoodTamper".Translate(Settings.actionWeightFoodTamper.ToString("F2")));
            Settings.actionWeightFoodTamper = listing.Slider(Settings.actionWeightFoodTamper, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_WeightInformant".Translate(Settings.actionWeightInformant.ToString("F2")));
            Settings.actionWeightInformant = listing.Slider(Settings.actionWeightInformant, 0f, 1f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_RaidCredit".Translate((int)(Settings.raidCreditChance * 100f)));
            Settings.raidCreditChance = listing.Slider(Settings.raidCreditChance, 0f, 1f);
            listing.Gap(8f);

            listing.Label("Nemesis_Settings_IgnoredThreshold".Translate(Settings.ignoredActionsThreshold));
            Settings.ignoredActionsThreshold = (int)listing.Slider(Settings.ignoredActionsThreshold, 2f, 8f);
            listing.Gap(12f);

            if (listing.ButtonText("Nemesis_Settings_Reset".Translate(), null, 0.25f))
                Settings.ResetToDefaults();

            listing.Gap(12f);
            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Settings_TrophyLedger".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            List<NemesisTrophyEntry> trophies = GameComponent_Nemesis.Instance?.Trophies;
            if (trophies == null || trophies.Count == 0)
            {
                listing.Label("Nemesis_Settings_TrophyEmpty".Translate());
            }
            else
            {
                for (int i = trophies.Count - 1; i >= 0; i--)
                {
                    NemesisTrophyEntry t = trophies[i];
                    if (t == null) continue;
                    listing.Label("Nemesis_Settings_TrophyLine".Translate(
                        t.nemesisName ?? "?",
                        t.factionName ?? "?",
                        t.trigger.ToString(),
                        t.endReason.ToString()));
                }
            }

            listing.End();
            _settingsViewHeight = listing.CurHeight + 24f;
            Widgets.EndScrollView();
        }
    }
}
