using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public class NemesisMod : Mod
    {
        public static NemesisSettings Settings;
        private Vector2 _settingsScroll;
        private float _settingsViewHeight;
        private bool _showAdvanced = true;

        public NemesisMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NemesisSettings>();
            new Harmony("azraelgodking.nemesis").PatchAll();
        }

        public override string SettingsCategory() => "Nemesis";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect view = new Rect(0f, 0f, inRect.width - 16f, _settingsViewHeight > 0f ? _settingsViewHeight : 1600f);
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
                listing.Gap(6f);

                if (comp.Data.active)
                {
                    GUI.color = new Color(0.85f, 0.25f, 0.25f);
                    if (listing.ButtonText("Nemesis_Settings_ResolveHunt".Translate()))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "Nemesis_Settings_ResolveHuntConfirm".Translate(comp.Data.nemesisName),
                            () =>
                            {
                                GameComponent_Nemesis.Instance?.EndHunt(NemesisEndReason.Cleared);
                                Messages.Message("Nemesis_Settings_ResolveHuntDone".Translate(), MessageTypeDefOf.NeutralEvent);
                            }));
                    }
                    GUI.color = Color.white;
                    listing.Gap(8f);
                }
            }

            listing.Label("Nemesis_Settings_Preset".Translate());
            listing.Gap(4f);
            if (listing.ButtonTextLabeled("Nemesis_Settings_PresetCurrent".Translate(PresetLabel(Settings.difficultyPreset)),
                    "Nemesis_Settings_PresetPick".Translate()))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Nemesis_Preset_Subtle".Translate(), () => Settings.ApplyPreset(NemesisDifficultyPreset.Subtle)),
                    new FloatMenuOption("Nemesis_Preset_Classic".Translate(), () => Settings.ApplyPreset(NemesisDifficultyPreset.Classic)),
                    new FloatMenuOption("Nemesis_Preset_Relentless".Translate(), () => Settings.ApplyPreset(NemesisDifficultyPreset.Relentless)),
                };
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            listing.Gap(10f);

            if (listing.ButtonText(_showAdvanced
                    ? "Nemesis_Settings_AdvancedHide".Translate()
                    : "Nemesis_Settings_AdvancedShow".Translate()))
                _showAdvanced = !_showAdvanced;

            if (_showAdvanced)
            {
                listing.Gap(8f);
                DrawAdvanced(listing);
            }

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

        private void DrawAdvanced(Listing_Standard listing)
        {
            listing.Label("Nemesis_Settings_Triggers".Translate());
            listing.Gap(4f);

            Slider(listing, "Nemesis_Settings_KilledAlly", ref Settings.killedAllyChance, 0f, 1f, pct: true);
            Slider(listing, "Nemesis_Settings_PrisonerEscaped", ref Settings.prisonerEscapedChance, 0f, 1f, pct: true);
            Slider(listing, "Nemesis_Settings_SlaveEscaped", ref Settings.slaveEscapedChance, 0f, 1f, pct: true);
            Slider(listing, "Nemesis_Settings_Fixation", ref Settings.fixationChance, 0f, 1f, pct: true);
            Slider(listing, "Nemesis_Settings_WoundedEscape", ref Settings.woundedEscapeChance, 0f, 1f, pct: true);

            listing.Label("Nemesis_Settings_TruceDays".Translate(Settings.truceDurationDays));
            float truce = listing.Slider(Settings.truceDurationDays, 1f, 120f);
            if ((int)truce != Settings.truceDurationDays) { Settings.truceDurationDays = (int)truce; Settings.MarkCustom(); }
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_MaxEscapes".Translate(Settings.maxEscapes));
            float esc = listing.Slider(Settings.maxEscapes, 1f, 12f);
            if ((int)esc != Settings.maxEscapes) { Settings.maxEscapes = (int)esc; Settings.MarkCustom(); }
            listing.Gap(10f);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Settings_Pacing".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            Slider(listing, "Nemesis_Settings_MaxAggression", ref Settings.maxAggressionCap, 0.1f, 1f, pct: true);
            listing.Label("Nemesis_Settings_Escalation".Translate(Settings.escalationRatePerDay.ToString("F2")));
            float escRate = listing.Slider(Settings.escalationRatePerDay, 0f, 0.2f);
            if (!Mathf.Approximately(escRate, Settings.escalationRatePerDay)) { Settings.escalationRatePerDay = escRate; Settings.MarkCustom(); }
            listing.Gap(6f);

            listing.Label("Nemesis_Settings_MinCooldown".Translate((Settings.minActionCooldownTicks / 60000f).ToString("F2")));
            float cool = listing.Slider(Settings.minActionCooldownTicks, 10000f, 100000f);
            int coolTicks = (int)(Mathf.Round(cool / 5000f) * 5000f);
            if (coolTicks != Settings.minActionCooldownTicks) { Settings.minActionCooldownTicks = coolTicks; Settings.MarkCustom(); }
            listing.Gap(10f);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("Nemesis_Settings_ActionMix".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            Weight(listing, "Nemesis_Settings_WeightTaunt", ref Settings.actionWeightTaunt);
            Weight(listing, "Nemesis_Settings_WeightRaid", ref Settings.actionWeightRaid);
            Weight(listing, "Nemesis_Settings_WeightAssault", ref Settings.actionWeightAssault);
            Weight(listing, "Nemesis_Settings_WeightWaste", ref Settings.actionWeightWaste);
            Weight(listing, "Nemesis_Settings_WeightFakeSignal", ref Settings.actionWeightFakeSignal);
            Weight(listing, "Nemesis_Settings_WeightCaravan", ref Settings.actionWeightCaravan);
            Weight(listing, "Nemesis_Settings_WeightSabotage", ref Settings.actionWeightSabotage);
            Weight(listing, "Nemesis_Settings_WeightFood", ref Settings.actionWeightFood);
            Weight(listing, "Nemesis_Settings_WeightKidnap", ref Settings.actionWeightKidnap);
            Weight(listing, "Nemesis_Settings_WeightSniper", ref Settings.actionWeightSniper);
            Weight(listing, "Nemesis_Settings_WeightGrave", ref Settings.actionWeightGrave);
            Weight(listing, "Nemesis_Settings_WeightFoodTamper", ref Settings.actionWeightFoodTamper);
            Weight(listing, "Nemesis_Settings_WeightInformant", ref Settings.actionWeightInformant);
            Slider(listing, "Nemesis_Settings_RaidCredit", ref Settings.raidCreditChance, 0f, 1f, pct: true);

            listing.Label("Nemesis_Settings_IgnoredThreshold".Translate(Settings.ignoredActionsThreshold));
            float ign = listing.Slider(Settings.ignoredActionsThreshold, 2f, 8f);
            if ((int)ign != Settings.ignoredActionsThreshold) { Settings.ignoredActionsThreshold = (int)ign; Settings.MarkCustom(); }
            listing.Gap(12f);

            if (listing.ButtonText("Nemesis_Settings_Reset".Translate(), null, 0.25f))
                Settings.ResetToDefaults();
        }

        private void Slider(Listing_Standard listing, string key, ref float value, float min, float max, bool pct = false)
        {
            listing.Label(pct ? key.Translate((int)(value * 100f)) : key.Translate(value.ToString("F2")));
            float next = listing.Slider(value, min, max);
            if (!Mathf.Approximately(next, value))
            {
                value = next;
                Settings.MarkCustom();
            }
            listing.Gap(4f);
        }

        private void Weight(Listing_Standard listing, string key, ref float value)
        {
            listing.Label(key.Translate(value.ToString("F2")));
            float next = listing.Slider(value, 0f, 1f);
            if (!Mathf.Approximately(next, value))
            {
                value = next;
                Settings.MarkCustom();
            }
            listing.Gap(4f);
        }

        private static string PresetLabel(NemesisDifficultyPreset p) => p switch
        {
            NemesisDifficultyPreset.Subtle => "Nemesis_Preset_Subtle".Translate(),
            NemesisDifficultyPreset.Relentless => "Nemesis_Preset_Relentless".Translate(),
            NemesisDifficultyPreset.Custom => "Nemesis_Preset_Custom".Translate(),
            _ => "Nemesis_Preset_Classic".Translate(),
        };
    }
}
