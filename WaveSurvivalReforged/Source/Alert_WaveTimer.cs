using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public class Alert_WaveTimer : Alert
    {
        public const int WarnTicks = 6000;
        private const int RefreshTicks = 250;

        private int builtTick = -1;
        private LoadedLanguage builtLanguage;
        private string label;
        private TaggedString explanation;

        public Alert_WaveTimer()
        {
            defaultPriority = AlertPriority.High;
        }

        private static WaveSurvivalComponent Waves =>
            WaveSurvivalComponent.ModeActive ? WaveSurvivalComponent.Instance : null;

        protected override Color BGColor
        {
            get
            {
                WaveSurvivalComponent waves = Waves;
                if (waves == null || waves.TicksUntilNextWave > WarnTicks)
                    return Color.clear;
                float pulse = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
                return new Color(pulse, pulse, pulse) * Color.red;
            }
        }

        public override string GetLabel()
        {
            WaveSurvivalComponent waves = Waves;
            if (waves == null)
                return "";
            Rebuild(waves);
            return label;
        }

        public override TaggedString GetExplanation()
        {
            WaveSurvivalComponent waves = Waves;
            if (waves == null)
                return "";
            Rebuild(waves);
            return explanation;
        }

        public override AlertReport GetReport()
        {
            WaveSurvivalComponent waves = Waves;
            return waves != null && waves.Started;
        }

        // Text changes at most once per check interval, so build it then instead of every frame.
        private void Rebuild(WaveSurvivalComponent waves)
        {
            int now = Find.TickManager.TicksGame;
            if (label != null && now - builtTick < RefreshTicks && now >= builtTick
                && builtLanguage == LanguageDatabase.activeLanguage)
                return;
            builtTick = now;
            builtLanguage = LanguageDatabase.activeLanguage;
            label = "WSR_WaveTimerLabel".Translate(waves.WaveNum, Remaining(waves, shortForm: true));
            explanation = "WSR_WaveTimerExplanation".Translate(waves.WaveNum, Remaining(waves, shortForm: false));
            WavePlan plan = waves.NextPlan;
            if (plan != null)
                explanation += "\n\n" + "WSR_WaveTimerPlan".Translate(plan.Describe(), Mathf.RoundToInt(plan.points));
            if (waves.NextWavePool == WavePool.Boss)
                explanation += "\n\n" + "WSR_WaveTimerBoss".Translate();
            else if (waves.NextWavePool == WavePool.Special)
                explanation += "\n\n" + "WSR_WaveTimerSpecial".Translate();
        }

        private static string Remaining(WaveSurvivalComponent waves, bool shortForm)
        {
            int ticks = Mathf.Max(0, waves.TicksUntilNextWave);
            return ticks.ToStringTicksToPeriod(allowSeconds: false, shortForm: shortForm);
        }
    }
}
