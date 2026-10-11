using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class Dialog_LoadSchedule : Window
    {
        private static readonly Color ForecastBrownout = new Color(0.95f, 0.80f, 0.25f, 0.95f);
        private static readonly Color ForecastCutoff = new Color(0.95f, 0.45f, 0.10f, 0.95f);

        private readonly CompLoadShedder shedder;
        private readonly float[] curve = new float[GridForecastUtility.CurveSteps];
        // Lowest projected supply charge per hour of day; -1 = outside the forecast horizon.
        private readonly float[] hourMin = new float[24];
        private int forecastTick = -1;
        private bool hasForecast;

        public Dialog_LoadSchedule(CompLoadShedder shedder)
        {
            this.shedder = shedder;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 370f);

        public override void DoWindowContents(Rect inRect)
        {
            RefreshForecast();
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Stormproof_LoadShedder_ScheduleTitle".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f),
                "Stormproof_LoadShedder_ScheduleHint".Translate());

            float brownoutLine = StormproofMod.Settings != null && StormproofMod.Settings.enableBrownout
                ? GridForecastUtility.BrownoutStart
                : 0f;
            float cellW = (inRect.width - 23f) / 24f;
            float y = inRect.y + 62f;
            for (int h = 0; h < 24; h++)
            {
                Rect cell = new Rect(inRect.x + h * (cellW + 1f), y, cellW, 36f);
                bool shed = shedder.HourSheds(h);
                if (shed)
                {
                    Widgets.DrawBoxSolid(cell, new Color(0.55f, 0.22f, 0.18f, 0.85f));
                }
                else
                {
                    Widgets.DrawBoxSolid(cell, new Color(0.22f, 0.42f, 0.28f, 0.85f));
                }
                if (hasForecast && hourMin[h] >= 0f)
                {
                    float low = hourMin[h];
                    if (low < shedder.CutoffFraction)
                    {
                        Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - 7f, cell.width, 7f), ForecastCutoff);
                    }
                    else if (low < brownoutLine)
                    {
                        Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - 7f, cell.width, 7f), ForecastBrownout);
                    }
                    TooltipHandler.TipRegion(cell,
                        "Stormproof_LoadShedder_ForecastHourTip".Translate(low.ToStringPercent()));
                }
                if (Widgets.ButtonInvisible(cell))
                {
                    shedder.ToggleHour(h);
                    forecastTick = -1;
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(cell, h.ToString());
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Rect toggle = new Rect(inRect.x, y + 48f, inRect.width, 28f);
            bool forecast = shedder.ForecastOverride;
            Widgets.CheckboxLabeled(toggle,
                "Stormproof_LoadShedder_ForecastOverride".Translate(),
                ref forecast);
            shedder.ForecastOverride = forecast;

            Widgets.Label(new Rect(inRect.x, y + 80f, inRect.width, 36f),
                "Stormproof_LoadShedder_ScheduleLegend".Translate());
            Widgets.Label(new Rect(inRect.x, y + 118f, inRect.width, 36f),
                hasForecast
                    ? "Stormproof_LoadShedder_ForecastLegend".Translate()
                    : "Stormproof_LoadShedder_ForecastNone".Translate());
            if (Widgets.ButtonText(new Rect(inRect.x, y + 160f, 180f, 28f),
                "Stormproof_LoadShedder_ScheduleClear".Translate()))
            {
                shedder.ClearSchedule();
                forecastTick = -1;
            }
        }

        private void RefreshForecast()
        {
            int tick = Find.TickManager.TicksGame;
            if (forecastTick >= 0 && tick - forecastTick < GridForecastUtility.StepTicks)
            {
                return;
            }
            forecastTick = tick;
            hasForecast = false;
            for (int h = 0; h < 24; h++)
            {
                hourMin[h] = -1f;
            }

            Map map = shedder.parent.Map;
            PowerNet supply = shedder.ForecastSupplyNet;
            if (map == null || supply == null || GridForecastUtility.ForecasterOn(supply) == null)
            {
                return;
            }
            if (!GridForecastUtility.TryProjectNet(map, supply, curve, out _))
            {
                return;
            }
            hasForecast = true;
            long ticksAbs = Find.TickManager.TicksAbs;
            float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            for (int i = 0; i < curve.Length; i++)
            {
                int hour = GenDate.HourOfDay(
                    ticksAbs + i * GridForecastUtility.StepTicks + GridForecastUtility.StepTicks / 2, longitude);
                hourMin[hour] = hourMin[hour] < 0f ? curve[i] : Mathf.Min(hourMin[hour], curve[i]);
            }
        }
    }
}
