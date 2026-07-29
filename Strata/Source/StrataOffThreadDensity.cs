using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Strata
{
    // A2/A3: plain-array density fills + overlay color bake on worker threads.
    // Main thread builds snapshots (cell indices, fog masks, gas meta); workers
    // never touch Verse APIs.
    internal static class StrataOffThreadDensity
    {
        public struct RoomDensityFill
        {
            public int gasIndex;
            public float density;
            public int[] cellIndices;
        }

        // Plain gas rules for OverlayContributionWeight / ResolveOverlayColor.
        public struct GasOverlayMeta
        {
            public int index;
            public bool valid;
            public bool harmWhenBelow;
            public float harmThreshold;
            public float overlayThreshold;
            public Color overlayColor;
            public bool isAtmospheric;
            public bool isPollutant;
            public bool isOxygen;
            public bool isNitrogen;
            public bool isArgon;
            public bool isCarbonDioxide;
            public bool hasHarmHediff;
            public float targetFraction;
            public bool naturalGasesActive;
            public bool pollutantGasesActive;
        }

        private static readonly Color LowOxygenAlarm = new Color(0.92f, 0.28f, 0.12f);
        private const float AmbientOverlayTolerance = 0.035f;
        private const float MaxOverlayAlpha = 0.42f;
        private const float MinOverlayAlpha = 0.04f;

        public static bool OffThreadEnabled => StrataOffThreadWork.OffThreadAtmosphereEnabled;

        // A2: fill density planes from snapshotted room cells + breath grids.
        public static void FillPlanesParallel(
            float[][] planes,
            List<RoomDensityFill> roomFills,
            float[] breathO2,
            float[] breathCo2,
            int oxygenIndex,
            int carbonDioxideIndex,
            bool[] foggedOrInvalid)
        {
            if (planes == null)
            {
                return;
            }

            if (roomFills != null && roomFills.Count > 0)
            {
                Parallel.For(0, roomFills.Count, i =>
                {
                    RoomDensityFill fill = roomFills[i];
                    if (fill.cellIndices == null
                        || fill.gasIndex < 0
                        || fill.gasIndex >= planes.Length
                        || planes[fill.gasIndex] == null)
                    {
                        return;
                    }
                    float[] plane = planes[fill.gasIndex];
                    int[] cells = fill.cellIndices;
                    float density = fill.density;
                    for (int c = 0; c < cells.Length; c++)
                    {
                        int index = cells[c];
                        if (index < 0 || index >= plane.Length)
                        {
                            continue;
                        }
                        if (foggedOrInvalid != null && foggedOrInvalid[index])
                        {
                            continue;
                        }
                        plane[index] = density;
                    }
                });
            }

            PaintBreathPlane(planes, breathO2, oxygenIndex, foggedOrInvalid);
            PaintBreathPlane(planes, breathCo2, carbonDioxideIndex, foggedOrInvalid);
        }

        private static void PaintBreathPlane(
            float[][] planes,
            float[] source,
            int gasIndex,
            bool[] foggedOrInvalid)
        {
            if (source == null || gasIndex < 0 || gasIndex >= planes.Length || planes[gasIndex] == null)
            {
                return;
            }
            float[] plane = planes[gasIndex];
            int len = Math.Min(plane.Length, source.Length);
            Parallel.For(0, len, i =>
            {
                float value = source[i];
                if (value <= 0f)
                {
                    return;
                }
                if (foggedOrInvalid != null && foggedOrInvalid[i])
                {
                    return;
                }
                plane[i] = value;
            });
        }

        // A3: bake per-cell overlay colors from density planes + gas meta.
        public static Color[] BakeOverlayColorsParallel(
            float[][] planes,
            GasOverlayMeta[] gases,
            int cellCount)
        {
            var colors = new Color[cellCount];
            if (planes == null || gases == null || cellCount <= 0)
            {
                return colors;
            }

            Parallel.For(0, cellCount, index =>
            {
                colors[index] = ComputeCellColor(planes, gases, index);
            });
            return colors;
        }

        public static GasOverlayMeta[] SnapshotGasMeta(Map map)
        {
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            var meta = new GasOverlayMeta[gases.Count];
            StrataSettings settings = StrataMod.Settings;
            bool natural = settings == null || settings.NaturalGasesActive;
            bool pollutant = settings == null || settings.PollutantGasesActive;
            AtmosphericMix.TargetMix target = AtmosphericMix.TargetForMap(map);

            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                if (gas == null)
                {
                    continue;
                }
                meta[i] = new GasOverlayMeta
                {
                    index = gas.index,
                    valid = true,
                    harmWhenBelow = gas.harmWhenBelow,
                    harmThreshold = gas.harmThreshold,
                    overlayThreshold = gas.overlayThreshold,
                    overlayColor = gas.overlayColor,
                    isAtmospheric = AtmosphericMix.IsAtmosphericComponent(gas),
                    isPollutant = AtmosphericMix.IsPollutantGas(gas),
                    isOxygen = gas == StrataGasDefOf.Strata_Oxygen,
                    isNitrogen = gas == StrataGasDefOf.Strata_Nitrogen,
                    isArgon = gas == StrataGasDefOf.Strata_Argon,
                    isCarbonDioxide = gas == StrataGasDefOf.Strata_CarbonDioxide,
                    hasHarmHediff = gas.harmHediff != null,
                    targetFraction = TargetFraction(gas, target),
                    naturalGasesActive = natural,
                    pollutantGasesActive = pollutant,
                };
            }
            return meta;
        }

        private static float TargetFraction(StrataGasDef gas, AtmosphericMix.TargetMix target)
        {
            if (gas == StrataGasDefOf.Strata_Nitrogen)
            {
                return target.nitrogen;
            }
            if (gas == StrataGasDefOf.Strata_Oxygen)
            {
                return target.oxygen;
            }
            if (gas == StrataGasDefOf.Strata_Argon)
            {
                return target.argon;
            }
            if (gas == StrataGasDefOf.Strata_CarbonDioxide)
            {
                return target.carbonDioxide;
            }
            return 0f;
        }

        private static Color ComputeCellColor(float[][] planes, GasOverlayMeta[] gases, int cellIndex)
        {
            GasOverlayMeta primary = default;
            GasOverlayMeta secondary = default;
            float primaryWeight = 0f;
            float secondaryWeight = 0f;
            float primaryDensity = 0f;
            float secondaryDensity = 0f;
            bool hasPrimary = false;
            bool hasSecondary = false;

            for (int i = 0; i < gases.Length; i++)
            {
                GasOverlayMeta gas = gases[i];
                if (!gas.valid || gas.index < 0 || gas.index >= planes.Length)
                {
                    continue;
                }
                float[] plane = planes[gas.index];
                if (plane == null || cellIndex >= plane.Length)
                {
                    continue;
                }
                float density = plane[cellIndex];
                float weight = OverlayWeight(gas, density);
                if (weight <= 0.001f)
                {
                    continue;
                }
                if (weight > primaryWeight)
                {
                    secondary = primary;
                    secondaryWeight = primaryWeight;
                    secondaryDensity = primaryDensity;
                    hasSecondary = hasPrimary;
                    primary = gas;
                    primaryWeight = weight;
                    primaryDensity = density;
                    hasPrimary = true;
                }
                else if (weight > secondaryWeight)
                {
                    secondary = gas;
                    secondaryWeight = weight;
                    secondaryDensity = density;
                    hasSecondary = true;
                }
            }

            if (!hasPrimary)
            {
                return Color.clear;
            }

            Color color = ResolveOverlayColor(primary, primaryDensity);
            if (hasSecondary && secondaryWeight >= primaryWeight * 0.2f)
            {
                float mix = secondaryWeight / (primaryWeight + secondaryWeight);
                color = Color.Lerp(color, ResolveOverlayColor(secondary, secondaryDensity), mix * 0.45f);
            }
            float blendWeight = primaryWeight + secondaryWeight * 0.35f;
            float alpha = Mathf.Lerp(
                MinOverlayAlpha,
                MaxOverlayAlpha,
                Mathf.Clamp01(Mathf.Sqrt(blendWeight)));
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static float OverlayWeight(GasOverlayMeta gas, float density)
        {
            if (density <= 0f)
            {
                return 0f;
            }
            if (!gas.naturalGasesActive && gas.isAtmospheric)
            {
                return 0f;
            }
            if (!gas.pollutantGasesActive && gas.isPollutant)
            {
                return 0f;
            }

            if (gas.harmWhenBelow)
            {
                if (density >= gas.harmThreshold)
                {
                    return 0f;
                }
                float t = 1f - Mathf.Clamp01(density / gas.harmThreshold);
                return Mathf.Sqrt(t);
            }

            if (gas.isAtmospheric)
            {
                float target = gas.targetFraction;
                if (gas.isCarbonDioxide)
                {
                    float excess = density - target;
                    if (excess <= gas.overlayThreshold)
                    {
                        return 0f;
                    }
                    float span = Mathf.Max(0.08f, gas.harmThreshold - target);
                    return Mathf.Clamp01(excess / span);
                }

                float delta = Mathf.Abs(density - target);
                if (delta <= AmbientOverlayTolerance)
                {
                    return 0f;
                }
                return Mathf.Clamp01((delta - AmbientOverlayTolerance) / 0.2f);
            }

            if (density <= gas.overlayThreshold)
            {
                return 0f;
            }
            float reference = gas.hasHarmHediff
                ? Mathf.Max(gas.harmThreshold, gas.overlayThreshold + 0.05f)
                : 0.55f;
            float pollutantSpan = reference - gas.overlayThreshold;
            if (pollutantSpan <= 0.001f)
            {
                return 1f;
            }
            return Mathf.Clamp01((density - gas.overlayThreshold) / pollutantSpan);
        }

        private static Color ResolveOverlayColor(GasOverlayMeta gas, float density)
        {
            Color color = gas.overlayColor;
            if (gas.harmWhenBelow && density < gas.harmThreshold)
            {
                float t = 1f - Mathf.Clamp01(density / gas.harmThreshold);
                color = Color.Lerp(color, LowOxygenAlarm, t * 0.85f);
            }
            return BoostForOverlay(color);
        }

        private static Color BoostForOverlay(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * 1.35f + 0.12f);
            v = Mathf.Clamp01(v * 1.15f + 0.08f);
            Color boosted = Color.HSVToRGB(h, s, v);
            boosted.a = color.a;
            return boosted;
        }
    }
}
