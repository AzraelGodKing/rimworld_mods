#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AzraelCommon;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Azrael
{
    public class AzraelMod : Mod
    {
        private const float RowHeight = 26f;
        private const string ReportFileName = "Azrael hub report.txt";

        internal static AzraelSettings? Settings { get; private set; }

        private Vector2 scrollPos;
        private string? notice;
        private float noticeUntil;
        // Height the page used last frame; the scroll view sizes to it (AZR-687).
        private float lastHeight = 1200f;
        private int lastDrawFrame = -10;

        public AzraelMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AzraelSettings>();
            ModVersionLog.Write("[Azrael]", content, AzraelBuildInfo.BuildStamp);
            AzraelCommon.SafePatchAll.Apply(new Harmony("azraelgodking.azrael"), "[Azrael]");
        }

        public override string SettingsCategory() => "Azrael_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // A gap in drawn frames means the page was just (re)opened: rescan (AZR-602, AZR-725).
            if (Time.frameCount - lastDrawFrame > 1)
            {
                SeriesHub.InvalidateHub();
            }
            lastDrawFrame = Time.frameCount;

            List<SeriesHub.ModRow> mods = SeriesHub.Mods();
            List<SeriesHub.BridgeRow> bridges = SeriesHub.Bridges();
            List<SeriesHub.ConflictRow> conflicts = SeriesHub.Conflicts();
            List<SeriesHub.DlcRow> dlc = SeriesHub.Dlc();
            List<string> fails = SeriesHub.HarmonyFailures();

            Rect view = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(lastHeight, inRect.height));
            Widgets.BeginScrollView(inRect, ref scrollPos, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            listing.Label("Azrael_Hub_Intro".Translate());
            listing.Gap(6f);
            DrawReportButtons(listing);

            if (Settings != null)
            {
                listing.Gap(4f);
                listing.CheckboxLabeled("Azrael_Settings_SeriesWeights".Translate(),
                    ref Settings.seriesIncidentWeights, "Azrael_Settings_SeriesWeightsTip".Translate());
            }

            Section(listing, "Azrael_Hub_Mods");
            listing.Label("Azrael_Hub_Runtime".Translate(SeriesHub.RuntimeDescription()));
            foreach (SeriesHub.ModRow row in mods)
            {
                GUI.color = SeriesHub.StatusColor(row.Loaded);
                string status = row.Loaded
                    ? "Azrael_Hub_Loaded".Translate()
                    : "Azrael_Hub_NotLoaded".Translate();
                listing.Label(row.Display + "  —  " + status + (row.Loaded ? "  v" + row.Version : "")
                    + (row.Loaded && !string.IsNullOrEmpty(row.Stamp) ? "  [" + row.Stamp + "]" : "")
                    + (row.Loaded && !string.IsNullOrEmpty(row.Folder) ? "  " + "Azrael_Hub_Folder".Translate(row.Folder) : ""));
                if (row.Mismatch != null)
                {
                    GUI.color = SeriesHub.WaitingColor;
                    listing.Label("    " + row.Mismatch);
                }
                GUI.color = Color.white;
            }

            Section(listing, "Azrael_Hub_Dlc");
            foreach (SeriesHub.DlcRow row in dlc)
            {
                GUI.color = SeriesHub.StatusColor(row.Loaded);
                string status = row.Loaded
                    ? "Azrael_Hub_Loaded".Translate()
                    : "Azrael_Hub_NotLoaded".Translate();
                listing.Label(row.Name + "  —  " + status);
                GUI.color = Color.white;
            }

            Section(listing, "Azrael_Hub_Bridges");
            listing.Label(SeriesHub.BridgeSummary(bridges));
            foreach (SeriesHub.BridgeRow row in bridges)
            {
                GUI.color = row.Live
                    ? SeriesHub.StatusColor(true)
                    : (row.Degraded ? SeriesHub.ConflictColor : SeriesHub.WaitingColor);
                listing.Label(row.LabelKey.Translate() + "  —  " + row.Status);
                GUI.color = Color.white;
            }

            Section(listing, "Azrael_Hub_Conflicts");
            if (conflicts.Count == 0)
            {
                GUI.color = SeriesHub.StatusColor(true);
                listing.Label("Azrael_Hub_ConflictsNone".Translate());
            }
            else
            {
                GUI.color = SeriesHub.ConflictColor;
                foreach (SeriesHub.ConflictRow row in conflicts)
                {
                    listing.Label(row.Name + "  —  " + row.Reason);
                }
            }
            GUI.color = Color.white;

            Section(listing, "Azrael_Hub_Harmony");
            if (fails.Count == 0)
            {
                listing.Label("Azrael_Hub_HarmonyNone".Translate());
            }
            else
            {
                GUI.color = SeriesHub.ConflictColor;
                listing.Label("Azrael_Hub_HarmonyCount".Translate(fails.Count));
                int shown = Mathf.Min(fails.Count, 8);
                for (int i = 0; i < shown; i++)
                {
                    listing.Label(fails[i]);
                }
                GUI.color = Color.white;
            }

            Section(listing, "Azrael_Removal_Section");
            listing.Label("Azrael_Removal_SectionHint".Translate());
            foreach (SeriesHub.ModRow row in mods)
            {
                if (!row.Loaded || string.Equals(row.PackageId, SeriesHub.AzraelPackageId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                Rect r = listing.GetRect(RowHeight);
                if (Widgets.ButtonText(new Rect(r.x, r.y, Mathf.Min(420f, r.width), 24f),
                        "Azrael_Removal_Prep".Translate(row.Display)))
                {
                    Find.WindowStack.Add(new Dialog_RemovalPrep(SeriesRemoval.Scan(row.Display, row.PackageId)));
                }
            }

            lastHeight = listing.CurHeight + 12f;
            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        private static void Section(Listing_Standard listing, string headerKey)
        {
            listing.GapLine();
            listing.Label(headerKey.Translate().CapitalizeFirst());
            listing.Gap(4f);
        }

        private void DrawReportButtons(Listing_Standard listing)
        {
            Rect rect = listing.GetRect(28f);
            Rect copy = new Rect(rect.x, rect.y, 220f, rect.height);
            Rect save = new Rect(copy.xMax + 8f, rect.y, 220f, rect.height);
            if (Widgets.ButtonText(copy, "Azrael_Hub_Copy".Translate()))
            {
                string report = SeriesHub.ClipboardReport();
                try
                {
                    GUIUtility.systemCopyBuffer = report;
                    Notify("Azrael_Hub_Copied".Translate());
                }
                catch (Exception e)
                {
                    // Clipboard unavailable (some Linux / Steam Deck setups): fall back to the file.
                    Log.Warning("[Azrael] Clipboard copy failed: " + e.Message);
                    Notify(WriteReport(report));
                }
            }
            if (Widgets.ButtonText(save, "Azrael_Hub_SaveReport".Translate()))
            {
                Notify(WriteReport(SeriesHub.ClipboardReport()));
            }
            if (!string.IsNullOrEmpty(notice) && Time.realtimeSinceStartup < noticeUntil)
            {
                Widgets.Label(new Rect(save.xMax + 10f, rect.y, rect.xMax - save.xMax - 10f, rect.height), notice);
            }
        }

        /// <summary>Writes the report beside the save folder (AZR-689) and returns the notice to show.</summary>
        private static string WriteReport(string report)
        {
            try
            {
                string path = Path.Combine(GenFilePaths.SaveDataFolderPath, ReportFileName);
                File.WriteAllText(path, report);
                Log.Message("[Azrael] Hub report written to " + path);
                return "Azrael_Hub_Saved".Translate(path);
            }
            catch (Exception e)
            {
                Log.Warning("[Azrael] Could not write the hub report: " + e.Message);
                return "Azrael_Hub_SaveFailed".Translate(e.Message);
            }
        }

        private void Notify(string text)
        {
            notice = text;
            noticeUntil = Time.realtimeSinceStartup + 4f;
        }
    }
}
