using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Mod options > Diagnostics: one row per fault-guard subsystem with its
    // health, last error and a re-arm button.
    public static class StrataDiagnosticsTab
    {
        private const float RowHeight = 54f;
        private static Vector2 scroll;

        private static readonly StrataFaultGuard.System[] Systems =
            (StrataFaultGuard.System[])Enum.GetValues(typeof(StrataFaultGuard.System));

        public static int TrippedCount()
        {
            int n = 0;
            for (int i = 0; i < Systems.Length; i++)
            {
                if (StrataFaultGuard.IsTripped(Systems[i]))
                {
                    n++;
                }
            }
            return n;
        }

        public static void Draw(Rect rect)
        {
            Text.Font = GameFont.Small;
            Rect header = new Rect(rect.x, rect.y, rect.width, 48f);
            GUI.color = Color.gray;
            Widgets.Label(header.LeftPart(0.72f), "Strata_Diag_Header".Translate());
            GUI.color = Color.white;
            Rect all = new Rect(header.xMax - 150f, header.y, 150f, 28f);
            if (Widgets.ButtonText(all, "Strata_Diag_RearmAll".Translate()))
            {
                for (int i = 0; i < Systems.Length; i++)
                {
                    StrataFaultGuard.Rearm(Systems[i]);
                }
            }

            Rect outRect = new Rect(rect.x, header.yMax + 4f, rect.width, rect.height - header.height - 4f);
            Rect view = new Rect(0f, 0f, outRect.width - 16f, Systems.Length * RowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, view);
            int now = Find.TickManager?.TicksGame ?? 0;
            for (int i = 0; i < Systems.Length; i++)
            {
                StrataFaultGuard.System system = Systems[i];
                Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 4f);
                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(row);
                }
                bool tripped = StrataFaultGuard.IsTripped(system);
                int errors = StrataFaultGuard.TotalErrors(system);

                Rect name = new Rect(row.x + 4f, row.y + 2f, row.width * 0.38f, 24f);
                Widgets.Label(name, StrataFaultGuard.Label(system).CapitalizeFirst());

                Rect status = new Rect(name.xMax, row.y + 2f, row.width * 0.32f, 24f);
                if (tripped)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    Widgets.Label(status, "Strata_Diag_Tripped".Translate(errors));
                }
                else if (errors > 0)
                {
                    GUI.color = ColorLibrary.Yellow;
                    Widgets.Label(status, "Strata_Diag_Errors".Translate(errors));
                }
                else
                {
                    GUI.color = ColorLibrary.Green;
                    Widgets.Label(status, "Strata_Diag_Healthy".Translate());
                }
                GUI.color = Color.white;

                Rect button = new Rect(row.xMax - 110f, row.y + 2f, 106f, 26f);
                if (errors > 0 || tripped)
                {
                    if (Widgets.ButtonText(button, "Strata_Diag_Rearm".Translate()))
                    {
                        StrataFaultGuard.Rearm(system);
                    }
                }

                string last = StrataFaultGuard.LastError(system);
                if (!last.NullOrEmpty())
                {
                    int ago = now - StrataFaultGuard.LastErrorTick(system);
                    Rect lastRect = new Rect(row.x + 12f, row.y + 26f, row.width - 130f, 22f);
                    GUI.color = Color.gray;
                    Text.Font = GameFont.Tiny;
                    string text = "Strata_Diag_LastError".Translate(ago.ToStringTicksToPeriod(), last);
                    Widgets.Label(lastRect, text.Truncate(lastRect.width));
                    TooltipHandler.TipRegion(lastRect, text);
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                }
            }
            Widgets.EndScrollView();
        }
    }
}
