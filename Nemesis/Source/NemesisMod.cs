using HarmonyLib;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class NemesisMod : Mod
    {
        public static NemesisSettings Settings;

        public NemesisMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NemesisSettings>();
            ModVersionLog.Write("[Nemesis]", content);
            HarmonyPatchAll.Apply(new Harmony("azraelgodking.nemesis"), "[Nemesis]");
        }

        public override string SettingsCategory() => "Nemesis";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

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
            listing.Label("Nemesis_Settings_Captain".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            listing.CheckboxLabeled(
                "Nemesis_Settings_EnableProgression".Translate(),
                ref Settings.enableCaptainProgression,
                "Nemesis_Settings_EnableProgressionTip".Translate());
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_MaxProgression".Translate(Settings.maxProgressionLevel));
            Settings.maxProgressionLevel = (int)listing.Slider(Settings.maxProgressionLevel, 1f, 12f);
            listing.Gap(4f);
            listing.Label("Nemesis_Settings_PostEscapeSabotage".Translate((int)(Settings.postEscapeSabotageWeightMul * 100f)));
            Settings.postEscapeSabotageWeightMul = listing.Slider(Settings.postEscapeSabotageWeightMul, 0f, 1f);
            listing.Gap(4f);
            listing.CheckboxLabeled(
                "Nemesis_Settings_SoftMounts".Translate(),
                ref Settings.enableSoftMounts,
                "Nemesis_Settings_SoftMountsTip".Translate());
            listing.CheckboxLabeled(
                "Nemesis_Settings_SoftMechs".Translate(),
                ref Settings.enableSoftMechs,
                "Nemesis_Settings_SoftMechsTip".Translate());
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
            listing.Gap(12f);

            if (listing.ButtonText("Nemesis_Settings_Reset".Translate(), null, 0.25f))
                Settings.ResetToDefaults();

            listing.End();
        }
    }
}
