using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class MainTabWindow_HomesteaderPantry : MainTabWindow
    {
        private Vector2 scrollPos;

        public override Vector2 RequestedTabSize => new Vector2(540f, 460f);

        public override void DoWindowContents(Rect inRect)
        {
            PantryReport report = PantryUtility.Snapshot();

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Homesteader_PantryTitle".Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + 36f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Homesteader_PantryKinds".Translate(report.preserveKinds));
            y += 22f;

            string days = report.colonistCount <= 0
                ? "—"
                : report.daysOfFood.ToString("F1");
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Homesteader_PantryDays".Translate(days, report.colonistCount));
            y += 22f;

            if (report.nearestRot != null && report.nearestRotDays >= 0f)
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                    "Homesteader_PantryNextRot".Translate(
                        report.nearestRot.LabelCap,
                        report.nearestRotDays.ToString("F1")));
            }
            else
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                    "Homesteader_PantryNextRotNone".Translate());
            }

            y += 28f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                "Homesteader_PantryContents".Translate());
            y += 24f;

            Rect view = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            if (report.kinds.Count == 0)
            {
                Widgets.Label(view, "Homesteader_PantryEmpty".Translate());
                return;
            }

            float innerH = report.kinds.Count * 22f;
            Rect inner = new Rect(0f, 0f, view.width - 16f, innerH);
            Widgets.BeginScrollView(view, ref scrollPos, inner);
            float rowY = 0f;
            for (int i = 0; i < report.kinds.Count; i++)
            {
                PantryReport.KindRow row = report.kinds[i];
                if (row.def == null)
                {
                    continue;
                }

                Widgets.Label(new Rect(0f, rowY, inner.width, 22f),
                    row.def.LabelCap + "  ×" + row.count);
                rowY += 22f;
            }

            Widgets.EndScrollView();
        }
    }
}
