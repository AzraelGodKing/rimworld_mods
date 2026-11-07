#nullable enable
using UnityEngine;
using Verse;

namespace Niceties
{
    public class NicetiesMod : Mod
    {
        public static NicetiesSettings Settings = new NicetiesSettings();

        private static readonly bool[] Expanded = { true, true, true, true, true, true, true };

        /// <summary>Vanilla animals offered as one-click melee caps (AZR-702). Missing defs are skipped.</summary>
        private static readonly string[] CapButtons = { "Cat", "Alpaca", "Deer", "Muffalo" };

        private Vector2 settingsScroll;
        private float settingsContentHeight = 2000f;

        public NicetiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NicetiesSettings>();
        }

        public override string SettingsCategory() => "Niceties_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            NicetiesSimFlags before = Settings.sim;
            NicetiesUiPrefs beforeUi = Settings.ui;

            Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, viewHeight));

            listing.Label("Niceties_Settings_Intro".Translate());
            listing.Gap(6f);
            DrawPresets(listing);
            DrawSaveSnapshotNotice(listing);

            ref NicetiesSimFlags sim = ref Settings.sim;

            if (DrawFeature(listing, NicetiesFeature.ApparelCare, "Niceties_Settings_ApparelCare",
                    "Niceties_Settings_ApparelCareTip", ref sim.enableApparelCare))
            {
                listing.CheckboxLabeled("Niceties_Settings_QualityScale".Translate(),
                    ref sim.apparelQualityScaling, "Niceties_Settings_QualityScaleTip".Translate());
                listing.CheckboxLabeled("Niceties_Settings_CraftingBonus".Translate(),
                    ref sim.apparelCraftingBonus, "Niceties_Settings_CraftingBonusTip".Translate());
                listing.CheckboxLabeled("Niceties_Settings_CorpseApparel".Translate(),
                    ref sim.protectCorpseApparel, "Niceties_Settings_CorpseApparelTip".Translate());
            }

            DrawFeature(listing, NicetiesFeature.ThroneAltar, "Niceties_Settings_ThroneAltar",
                "Niceties_Settings_ThroneAltarTip", ref sim.allowThroneAltars);

            if (DrawFeature(listing, NicetiesFeature.WearAnyGender, "Niceties_Settings_WearAny",
                    "Niceties_Settings_WearAnyTip", ref sim.wearAnyGender))
            {
                listing.CheckboxLabeled("Niceties_Settings_WearAnyGenerated".Translate(),
                    ref sim.wearAnyGenderGenerated, "Niceties_Settings_WearAnyGeneratedTip".Translate());
            }

            if (DrawFeature(listing, NicetiesFeature.HideCryptosleep, "Niceties_Settings_HideCrypto",
                    "Niceties_Settings_HideCryptoTip", ref sim.hideCryptosleep))
            {
                listing.CheckboxLabeled("Niceties_Settings_CryptoCount".Translate(),
                    ref Settings.ui.showCryptosleepCount, "Niceties_Settings_CryptoCountTip".Translate());
                if (Settings.ui.showCryptosleepCount)
                {
                    listing.CheckboxLabeled("Niceties_Settings_CryptoRoster".Translate(),
                        ref Settings.ui.cryptosleepRoster, "Niceties_Settings_CryptoRosterTip".Translate());
                }

                if (ModsConfig.BiotechActive)
                {
                    listing.CheckboxLabeled("Niceties_Settings_HideVats".Translate(),
                        ref Settings.ui.hideGrowthVats, "Niceties_Settings_HideVatsTip".Translate());
                }

                if (ModsConfig.IdeologyActive || ModsConfig.BiotechActive)
                {
                    listing.CheckboxLabeled("Niceties_Settings_HidePods".Translate(),
                        ref Settings.ui.hidePods, "Niceties_Settings_HidePodsTip".Translate());
                }
            }

            if (DrawFeature(listing, NicetiesFeature.MeleeHunting, "Niceties_Settings_MeleeHunt",
                    "Niceties_Settings_MeleeHuntTip", ref sim.meleeHunting))
            {
                DrawMeleeKnobs(listing, ref sim);
            }

            if (DrawFeature(listing, NicetiesFeature.SharedRooms, "Niceties_Settings_SharedRooms",
                    "Niceties_Settings_SharedRoomsTip", ref sim.enableSharedRooms))
            {
                listing.CheckboxLabeled("Niceties_Settings_SkipDisturbedSleep".Translate(),
                    ref sim.skipDisturbedSleepWhenSharing, "Niceties_Settings_SkipDisturbedSleepTip".Translate());
                if (ModsConfig.RoyaltyActive)
                {
                    listing.CheckboxLabeled("Niceties_Settings_NobleAlert".Translate(),
                        ref Settings.ui.sharedRoomNobleAlert, "Niceties_Settings_NobleAlertTip".Translate());
                }
            }

            if (DrawFeature(listing, NicetiesFeature.LeaveAWayOut, "Niceties_Settings_LeaveAWayOut",
                    "Niceties_Settings_LeaveAWayOutTip", ref sim.enableLeaveAWayOut))
            {
                listing.CheckboxLabeled("Niceties_Settings_EncloseOverlay".Translate(),
                    ref Settings.ui.showEncloseOverlay, "Niceties_Settings_EncloseOverlayTip".Translate());
                listing.Label("Niceties_Settings_MaxRoom".Translate(sim.leaveAWayOutMaxRoom),
                    tooltip: "Niceties_Settings_MaxRoomTip".Translate());
                sim.leaveAWayOutMaxRoom = Mathf.RoundToInt(listing.Slider(sim.leaveAWayOutMaxRoom,
                    NicetiesLimits.MinMaxRoom, NicetiesLimits.MaxMaxRoom));
            }

            listing.GapLine();
            if (listing.ButtonText("Niceties_Settings_Reset".Translate()))
            {
                Settings.ResetToDefaults();
            }

            settingsContentHeight = Mathf.Max(listing.CurHeight + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            Settings.sim.Clamp();
            if (Settings.sim != before || Settings.ui != beforeUi)
            {
                Settings.Write();
                NicetiesSim.SyncFromModSettings();
                OnSettingsChanged();
            }
        }

        private static void DrawPresets(Listing_Standard listing)
        {
            Rect presetRow = listing.GetRect(28f);
            float bw = (presetRow.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(presetRow.x, presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetSoft".Translate()))
            {
                Settings.ApplyPreset(NicetiesPreset.Soft);
            }

            if (Widgets.ButtonText(new Rect(presetRow.x + bw + 8f, presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetDefault".Translate()))
            {
                Settings.ApplyPreset(NicetiesPreset.Default);
            }

            if (Widgets.ButtonText(new Rect(presetRow.x + 2f * (bw + 8f), presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetHard".Translate()))
            {
                Settings.ApplyPreset(NicetiesPreset.Hard);
            }

            listing.Gap(4f);
            listing.Label("Niceties_Settings_ActivePreset".Translate(Settings.ActivePresetLabelKey().Translate()));
            listing.Label("Niceties_Settings_PresetsTip".Translate());
        }

        private static void DrawMeleeKnobs(Listing_Standard listing, ref NicetiesSimFlags sim)
        {
            listing.CheckboxLabeled("Niceties_Settings_UnarmedHunt".Translate(),
                ref sim.unarmedHunting, "Niceties_Settings_UnarmedHuntTip".Translate());
            listing.Label("Niceties_Settings_MeleeSize".Translate(sim.meleeHuntMaxBodySize.ToString("F1")));
            sim.meleeHuntMaxBodySize = listing.Slider(sim.meleeHuntMaxBodySize,
                NicetiesLimits.MinMeleeCap, NicetiesLimits.MaxMeleeCap);
            string? example = MeleeHunt.ReferenceAnimalLabel(sim.meleeHuntMaxBodySize);
            listing.Label(example != null
                ? "Niceties_Settings_MeleeSizeExample".Translate(example)
                : "Niceties_Settings_MeleeSizeTiny".Translate());

            Rect row = listing.GetRect(26f);
            float x = row.x;
            for (int i = 0; i < CapButtons.Length; i++)
            {
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(CapButtons[i]);
                if (def?.race == null)
                {
                    continue;
                }

                string label = def.LabelCap;
                float w = Mathf.Max(70f, Text.CalcSize(label).x + 16f);
                if (x + w > row.xMax)
                {
                    break;
                }

                if (Widgets.ButtonText(new Rect(x, row.y, w, 24f), label))
                {
                    sim.meleeHuntMaxBodySize = def.race.baseBodySize;
                }

                TooltipHandler.TipRegion(new Rect(x, row.y, w, 24f),
                    "Niceties_Settings_MeleeCapButtonTip".Translate(label, def.race.baseBodySize.ToString("0.##")));
                x += w + 6f;
            }

            listing.Gap(4f);
            string revenge = sim.meleeHuntMaxRevengeChance >= 0.999f
                ? "Niceties_Settings_RevengeOff".Translate().ToString()
                : sim.meleeHuntMaxRevengeChance.ToStringPercent();
            listing.Label("Niceties_Settings_Revenge".Translate(revenge),
                tooltip: "Niceties_Settings_RevengeTip".Translate());
            sim.meleeHuntMaxRevengeChance = Mathf.Round(listing.Slider(sim.meleeHuntMaxRevengeChance, 0f, 1f) * 20f) / 20f;
        }

        /// <summary>
        /// Section header: collapse arrow, master toggle, a per-feature reset (AZR-656), and a
        /// status line when the nicety cannot run here (AZR-533). Returns true when the nested
        /// knobs should be drawn.
        /// </summary>
        private static bool DrawFeature(Listing_Standard listing, NicetiesFeature feature, string labelKey,
            string tipKey, ref bool enabled)
        {
            listing.GapLine();
            int index = (int)feature;
            Rect row = listing.GetRect(Text.LineHeight + 4f);
            Rect arrow = new Rect(row.x, row.y + 2f, 18f, 18f);
            if (Widgets.ButtonImage(arrow, Expanded[index] ? TexButton.Collapse : TexButton.Reveal))
            {
                Expanded[index] = !Expanded[index];
            }

            const float resetWidth = 90f;
            Rect reset = new Rect(row.xMax - resetWidth, row.y, resetWidth, row.height);
            if (Widgets.ButtonText(reset, "Niceties_Settings_ResetOne".Translate()))
            {
                Settings.ResetFeature(feature);
            }

            TooltipHandler.TipRegion(reset, "Niceties_Settings_ResetOneTip".Translate(labelKey.Translate()));
            Rect check = new Rect(arrow.xMax + 4f, row.y, reset.x - arrow.xMax - 12f, row.height);
            Widgets.CheckboxLabeled(check, labelKey.Translate(), ref enabled);
            TooltipHandler.TipRegion(check, tipKey.Translate());

            string? status = StatusFor(feature);
            if (status != null)
            {
                GameFont font = Text.Font;
                Color color = GUI.color;
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.75f, 0.45f);
                listing.Label(status);
                GUI.color = color;
                Text.Font = font;
            }

            return enabled && Expanded[index];
        }

        private static string? StatusFor(NicetiesFeature feature)
        {
            switch (feature)
            {
                case NicetiesFeature.ThroneAltar when !ThroneAltar.DlcsActive:
                    return "Niceties_Status_NeedsRoyaltyIdeology".Translate();
                case NicetiesFeature.LeaveAWayOut when OpenBuilds.SmarterConstructionLoaded:
                    return "Niceties_Status_SmarterConstruction".Translate();
                case NicetiesFeature.HideCryptosleep when PatchTargets.Unavailable(PatchTargets.HideCryptosleep):
                    return "Niceties_Status_Unavailable".Translate();
                case NicetiesFeature.SharedRooms when PatchTargets.Unavailable(PatchTargets.RoommateSleep):
                    return "Niceties_Status_RoommateSleepUnavailable".Translate();
            }

            if (Current.Game != null && !NicetiesSim.CanApplyModSettingsNow)
            {
                return "Niceties_Status_HostBaked".Translate();
            }

            return null;
        }

        private static void DrawSaveSnapshotNotice(Listing_Standard listing)
        {
            GameComponent_NicetiesSim? gc = GameComponent_NicetiesSim.Get();
            if (gc == null || !gc.DiffersFromModSettings())
            {
                return;
            }

            listing.Gap(4f);
            if (!NicetiesSim.CanApplyModSettingsNow)
            {
                listing.Label("Niceties_Settings_SnapshotHost".Translate());
                return;
            }

            listing.Label("Niceties_Settings_SnapshotDiffers".Translate());
            if (listing.ButtonText("Niceties_Settings_SnapshotApply".Translate()))
            {
                gc.PullFromModSettings();
                OnSettingsChanged();
            }
        }

        private static void OnSettingsChanged()
        {
            CryptosleepBar.MarkDirty();
            OpenBuilds.ClearCache();
        }
    }
}
