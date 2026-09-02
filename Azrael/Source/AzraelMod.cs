using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Azrael
{
    public class AzraelMod : Mod
    {
        private Vector2 scrollPos;
        private string copiedNotice;
        private float copiedUntil;

        public AzraelMod(ModContentPack content) : base(content)
        {
            ModVersionLog.Write("[Azrael]", content, "azr-86-v1");
        }

        public override string SettingsCategory() => "Azrael_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<SeriesHub.ModRow> mods = SeriesHub.Mods();
            List<SeriesHub.BridgeRow> bridges = SeriesHub.Bridges();
            List<SeriesHub.ConflictRow> conflicts = SeriesHub.Conflicts();
            List<string> fails = SeriesHub.HarmonyFailures();

            float height = 280f + (mods.Count + bridges.Count + Mathf.Max(1, conflicts.Count) + Mathf.Min(fails.Count, 8) + 1) * 26f;
            Rect view = new Rect(0f, 0f, inRect.width - 20f, height);
            Widgets.BeginScrollView(inRect, ref scrollPos, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            listing.Label("Azrael_Hub_Intro".Translate());
            listing.Gap(6f);

            Rect copyRect = listing.GetRect(28f);
            Rect button = new Rect(copyRect.x, copyRect.y, 280f, copyRect.height);
            if (Widgets.ButtonText(button, "Azrael_Hub_Copy".Translate()))
            {
                GUIUtility.systemCopyBuffer = SeriesHub.ClipboardReport();
                copiedNotice = "Azrael_Hub_Copied".Translate();
                copiedUntil = Time.realtimeSinceStartup + 2.5f;
            }
            if (!string.IsNullOrEmpty(copiedNotice) && Time.realtimeSinceStartup < copiedUntil)
            {
                Widgets.Label(new Rect(copyRect.x + 290f, copyRect.y, copyRect.width - 290f, copyRect.height), copiedNotice);
            }

            listing.GapLine();
            listing.Label("Azrael_Hub_Mods".Translate().CapitalizeFirst());
            listing.Gap(4f);
            foreach (SeriesHub.ModRow row in mods)
            {
                GUI.color = SeriesHub.StatusColor(row.Loaded);
                string status = row.Loaded
                    ? "Azrael_Hub_Loaded".Translate()
                    : "Azrael_Hub_NotLoaded".Translate();
                listing.Label(row.Display + "  —  " + status + (row.Loaded ? "  v" + row.Version : ""));
                GUI.color = Color.white;
            }

            listing.GapLine();
            listing.Label("Azrael_Hub_Bridges".Translate().CapitalizeFirst());
            listing.Gap(4f);
            foreach (SeriesHub.BridgeRow row in bridges)
            {
                GUI.color = row.Live ? SeriesHub.StatusColor(true) : SeriesHub.WaitingColor;
                listing.Label(row.LabelKey.Translate() + "  —  " + row.Status);
                GUI.color = Color.white;
            }

            listing.GapLine();
            listing.Label("Azrael_Hub_Conflicts".Translate().CapitalizeFirst());
            listing.Gap(4f);
            if (conflicts.Count == 0)
            {
                GUI.color = SeriesHub.StatusColor(true);
                listing.Label("Azrael_Hub_ConflictsNone".Translate());
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = SeriesHub.ConflictColor;
                foreach (SeriesHub.ConflictRow row in conflicts)
                {
                    listing.Label(row.Name + "  —  " + row.Reason);
                }
                GUI.color = Color.white;
            }

            listing.GapLine();
            listing.Label("Azrael_Hub_Harmony".Translate().CapitalizeFirst());
            listing.Gap(4f);
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

            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
