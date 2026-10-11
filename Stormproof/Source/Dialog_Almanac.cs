using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class Dialog_Almanac : Window
    {
        private static readonly Color CurveColor = new Color(0.45f, 0.80f, 0.95f);
        private static readonly Color BrownoutLineColor = new Color(0.95f, 0.80f, 0.25f, 0.6f);

        private readonly MapComponent_Stormproof component;
        private readonly CompWeatherForecaster forecaster;
        private readonly float[] curve = new float[GridForecastUtility.CurveSteps];
        private GridForecast forecast;
        private bool hasCurve;
        private int curveTick = -1;
        private Vector2 scroll;

        public Dialog_Almanac(MapComponent_Stormproof component, CompWeatherForecaster forecaster = null)
        {
            this.component = component;
            this.forecaster = forecaster;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Stormproof_Almanac_Title".Translate());
            Text.Font = GameFont.Small;

            IReadOnlyList<AlmanacEntry> entries = component.Almanac;
            AlmanacTotals t = component.Lifetime;
            float headerH = 0f;
            RefreshCurve();
            if (hasCurve)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, 24f),
                    "Stormproof_Almanac_ChargeForecast".Translate(
                        forecast.StartFraction.ToStringPercent(),
                        forecast.NadirFraction.ToStringPercent()));
                DrawSparkline(new Rect(inRect.x, inRect.y + 60f, inRect.width, 30f));
                headerH += 58f;
            }
            if (t != null && t.Any)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 36f + headerH, inRect.width, 24f),
                    "Stormproof_Almanac_Lifetime".Translate(
                        t.strikesCaught, t.strikesMissed,
                        t.zzztAbsorbed, t.zzztSuffered,
                        t.firesSnuffed, t.wearHits));
                headerH += 26f;
            }
            if (component.LongestCleanTicks > 0)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 36f + headerH, inRect.width, 24f),
                    "Stormproof_Almanac_CleanStretch".Translate(
                        component.LongestCleanTicks.ToStringTicksToPeriod(),
                        component.CurrentCleanTicks.ToStringTicksToPeriod()));
                headerH += 26f;
            }
            if (headerH > 0f)
            {
                headerH += 2f;
            }

            Rect listRect = new Rect(inRect.x, inRect.y + 40f + headerH, inRect.width,
                inRect.height - 40f - headerH);
            float viewH = 8f;
            for (int i = 0; i < entries.Count; i++)
            {
                viewH += entries[i].HasLedger ? 48f : 26f;
            }
            viewH = Mathf.Max(listRect.height, viewH);
            Rect view = new Rect(0f, 0f, listRect.width - 16f, viewH);
            Widgets.BeginScrollView(listRect, ref scroll, view);
            if (entries.Count == 0)
            {
                Widgets.Label(view, "Stormproof_Almanac_Empty".Translate());
            }
            else
            {
                float y = 0f;
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    AlmanacEntry e = entries[i];
                    string season = ((Quadrum)e.quadrum).Label();
                    string dur = e.durationTicks > 0
                        ? e.durationTicks.ToStringTicksToPeriod()
                        : "Stormproof_Almanac_Ongoing".Translate().ToString();
                    string line = "Stormproof_Almanac_Line".Translate(
                        e.year.ToString(), season, e.label, dur);
                    float h = e.HasLedger ? 46f : 26f;
                    Widgets.Label(new Rect(0f, y, view.width, 24f), line);
                    if (e.HasLedger)
                    {
                        Widgets.Label(new Rect(8f, y + 22f, view.width - 8f, 22f),
                            "Stormproof_Almanac_Ledger".Translate(
                                e.strikesCaught, e.strikesMissed,
                                e.zzztAbsorbed, e.zzztSuffered,
                                e.firesSnuffed, e.wearHits));
                    }
                    y += h;
                }
            }
            Widgets.EndScrollView();
        }

        private void RefreshCurve()
        {
            int tick = Find.TickManager.TicksGame;
            if (curveTick >= 0 && tick - curveTick < GridForecastUtility.StepTicks)
            {
                return;
            }
            curveTick = tick;
            hasCurve = forecaster != null && forecaster.Active
                && GridForecastUtility.TryProjectNet(forecaster.parent.Map, forecaster.PowerNet, curve, out forecast);
        }

        private void DrawSparkline(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.25f));
            float brownoutY = rect.yMax - rect.height * GridForecastUtility.BrownoutStart;
            Widgets.DrawLine(new Vector2(rect.x, brownoutY), new Vector2(rect.xMax, brownoutY),
                BrownoutLineColor, 1f);
            int n = curve.Length;
            Vector2 prev = new Vector2(rect.x, rect.yMax - rect.height * Mathf.Clamp01(forecast.StartFraction));
            for (int i = 0; i < n; i++)
            {
                Vector2 next = new Vector2(
                    rect.x + rect.width * (i + 1) / n,
                    rect.yMax - rect.height * Mathf.Clamp01(curve[i]));
                Widgets.DrawLine(prev, next, CurveColor, 1.5f);
                prev = next;
            }
            TooltipHandler.TipRegion(rect, "Stormproof_Almanac_ChargeForecastTip".Translate());
        }
    }
}
