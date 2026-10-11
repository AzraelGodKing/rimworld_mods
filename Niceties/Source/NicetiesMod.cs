using UnityEngine;
using Verse;

namespace Niceties
{
    public class NicetiesMod : Mod
    {
        public static NicetiesSettings Settings;
        private Vector2 settingsScroll;
        private float settingsContentHeight = 2000f;

        public NicetiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NicetiesSettings>();
        }

        public override string SettingsCategory() => "Niceties_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
            {
                return;
            }

            int before = Settings.ContentFingerprint();

            Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));

            listing.Label("Niceties_Settings_Intro".Translate());
            listing.Gap(6f);

            Rect presetRow = listing.GetRect(28f);
            float bw = (presetRow.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(presetRow.x, presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetSoft".Translate()))
            {
                Settings.ApplySoft();
            }
            if (Widgets.ButtonText(new Rect(presetRow.x + bw + 8f, presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetDefault".Translate()))
            {
                Settings.ResetToDefaults();
            }
            if (Widgets.ButtonText(new Rect(presetRow.x + 2f * (bw + 8f), presetRow.y, bw, 28f),
                    "Niceties_Settings_PresetHard".Translate()))
            {
                Settings.ApplyHard();
            }

            listing.Gap(4f);
            listing.Label("Niceties_Settings_ActivePreset".Translate(Settings.ActivePresetLabelKey().Translate()));
            listing.Label("Niceties_Settings_PresetsTip".Translate());
            DrawSaveSnapshotNotice(listing);

            DrawFeature(listing, "Niceties_Settings_ApparelCare", "Niceties_Settings_ApparelCareTip",
                ref Settings.enableApparelCare);
            if (Settings.enableApparelCare)
            {
                listing.CheckboxLabeled("Niceties_Settings_QualityScale".Translate(),
                    ref Settings.apparelQualityScaling, "Niceties_Settings_QualityScaleTip".Translate());
                listing.CheckboxLabeled("Niceties_Settings_CraftingBonus".Translate(),
                    ref Settings.apparelCraftingBonus, "Niceties_Settings_CraftingBonusTip".Translate());
                listing.CheckboxLabeled("Niceties_Settings_CorpseApparel".Translate(),
                    ref Settings.protectCorpseApparel, "Niceties_Settings_CorpseApparelTip".Translate());
                listing.CheckboxLabeled("Niceties_Settings_WeaponCare".Translate(),
                    ref Settings.enableWeaponCare, "Niceties_Settings_WeaponCareTip".Translate());
            }

            DrawFeature(listing, "Niceties_Settings_ThroneAltar", "Niceties_Settings_ThroneAltarTip",
                ref Settings.allowThroneAltars);

            DrawFeature(listing, "Niceties_Settings_WearAny", "Niceties_Settings_WearAnyTip",
                ref Settings.wearAnyGender);

            DrawFeature(listing, "Niceties_Settings_HideCrypto", "Niceties_Settings_HideCryptoTip",
                ref Settings.hideCryptosleep);
            if (Settings.hideCryptosleep)
            {
                listing.CheckboxLabeled("Niceties_Settings_CryptoCount".Translate(),
                    ref Settings.showCryptosleepCount, "Niceties_Settings_CryptoCountTip".Translate());
            }

            DrawFeature(listing, "Niceties_Settings_MeleeHunt", "Niceties_Settings_MeleeHuntTip",
                ref Settings.meleeHunting);
            if (Settings.meleeHunting)
            {
                listing.CheckboxLabeled("Niceties_Settings_UnarmedHunt".Translate(),
                    ref Settings.unarmedHunting, "Niceties_Settings_UnarmedHuntTip".Translate());
                listing.Label("Niceties_Settings_MeleeSize".Translate(
                    Settings.meleeHuntMaxBodySize.ToString("F1")));
                Settings.meleeHuntMaxBodySize = listing.Slider(Settings.meleeHuntMaxBodySize, 0.2f, 8f);
                string example = MeleeHunt.ReferenceAnimalLabel(Settings.meleeHuntMaxBodySize);
                listing.Label(example != null
                    ? "Niceties_Settings_MeleeSizeExample".Translate(example)
                    : "Niceties_Settings_MeleeSizeTiny".Translate());
            }

            DrawFeature(listing, "Niceties_Settings_SharedRooms", "Niceties_Settings_SharedRoomsTip",
                ref Settings.enableSharedRooms);
            if (Settings.enableSharedRooms)
            {
                listing.CheckboxLabeled("Niceties_Settings_SkipDisturbedSleep".Translate(),
                    ref Settings.skipDisturbedSleepWhenSharing,
                    "Niceties_Settings_SkipDisturbedSleepTip".Translate());
            }

            DrawFeature(listing, "Niceties_Settings_LeaveAWayOut", "Niceties_Settings_LeaveAWayOutTip",
                ref Settings.enableLeaveAWayOut);

            listing.GapLine();
            if (listing.ButtonText("Niceties_Settings_Reset".Translate()))
            {
                Settings.ResetToDefaults();
                Settings.showCryptosleepCount = true;
            }

            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            Settings.Clamp();
            if (Settings.ContentFingerprint() != before)
            {
                Settings.Write();
                NicetiesSim.SyncFromModSettings();
                OnFeatureTogglesChanged();
            }
        }

        private static void DrawSaveSnapshotNotice(Listing_Standard listing)
        {
            GameComponent_NicetiesSim gc = GameComponent_NicetiesSim.Get();
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
                OnFeatureTogglesChanged();
            }
        }

        private static void DrawFeature(Listing_Standard listing, string labelKey, string tipKey,
            ref bool enabled)
        {
            listing.GapLine();
            listing.CheckboxLabeled(labelKey.Translate(), ref enabled, tipKey.Translate());
        }

        private static void OnFeatureTogglesChanged()
        {
            ApparelGender.ApplyEffective();
            CryptosleepBar.MarkDirty();
        }
    }
}
