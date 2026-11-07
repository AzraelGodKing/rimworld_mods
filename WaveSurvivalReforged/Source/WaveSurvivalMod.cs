using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    // Client-side settings. Gameplay values only reach a game through RunSettings, copied when a run starts.
    public class WaveSurvivalSettings : ModSettings
    {
        public bool showHud = true;
        // RimWorld 1.7 only. Turn off for photosensitivity.
        public bool screenEffect = true;
        public WavePreset preset = WavePreset.Standard;
        public RunSettings custom;
        // Local best runs, most waves first. Never read by gameplay.
        public List<RunRecord> bestRuns = new List<RunRecord>();

        // What a new run will use.
        public RunSettings ForNewRun()
        {
            if (preset != WavePreset.Custom)
                return RunSettings.For(preset);
            RunSettings run = (custom ??= RunSettings.For(WavePreset.Standard)).Copy();
            run.preset = WavePreset.Custom;
            run.Clamp();
            return run;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showHud, "showHud", true);
            Scribe_Values.Look(ref screenEffect, "screenEffect", true);
            Scribe_Values.Look(ref preset, "preset", WavePreset.Standard);
            Scribe_Deep.Look(ref custom, "custom");
            Scribe_Collections.Look(ref bestRuns, "bestRuns", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                bestRuns ??= new List<RunRecord>();
        }
    }

    public class WaveSurvivalMod : Mod
    {
        public const int BestRunsKept = 10;

        private static WaveSurvivalMod instance;
        private Vector2 scroll;
        private float viewHeight = 600f;

        public WaveSurvivalMod(ModContentPack content) : base(content)
        {
            instance = this;
            Settings = GetSettings<WaveSurvivalSettings>();
        }

        public static void RecordRun(RunRecord record)
        {
            List<RunRecord> runs = Settings.bestRuns;
            runs.Add(record);
            runs.SortByDescending(r => r.waves, r => r.days);
            while (runs.Count > BestRunsKept)
                runs.RemoveAt(runs.Count - 1);
            instance.WriteSettings();
        }

        public static WaveSurvivalSettings Settings { get; private set; }

        public override string SettingsCategory() => "WSR_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            WaveSurvivalSettings s = Settings;
            var view = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(inRect, ref scroll, view);
            var list = new Listing_Standard();
            list.Begin(view);

            list.CheckboxLabeled("WSR_SettingsShowHud".Translate(), ref s.showHud, "WSR_SettingsShowHudTip".Translate());
#if RIMWORLD_1_7
            list.CheckboxLabeled("WSR_SettingsScreenEffect".Translate(), ref s.screenEffect, "WSR_SettingsScreenEffectTip".Translate());
#endif
            list.GapLine();

            list.Label("WSR_SettingsPreset".Translate(), tooltip: "WSR_SettingsPresetTip".Translate());
            foreach (WavePreset p in new[] { WavePreset.Casual, WavePreset.Standard, WavePreset.Brutal, WavePreset.Custom })
            {
                if (list.RadioButton(("WSR_Preset_" + p).Translate(), s.preset == p, 12f, ("WSR_Preset_" + p + "Tip").Translate()))
                    s.preset = p;
            }

            RunSettings shown = s.ForNewRun();
            if (s.preset == WavePreset.Custom)
            {
                RunSettings c = s.custom;
                list.Gap();
                list.Label("WSR_SettingsPoints".Translate(c.pointsFactor.ToStringPercent()));
                c.pointsFactor = Round(list.Slider(c.pointsFactor, 0.1f, 5f), 0.05f);
                list.Label("WSR_SettingsFirstWave".Translate(c.daysBeforeFirstWave.ToString("0.#")));
                c.daysBeforeFirstWave = Round(list.Slider(c.daysBeforeFirstWave, 0.5f, 30f), 0.5f);
                list.Label("WSR_SettingsBetween".Translate(c.daysBetweenWaves.ToString("0.##")));
                c.daysBetweenWaves = Round(list.Slider(c.daysBetweenWaves, 0.25f, 15f), 0.25f);
                list.Label("WSR_SettingsSpecial".Translate(c.specialWaveInterval));
                c.specialWaveInterval = Mathf.RoundToInt(list.Slider(c.specialWaveInterval, 1f, 20f));
                list.Label("WSR_SettingsSupply".Translate(c.supplyFactor.ToStringPercent()));
                c.supplyFactor = Round(list.Slider(c.supplyFactor, 0f, 3f), 0.05f);
                list.Label("WSR_SettingsMood".Translate(Mathf.RoundToInt(c.moodBuff)));
                c.moodBuff = Mathf.Round(list.Slider(c.moodBuff, 0f, 60f));
                list.Label("WSR_SettingsWanderer".Translate(c.wandererFactor.ToStringPercent()));
                c.wandererFactor = Round(list.Slider(c.wandererFactor, 0f, 5f), 0.05f);
            }
            else
            {
                list.Gap();
                list.Label("WSR_SettingsSummary".Translate(shown.pointsFactor.ToStringPercent(),
                    shown.daysBeforeFirstWave.ToString("0.#"), shown.daysBetweenWaves.ToString("0.##"),
                    shown.specialWaveInterval, shown.supplyFactor.ToStringPercent(), Mathf.RoundToInt(shown.moodBuff),
                    shown.wandererFactor.ToStringPercent()));
            }

            list.GapLine();
            WaveSurvivalComponent waves = Current.Game == null ? null : WaveSurvivalComponent.Instance;
            if (waves?.Run != null)
                list.Label("WSR_SettingsCurrentRun".Translate(("WSR_Preset_" + waves.Run.preset).Translate()));
            else
                list.Label("WSR_SettingsNewRunsOnly".Translate());
            RunStats stats = waves?.Stats;
            if (stats != null)
            {
                list.Label("WSR_SettingsRunStats".Translate(stats.highestWave, stats.wavesCleared, stats.TotalKills,
                    stats.ColonistsLost, stats.dropsReceived, (stats.TicksSurvived / (float)GenDate.TicksPerDay).ToString("0.#")));
            }
            list.Label("WSR_SettingsMultiplayer".Translate());

            list.GapLine();
            list.Label("WSR_SettingsBestRuns".Translate());
            if (s.bestRuns.Count == 0)
            {
                list.Label("WSR_SettingsBestRunsEmpty".Translate());
            }
            else
            {
                for (int i = 0; i < s.bestRuns.Count; i++)
                {
                    RunRecord r = s.bestRuns[i];
                    list.Label("WSR_SettingsBestRunRow".Translate(i + 1, r.waves, r.colony, r.days.ToString("0.#"), r.kills,
                        r.colonistsLost, ("WSR_Preset_" + r.preset).Translate(), r.difficulty, r.date));
                }
                if (list.ButtonText("WSR_SettingsBestRunsClear".Translate()))
                    s.bestRuns.Clear();
            }

            viewHeight = list.CurHeight + 12f;
            list.End();
            Widgets.EndScrollView();
        }

        private static float Round(float value, float step) => Mathf.Round(value / step) * step;
    }
}
