using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class MainTabWindow_HomesteaderPantry : MainTabWindow
    {
        private Vector2 stockScroll;
        private Vector2 makeScroll;
        private int page;
        private PantrySortMode sortMode = PantrySortMode.Count;
        private PantryReport sortedReport;
        private PantrySortMode sortedMode;

        public override Vector2 RequestedTabSize => new Vector2(620f, 540f);

        public override void PreOpen()
        {
            base.PreOpen();
            PantryUtility.Invalidate();
            PantryMakeUtility.Invalidate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "Homesteader_PantryTitle".Translate());
            Text.Font = GameFont.Small;

            float y = inRect.y + 30f;
            Rect stockTab = new Rect(inRect.x, y, 120f, 28f);
            Rect makeTab = new Rect(inRect.x + 128f, y, 140f, 28f);
            if (page == 0)
            {
                Widgets.DrawHighlight(stockTab);
            }
            else
            {
                Widgets.DrawHighlight(makeTab);
            }

            if (Widgets.ButtonText(stockTab, "Homesteader_PantryTabStock".Translate()))
            {
                page = 0;
            }

            if (Widgets.ButtonText(makeTab, "Homesteader_PantryTabMake".Translate()))
            {
                page = 1;
            }

            y += 36f;
            Rect body = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            if (page == 0)
            {
                DrawStock(body);
            }
            else
            {
                DrawMake(body);
            }
        }

        private void DrawStock(Rect inRect)
        {
            PantryReport report = PantryUtility.Snapshot();
            float y = inRect.y;
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
                Rect rotLabel = new Rect(inRect.x, y, inRect.width - 88f, 22f);
                Rect rotBtn = new Rect(inRect.xMax - 84f, y, 80f, 22f);
                Widgets.Label(rotLabel,
                    "Homesteader_PantryNextRot".Translate(
                        report.nearestRot.LabelCap,
                        report.nearestRotDays.ToString("F1")));
                if (report.nearestRot.Spawned
                    && Widgets.ButtonText(rotBtn, "Homesteader_PantryJump".Translate()))
                {
                    CameraJumper.TryJumpAndSelect(report.nearestRot);
                }
            }
            else
            {
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                    "Homesteader_PantryNextRotNone".Translate());
            }

            y += 28f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width - 168f, 22f),
                "Homesteader_PantryContents".Translate());
            if (Widgets.ButtonText(new Rect(inRect.xMax - 164f, y, 160f, 22f), SortLabel(sortMode)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (PantrySortMode mode in new[] { PantrySortMode.Count, PantrySortMode.Name, PantrySortMode.RotSoonest })
                {
                    PantrySortMode chosen = mode;
                    options.Add(new FloatMenuOption(SortLabel(chosen), () => sortMode = chosen));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (report != sortedReport || sortMode != sortedMode)
            {
                PantryUtility.SortKinds(report, sortMode);
                sortedReport = report;
                sortedMode = sortMode;
            }

            y += 24f;

            Rect view = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            if (report.kinds.Count == 0)
            {
                Widgets.Label(view, "Homesteader_PantryEmpty".Translate());
                return;
            }

            float innerH = report.kinds.Count * 22f;
            Rect inner = new Rect(0f, 0f, view.width - 16f, innerH);
            Widgets.BeginScrollView(view, ref stockScroll, inner);
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

        private void DrawMake(Rect inRect)
        {
            PantryMakeReport report = PantryMakeUtility.Snapshot();
            float y = inRect.y;
            float innerH = 8f
                + SectionHeight("Homesteader_PantryMakeNow".Translate(), report.canMake.Count, true)
                + SectionHeight("Homesteader_PantryMakeShort".Translate(), report.oneShort.Count, false)
                + SectionHeight("Homesteader_PantryMakeLocked".Translate(), report.locked.Count, false);

            Rect view = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            Rect inner = new Rect(0f, 0f, view.width - 16f, innerH);
            Widgets.BeginScrollView(view, ref makeScroll, inner);
            float rowY = 0f;
            rowY = DrawSection(inner, rowY, "Homesteader_PantryMakeNow".Translate(), report.canMake, true);
            rowY = DrawSection(inner, rowY, "Homesteader_PantryMakeShort".Translate(), report.oneShort, false);
            DrawSection(inner, rowY, "Homesteader_PantryMakeLocked".Translate(), report.locked, false);
            Widgets.EndScrollView();
        }

        private static float SectionHeight(string header, int count, bool bill)
        {
            int n = count == 0 ? 1 : count;
            return 24f + n * (bill ? 28f : 22f) + 8f;
        }

        private static float DrawSection(
            Rect inner,
            float y,
            string header,
            System.Collections.Generic.List<PantryMakeRow> rows,
            bool bill)
        {
            Widgets.Label(new Rect(0f, y, inner.width, 22f), header);
            y += 24f;
            if (rows.Count == 0)
            {
                Widgets.Label(new Rect(8f, y, inner.width - 8f, 22f),
                    "Homesteader_PantryMakeNone".Translate());
                return y + 30f;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                PantryMakeRow row = rows[i];
                float h = bill ? 28f : 22f;
                Rect line = new Rect(8f, y, inner.width - 8f, h);
                string text = LineText(row);
                if (bill)
                {
                    Rect label = new Rect(line.x, line.y, line.width - 88f, h);
                    Rect btn = new Rect(line.xMax - 84f, line.y, 80f, 24f);
                    Widgets.Label(label, text);
                    List<Building_WorkTable> stations = row.stations;
                    if (stations.Count > 1)
                    {
                        TooltipHandler.TipRegion(btn, "Homesteader_PantryMakeChooseStation".Translate(stations.Count));
                    }

                    if (Widgets.ButtonText(btn, "Homesteader_PantryMakeBill".Translate()))
                    {
                        AddBillOrChoose(row.recipe, stations);
                    }
                }
                else
                {
                    Widgets.Label(line, text);
                }

                y += h;
            }

            return y + 8f;
        }

        private static void AddBillOrChoose(RecipeDef recipe, List<Building_WorkTable> stations)
        {
            stations = stations.FindAll(t => t != null && t.Spawned && t.billStack != null);
            if (stations.Count == 0)
            {
                Messages.Message(
                    "Homesteader_PantryMakeBillFail".Translate(),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            if (stations.Count == 1)
            {
                PantryMakeUtility.AddBillAt(recipe, stations[0]);
                return;
            }

            bool multiMap = false;
            for (int i = 1; i < stations.Count; i++)
            {
                if (stations[i].Map != stations[0].Map)
                {
                    multiMap = true;
                    break;
                }
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < stations.Count; i++)
            {
                Building_WorkTable table = stations[i];
                string label = table.LabelCap;
                if (multiMap && table.Map?.Parent != null)
                {
                    label += " (" + table.Map.Parent.LabelCap + ")";
                }

                label += "  —  " + "Homesteader_PantryMakeBillCount".Translate(table.billStack.Count);
                options.Add(new FloatMenuOption(
                    label,
                    () => PantryMakeUtility.AddBillAt(recipe, table),
                    mouseoverGuiAction: _ => TargetHighlighter.Highlight(table)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static string SortLabel(PantrySortMode mode)
        {
            switch (mode)
            {
                case PantrySortMode.Name:
                    return "Homesteader_PantrySortName".Translate();
                case PantrySortMode.RotSoonest:
                    return "Homesteader_PantrySortRot".Translate();
                default:
                    return "Homesteader_PantrySortCount".Translate();
            }
        }

        private static string LineText(PantryMakeRow row)
        {
            string name = row.recipe?.LabelCap ?? "recipe";
            switch (row.status)
            {
                case PantryMakeStatus.CanMake:
                    return name + "  ×" + row.batches;
                case PantryMakeStatus.OneShort:
                    string missing = row.missingDef != null
                        ? row.missingDef.LabelCap
                        : "Homesteader_PantryMakeUnknownIng".Translate().ToString();
                    return name + "  —  " + "Homesteader_PantryMakeNeedIng".Translate(row.missingCount, missing);
                default:
                    return name + "  —  " + row.lockReason;
            }
        }
    }
}
