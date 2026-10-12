using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public class Alert_WaveTimer : Alert
    {
        private const int WarnTicks = 6000;

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
            return "WSR_WaveTimerLabel".Translate(waves.WaveNum, Remaining(waves, shortForm: true));
        }

        public override TaggedString GetExplanation()
        {
            WaveSurvivalComponent waves = Waves;
            if (waves == null)
                return "";
            TaggedString text = "WSR_WaveTimerExplanation".Translate(waves.WaveNum, Remaining(waves, shortForm: false));
            if (waves.NextWaveIsSpecial)
                text += "\n\n" + "WSR_WaveTimerSpecial".Translate();
            return text;
        }

        public override AlertReport GetReport()
        {
            WaveSurvivalComponent waves = Waves;
            return waves != null && waves.Started;
        }

        private static string Remaining(WaveSurvivalComponent waves, bool shortForm)
        {
            int ticks = Mathf.Max(0, waves.TicksUntilNextWave);
            return ticks.ToStringTicksToPeriod(allowSeconds: false, shortForm: shortForm);
        }
    }
}
