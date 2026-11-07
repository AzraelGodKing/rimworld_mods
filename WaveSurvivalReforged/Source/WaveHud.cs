using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    // A small box at the top of the screen: the next wave, what it is, and how the last one went.
    // Draw-only; the text is rebuilt at most once per check interval.
    public static class WaveHud
    {
        private const float Width = 300f;
        private const float LineHeight = 22f;
        private const int RefreshTicks = 250;

        private static int builtTick = -1;
        private static LoadedLanguage builtLanguage;
        private static string line1;
        private static string line2;
        private static string line3;

        public static void Draw(WaveSurvivalComponent waves)
        {
            if (Event.current.type != EventType.Repaint || !WaveSurvivalMod.Settings.showHud)
                return;
            if (!WaveSurvivalComponent.ModeActive || !waves.Started || Find.CurrentMap == null
                || WorldRendererUtility.WorldSelected || Find.UIRoot.screenshotMode.FiltersCurrentEvent)
                return;
            Rebuild(waves);

            int lines = line3 == null ? 2 : 3;
            var rect = new Rect((UI.screenWidth - Width) / 2f, 6f, Width, lines * LineHeight + 8f);
            Widgets.DrawWindowBackground(rect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect row = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, LineHeight);
            GUI.color = waves.TicksUntilNextWave <= Alert_WaveTimer.WarnTicks ? ColorLibrary.RedReadable : Color.white;
            Widgets.Label(row, line1);
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            row.y += LineHeight;
            Widgets.Label(row, line2);
            if (line3 != null)
            {
                row.y += LineHeight;
                Widgets.Label(row, line3);
            }
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void Rebuild(WaveSurvivalComponent waves)
        {
            int now = Find.TickManager.TicksGame;
            if (line1 != null && now - builtTick < RefreshTicks && now >= builtTick
                && builtLanguage == LanguageDatabase.activeLanguage)
                return;
            builtTick = now;
            builtLanguage = LanguageDatabase.activeLanguage;
            int ticks = Mathf.Max(0, waves.TicksUntilNextWave);
            line1 = "WSR_HudNext".Translate(waves.WaveNum, ticks.ToStringTicksToPeriod(allowSeconds: false, shortForm: true));
            WavePlan plan = waves.NextPlan;
            line2 = plan != null
                ? (string)"WSR_HudPlan".Translate(plan.Describe(), Mathf.RoundToInt(plan.points))
                : (string)"WSR_HudPlanUnknown".Translate();
            WaveTracker tracker = waves.Tracker;
            if (tracker.Active)
                line3 = "WSR_HudActive".Translate(tracker.ActiveWave, tracker.Standing);
            else if (tracker.LastWave > 0)
                line3 = "WSR_HudLast".Translate(tracker.LastWave, tracker.LastKilled, tracker.LastDowned, tracker.LastFled);
            else
                line3 = null;
        }
    }
}
