using UnityEngine;
using Verse;

namespace Niceties
{
    public class NicetiesMod : Mod
    {
        public static NicetiesSettings Settings;
        private Vector2 settingsScroll;
        private float settingsContentHeight = 2000f;

        private bool foldPresets = true;
        private bool foldApparel = true;
        private bool foldThrone = true;
        private bool foldWearAny = true;
        private bool foldCrypto = true;
        private bool foldMelee = true;
        private bool foldShared = true;
        private bool foldLeaveWay = true;

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

            Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
            float viewHeight = Mathf.Max(settingsContentHeight, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            listing.Begin(new Rect(0f, 0f, viewRect.width, 99999f));

            listing.Label("Niceties_Settings_Intro".Translate());
            listing.Gap(6f);

            if (SectionHeader(listing, "Niceties_Settings_SectionPresets".Translate(), ref foldPresets))
            {
                Rect presetRow = listing.GetRect(28f);
                float bw = (presetRow.width - 16f) / 3f;
                if (Widgets.ButtonText(new Rect(presetRow.x, presetRow.y, bw, 28f),
                        "Niceties_Settings_PresetSoft".Translate()))
                {
                    Settings.ApplySoft();
                    OnFeatureTogglesChanged();
                }
                if (Widgets.ButtonText(new Rect(presetRow.x + bw + 8f, presetRow.y, bw, 28f),
                        "Niceties_Settings_PresetDefault".Translate()))
                {
                    Settings.ResetToDefaults();
                    OnFeatureTogglesChanged();
                }
                if (Widgets.ButtonText(new Rect(presetRow.x + 2f * (bw + 8f), presetRow.y, bw, 28f),
                        "Niceties_Settings_PresetHard".Translate()))
                {
                    Settings.ApplyHard();
                    OnFeatureTogglesChanged();
                }

                listing.Gap(4f);
                listing.Label("Niceties_Settings_PresetsTip".Translate());
            }

            if (SectionHeader(listing, "Niceties_Settings_ApparelCare".Translate(), ref foldApparel))
            {
                DrawFeatureBody(listing, "Niceties_Settings_ApparelCare", "Niceties_Settings_ApparelCareTip",
                    ref Settings.enableApparelCare, null);
                if (Settings.enableApparelCare)
                {
                    listing.CheckboxLabeled("Niceties_Settings_QualityScale".Translate(),
                        ref Settings.apparelQualityScaling, "Niceties_Settings_QualityScaleTip".Translate());
                    listing.CheckboxLabeled("Niceties_Settings_CraftingBonus".Translate(),
                        ref Settings.apparelCraftingBonus, "Niceties_Settings_CraftingBonusTip".Translate());
                    listing.CheckboxLabeled("Niceties_Settings_CorpseApparel".Translate(),
                        ref Settings.protectCorpseApparel, "Niceties_Settings_CorpseApparelTip".Translate());
                }
            }

            if (SectionHeader(listing, "Niceties_Settings_ThroneAltar".Translate(), ref foldThrone))
            {
                DrawFeatureBody(listing, "Niceties_Settings_ThroneAltar", "Niceties_Settings_ThroneAltarTip",
                    ref Settings.allowThroneAltars, null);
            }

            if (SectionHeader(listing, "Niceties_Settings_WearAny".Translate(), ref foldWearAny))
            {
                DrawFeatureBody(listing, "Niceties_Settings_WearAny", "Niceties_Settings_WearAnyTip",
                    ref Settings.wearAnyGender, () => ApparelGender.Apply(Settings.wearAnyGender));
            }

            if (SectionHeader(listing, "Niceties_Settings_HideCrypto".Translate(), ref foldCrypto))
            {
                DrawFeatureBody(listing, "Niceties_Settings_HideCrypto", "Niceties_Settings_HideCryptoTip",
                    ref Settings.hideCryptosleep, CryptosleepBar.MarkDirty);
            }

            if (SectionHeader(listing, "Niceties_Settings_MeleeHunt".Translate(), ref foldMelee))
            {
                DrawFeatureBody(listing, "Niceties_Settings_MeleeHunt", "Niceties_Settings_MeleeHuntTip",
                    ref Settings.meleeHunting, null);
                if (Settings.meleeHunting)
                {
                    listing.CheckboxLabeled("Niceties_Settings_UnarmedHunt".Translate(),
                        ref Settings.unarmedHunting, "Niceties_Settings_UnarmedHuntTip".Translate());
                    listing.Label("Niceties_Settings_MeleeSize".Translate(
                        Settings.meleeHuntMaxBodySize.ToString("F1")));
                    Settings.meleeHuntMaxBodySize = listing.Slider(Settings.meleeHuntMaxBodySize, 0.2f, 8f);
                }
            }

            if (SectionHeader(listing, "Niceties_Settings_SharedRooms".Translate(), ref foldShared))
            {
                DrawFeatureBody(listing, "Niceties_Settings_SharedRooms", "Niceties_Settings_SharedRoomsTip",
                    ref Settings.enableSharedRooms, null);
                if (Settings.enableSharedRooms)
                {
                    listing.CheckboxLabeled("Niceties_Settings_SkipDisturbedSleep".Translate(),
                        ref Settings.skipDisturbedSleepWhenSharing,
                        "Niceties_Settings_SkipDisturbedSleepTip".Translate());
                    listing.CheckboxLabeled("Niceties_Settings_SkipNonPartnerBed".Translate(),
                        ref Settings.skipNonPartnerBedShareMood,
                        "Niceties_Settings_SkipNonPartnerBedTip".Translate());
                }
            }

            if (SectionHeader(listing, "Niceties_Settings_LeaveAWayOut".Translate(), ref foldLeaveWay))
            {
                DrawFeatureBody(listing, "Niceties_Settings_LeaveAWayOut", "Niceties_Settings_LeaveAWayOutTip",
                    ref Settings.enableLeaveAWayOut, null);
            }

            listing.GapLine();
            if (listing.ButtonText("Niceties_Settings_Reset".Translate()))
            {
                Settings.ResetToDefaults();
                OnFeatureTogglesChanged();
            }

            settingsContentHeight = Mathf.Max(listing.MaxColumnHeightSeen + 24f, inRect.height);
            listing.End();
            Widgets.EndScrollView();
            Settings.Clamp();
            Settings.Write();
        }

        private static bool SectionHeader(Listing_Standard listing, string title, ref bool open)
        {
            listing.GapLine();
            Rect row = listing.GetRect(24f);
            string mark = open ? "▼ " : "▶ ";
            if (Widgets.ButtonText(row, mark + title, drawBackground: false, doMouseoverSound: true))
            {
                open = !open;
            }
            return open;
        }

        private static void DrawFeatureBody(Listing_Standard listing, string labelKey, string tipKey,
            ref bool enabled, System.Action onChanged)
        {
            bool was = enabled;
            listing.CheckboxLabeled(labelKey.Translate(), ref enabled, tipKey.Translate());
            if (onChanged != null && was != enabled)
            {
                onChanged();
            }
        }

        private static void OnFeatureTogglesChanged()
        {
            ApparelGender.Apply(Settings.wearAnyGender);
            CryptosleepBar.MarkDirty();
        }
    }
}
