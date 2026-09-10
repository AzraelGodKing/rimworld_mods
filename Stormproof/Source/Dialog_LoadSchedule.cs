using RimWorld;
using UnityEngine;
using Verse;

namespace Stormproof
{
    public class Dialog_LoadSchedule : Window
    {
        private readonly CompLoadShedder shedder;

        public Dialog_LoadSchedule(CompLoadShedder shedder)
        {
            this.shedder = shedder;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 320f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "Stormproof_LoadShedder_ScheduleTitle".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f),
                "Stormproof_LoadShedder_ScheduleHint".Translate());

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
                if (Widgets.ButtonInvisible(cell))
                {
                    shedder.ToggleHour(h);
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
            if (Widgets.ButtonText(new Rect(inRect.x, y + 118f, 180f, 28f),
                "Stormproof_LoadShedder_ScheduleClear".Translate()))
            {
                shedder.ClearSchedule();
            }
        }
    }
}
