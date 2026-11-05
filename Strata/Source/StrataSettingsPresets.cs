using System;
using Verse;

namespace Strata
{
    public enum StrataSettingsPreset
    {
        Custom,
        Performance,
        Balanced,
        Immersive,
    }

    // Presets set only the options that change cost or how much Strata
    // simulates. The current preset is worked out from the values, so
    // changing any one of them shows Custom and nothing extra is saved.
    public static class StrataSettingsPresets
    {
        private struct Values
        {
            public bool naturalGases;
            public bool pollutantGases;
            public bool gasEvents;
            public bool gasOverlayRoomLabels;
            public AtmosphereQualityLevel atmosphereQuality;
            public bool offThreadAtmosphere;
            public bool offThreadWorkRelay;
            public bool hibernateEmptyLevels;
            public bool reduceBackgroundLevels;
            public bool lightweightOccupiedLevels;
            public bool seeBelowLive;
            public bool depthDim;
            public bool crossLevelAutoEngage;
            public bool floodEvents;
            public bool noiseInfestation;
        }

        // Balanced is the shipped defaults.
        private static readonly Values Defaults = Read(new StrataSettings());

        private static Values For(StrataSettingsPreset preset)
        {
            Values balanced = Defaults;
            switch (preset)
            {
                case StrataSettingsPreset.Performance:
                    balanced.naturalGases = false;
                    balanced.pollutantGases = false;
                    balanced.gasEvents = false;
                    balanced.gasOverlayRoomLabels = false;
                    balanced.atmosphereQuality = AtmosphereQualityLevel.Low;
                    balanced.offThreadAtmosphere = true;
                    balanced.offThreadWorkRelay = true;
                    balanced.hibernateEmptyLevels = true;
                    balanced.reduceBackgroundLevels = true;
                    balanced.lightweightOccupiedLevels = true;
                    balanced.seeBelowLive = false;
                    balanced.depthDim = false;
                    balanced.crossLevelAutoEngage = false;
                    return balanced;
                case StrataSettingsPreset.Immersive:
                    balanced.naturalGases = true;
                    balanced.pollutantGases = true;
                    balanced.gasEvents = true;
                    balanced.gasOverlayRoomLabels = true;
                    balanced.atmosphereQuality = AtmosphereQualityLevel.High;
                    balanced.seeBelowLive = true;
                    balanced.depthDim = true;
                    balanced.crossLevelAutoEngage = true;
                    balanced.floodEvents = true;
                    balanced.noiseInfestation = true;
                    return balanced;
                default:
                    return balanced;
            }
        }

        private static Values Read(StrataSettings s)
        {
            return new Values
            {
                naturalGases = s.naturalGasesEnabled,
                pollutantGases = s.pollutantGasesEnabled,
                gasEvents = s.gasEventsEnabled,
                gasOverlayRoomLabels = s.gasOverlayRoomLabels,
                atmosphereQuality = s.atmosphereQuality,
                offThreadAtmosphere = s.offThreadAtmosphere,
                offThreadWorkRelay = s.offThreadWorkRelay,
                hibernateEmptyLevels = s.hibernateEmptyLevels,
                reduceBackgroundLevels = s.reduceBackgroundLevels,
                lightweightOccupiedLevels = s.lightweightOccupiedLevels,
                seeBelowLive = s.seeBelowLive,
                depthDim = s.depthDimEnabled,
                crossLevelAutoEngage = s.crossLevelAutoEngage,
                floodEvents = s.floodEventsEnabled,
                noiseInfestation = s.noiseInfestationEnabled,
            };
        }

        public static void Apply(StrataSettings s, StrataSettingsPreset preset)
        {
            if (s == null || preset == StrataSettingsPreset.Custom)
            {
                return;
            }
            Values v = For(preset);
            s.naturalGasesEnabled = v.naturalGases;
            s.pollutantGasesEnabled = v.pollutantGases;
            s.gasEventsEnabled = v.gasEvents;
            s.gasOverlayRoomLabels = v.gasOverlayRoomLabels;
            s.atmosphereQuality = v.atmosphereQuality;
            s.offThreadAtmosphere = v.offThreadAtmosphere;
            s.offThreadWorkRelay = v.offThreadWorkRelay;
            s.hibernateEmptyLevels = v.hibernateEmptyLevels;
            s.reduceBackgroundLevels = v.reduceBackgroundLevels;
            s.lightweightOccupiedLevels = v.lightweightOccupiedLevels;
            s.seeBelowLive = v.seeBelowLive;
            s.depthDimEnabled = v.depthDim;
            s.crossLevelAutoEngage = v.crossLevelAutoEngage;
            s.floodEventsEnabled = v.floodEvents;
            s.noiseInfestationEnabled = v.noiseInfestation;
        }

        public static StrataSettingsPreset Current(StrataSettings s)
        {
            if (s == null)
            {
                return StrataSettingsPreset.Custom;
            }
            Values now = Read(s);
            foreach (StrataSettingsPreset preset in Enum.GetValues(typeof(StrataSettingsPreset)))
            {
                if (preset != StrataSettingsPreset.Custom && now.Equals(For(preset)))
                {
                    return preset;
                }
            }
            return StrataSettingsPreset.Custom;
        }

        public static string Label(StrataSettingsPreset preset) =>
            ("Strata_Settings_Preset" + preset).Translate();

        public static string Description(StrataSettingsPreset preset) =>
            ("Strata_Settings_Preset" + preset + "Desc").Translate();
    }
}
