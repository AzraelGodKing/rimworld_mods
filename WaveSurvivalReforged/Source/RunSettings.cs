using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public enum WavePreset
    {
        Casual,
        Standard,
        Brutal,
        Custom,
    }

    /// <summary>
    /// The tuning one run plays with. Copied from the mod settings when the run starts and
    /// saved with the game, so changing settings later never changes a run in progress.
    /// Standard is exactly Defs/WaveSurvivalConfig.xml.
    /// </summary>
    public class RunSettings : IExposable
    {
        public const float StandardMood = 35f;

        public WavePreset preset = WavePreset.Standard;
        public float pointsFactor = 1f;
        public float daysBeforeFirstWave = 5f;
        public float daysBetweenWaves = 2f;
        public int specialWaveInterval = 4;
        public float supplyFactor = 1f;
        public float moodBuff = StandardMood;
        public float wandererFactor = 1f;

        public static RunSettings For(WavePreset preset)
        {
            WaveSurvivalConfigDef cfg = WSR_DefOf.WSR_Config;
            var run = new RunSettings
            {
                preset = preset,
                daysBeforeFirstWave = cfg.daysBeforeFirstWave,
                daysBetweenWaves = cfg.daysBetweenWaves,
                specialWaveInterval = cfg.specialWaveInterval,
            };
            switch (preset)
            {
                case WavePreset.Casual:
                    run.pointsFactor = 0.6f;
                    run.daysBeforeFirstWave *= 1.4f;
                    run.daysBetweenWaves *= 1.25f;
                    run.specialWaveInterval += 1;
                    run.supplyFactor = 1.3f;
                    run.wandererFactor = 1.5f;
                    break;
                case WavePreset.Brutal:
                    run.pointsFactor = 1.5f;
                    run.daysBeforeFirstWave *= 0.6f;
                    run.daysBetweenWaves *= 0.75f;
                    run.specialWaveInterval = Mathf.Max(2, run.specialWaveInterval - 1);
                    run.supplyFactor = 0.75f;
                    run.moodBuff = 20f;
                    run.wandererFactor = 0.5f;
                    break;
            }
            return run;
        }

        public RunSettings Copy()
        {
            return (RunSettings)MemberwiseClone();
        }

        public void Clamp()
        {
            pointsFactor = Mathf.Clamp(pointsFactor, 0.1f, 5f);
            daysBeforeFirstWave = Mathf.Clamp(daysBeforeFirstWave, 0.5f, 30f);
            daysBetweenWaves = Mathf.Clamp(daysBetweenWaves, 0.25f, 15f);
            specialWaveInterval = Mathf.Clamp(specialWaveInterval, 1, 20);
            supplyFactor = Mathf.Clamp(supplyFactor, 0f, 3f);
            moodBuff = Mathf.Clamp(moodBuff, 0f, 60f);
            wandererFactor = Mathf.Clamp(wandererFactor, 0f, 5f);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref preset, "preset", WavePreset.Standard);
            Scribe_Values.Look(ref pointsFactor, "pointsFactor", 1f);
            Scribe_Values.Look(ref daysBeforeFirstWave, "daysBeforeFirstWave", 5f);
            Scribe_Values.Look(ref daysBetweenWaves, "daysBetweenWaves", 2f);
            Scribe_Values.Look(ref specialWaveInterval, "specialWaveInterval", 4);
            Scribe_Values.Look(ref supplyFactor, "supplyFactor", 1f);
            Scribe_Values.Look(ref moodBuff, "moodBuff", StandardMood);
            Scribe_Values.Look(ref wandererFactor, "wandererFactor", 1f);
        }
    }
}
