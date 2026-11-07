#nullable enable
using System.Collections.Generic;
using AzraelCommon;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DateNight
{
    public class DateNightMod : Mod
    {
        // Set in the Mod constructor, before any def or game code runs.
        public static DateNightSettings Settings { get; private set; } = null!;

        public DateNightMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DateNightSettings>();
            ModVersionLog.Write("[DateNight]", content, extra: "datenight2-net10");
            // PatchAll runs after defs load — Harmony compiling TimeAssignmentSelector
            // patches otherwise touches TimeAssignmentDefOf before DefOfs exist.
        }

        public override string SettingsCategory() => "DateNight_SettingsCategory".Translate();

        private static Vector2 settingsScroll;
        private static float settingsHeight = 1200f;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Scrolls so long translations (Russian is ~40% longer) still fit at 1280x720.
            Rect view = new Rect(0f, 0f, inRect.width - 16f, settingsHeight);
            Widgets.BeginScrollView(inRect, ref settingsScroll, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            listing.Label("DateNight_Settings_Intro".Translate());

            Section(listing, "DateNight_Settings_Section_Lovin");
            Check(listing, "PregnancySafe", ref Settings.pregnancySafeCooldown);
            if (Settings.pregnancySafeCooldown)
            {
                Settings.eagerCooldown = false;
            }
            Check(listing, "Eager", ref Settings.eagerCooldown);
            if (Settings.eagerCooldown)
            {
                Settings.pregnancySafeCooldown = false;
            }
            Check(listing, "SelfLovin", ref Settings.allowSelfLovin);
            Check(listing, "WindowBed", ref Settings.allowWindowBedClaim);
            Check(listing, "PostDateBoost", ref Settings.postDateLovinBoost);

            Section(listing, "DateNight_Settings_Section_Dates");
            Check(listing, "Activities", ref Settings.enableDateActivities);
            Check(listing, "Quality", ref Settings.enableDateQuality);
            Check(listing, "FavoriteSpot", ref Settings.rememberFavoriteSpot);
            Check(listing, "DoubleDates", ref Settings.allowDoubleDates);

            Section(listing, "DateNight_Settings_Section_Gifts");
            Check(listing, "Gifts", ref Settings.allowGiftDates);
            if (Settings.allowGiftDates)
            {
                Check(listing, "GiftExtend", ref Settings.extendGiftsWithJoyItems);
                DoGiftList(listing);
            }

            Section(listing, "DateNight_Settings_Section_Romance");
            Check(listing, "Anniversaries", ref Settings.enableAnniversaries);
            Check(listing, "GradualRomance", ref Settings.enableGradualRomance);
            Check(listing, "RomanceGizmos", ref Settings.showRomanceGizmos);

            Section(listing, "DateNight_Settings_Section_Schedule");
            DoColumnOffset(listing);

            listing.Gap(12f);
            if (listing.ButtonText("DateNight_Settings_Reset".Translate(), null, 0.25f))
            {
                Settings.ResetToDefaults();
            }

            settingsHeight = listing.CurHeight + 12f;
            listing.End();
            Widgets.EndScrollView();
        }

        private static void Section(Listing_Standard listing, string key)
        {
            listing.Gap(10f);
            Text.Font = GameFont.Medium;
            listing.Label(key.Translate());
            Text.Font = GameFont.Small;
            listing.GapLine(4f);
        }

        private static void Check(Listing_Standard listing, string name, ref bool value)
        {
            listing.CheckboxLabeled(
                ("DateNight_Settings_" + name).Translate(),
                ref value,
                ("DateNight_Settings_" + name + "Tip").Translate());
            listing.Gap(4f);
        }

        /// <summary>
        /// Gift list as rows with a remove button; names that match no ThingDef
        /// (typo, mod removed) are shown in red instead of being dropped silently.
        /// </summary>
        private static void DoGiftList(Listing_Standard listing)
        {
            listing.Label("DateNight_Settings_GiftList".Translate());
            List<string> names = Settings.giftDefNames;
            for (int i = 0; i < names.Count; i++)
            {
                Rect row = listing.GetRect(26f);
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(names[i]);
                Rect remove = new Rect(row.xMax - 26f, row.y, 26f, 26f);
                Rect label = new Rect(row.x + 12f, row.y + 2f, row.width - 44f, row.height);
                if (def == null)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    Widgets.Label(label, "DateNight_Settings_GiftUnknown".Translate(names[i]));
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.Label(label, def.LabelCap + "  (" + def.defName + ")");
                }
                TooltipHandler.TipRegion(remove, "DateNight_Settings_GiftRemove".Translate());
                if (Widgets.ButtonImage(remove, TexButton.Delete))
                {
                    names.RemoveAt(i);
                    DateNightActivities.InvalidateGiftCache();
                    break;
                }
            }
            if (listing.ButtonText("DateNight_Settings_GiftAdd".Translate(), null, 0.35f))
            {
                Find.WindowStack.Add(new Dialog_DateNightGiftPicker(def =>
                {
                    if (!Settings.giftDefNames.Contains(def.defName))
                    {
                        Settings.giftDefNames.Add(def.defName);
                        DateNightActivities.InvalidateGiftCache();
                    }
                }));
            }
        }

        private static void DoColumnOffset(Listing_Standard listing)
        {
            Rect row = listing.GetRect(28f);
            TooltipHandler.TipRegion(row, "DateNight_Settings_ColumnOffsetTip".Translate());
            Widgets.Label(new Rect(row.x, row.y + 3f, row.width - 120f, row.height),
                "DateNight_Settings_ColumnOffset".Translate(Settings.scheduleColumnOffset));
            if (Widgets.ButtonText(new Rect(row.xMax - 116f, row.y, 56f, row.height), "-"))
            {
                Settings.scheduleColumnOffset = Mathf.Max(-4, Settings.scheduleColumnOffset - 1);
            }
            if (Widgets.ButtonText(new Rect(row.xMax - 56f, row.y, 56f, row.height), "+"))
            {
                Settings.scheduleColumnOffset = Mathf.Min(8, Settings.scheduleColumnOffset + 1);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class DateNightInit
    {
        public const string HarmonyId = "azraelgodking.datenight";

        static DateNightInit()
        {
            SafePatchAll.Apply(new Harmony(HarmonyId), "[DateNight]");
            _ = DateNightRjwSoftCompat.Active;
            _ = DateNightRotrSoftCompat.Active;
            DateNightPatchCheck.Verify(HarmonyId);
        }
    }
}
