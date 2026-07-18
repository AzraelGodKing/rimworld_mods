using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Earth-like atmospheric baseline and depth-scarce O₂ for underground levels.
    // Every tracked gas is a volume / mole fraction of one room atmosphere that
    // normally sums to ≈1.0 (100%). Pollutant injection (smoke, methane, deep
    // gas, …) displaces inert buffer only (N₂ first, then Ar) — never O₂ or
    // ambient CO₂. At pollutant load ≈100% or with no inert left, emitters
    // stall instead of stealing breathables. Metabolism (O₂→CO₂) uses separate
    // consume/refill paths. Ambient replenishment fills the remaining budget.
    // Pressurized deep-gas pockets may exceed 1.0 on that channel only.
    public static class AtmosphericMix
    {
        // Mole / volume fractions (sum ≈ 1.0).
        public const float NitrogenFraction = 0.7808f;
        public const float OxygenFraction = 0.2095f;
        public const float ArgonFraction = 0.0093f;
        public const float CarbonDioxideFraction = 0.0004f;

        // Legacy alias used by breath grid and pumps.
        public const float AmbientOxygen = OxygenFraction;

        private const float MinDeepOxygenFraction = 0.07f;
        private const float DepthOxygenFalloff = 0.11f;
        private const float NormalizeEpsilon = 0.0005f;
        private const float OversumMigrateThreshold = 1.05f;
        private const float MaxDeepGasDensity = 3f;
        // Pollutant load at or above this fraction blocks new pollutant emission.
        private const float PollutantLoadFullThreshold = 0.99f;

        public struct TargetMix
        {
            public float nitrogen;
            public float oxygen;
            public float argon;
            public float carbonDioxide;
        }

        public static TargetMix TargetForMap(Map map)
        {
            float o2 = OxygenFraction;
            if (map != null && StrataMapUtility.IsUnderground(map) && !StrataMapUtility.IsUpperLevel(map))
            {
                int depth = StrataDepth.Of(map);
                if (depth > 1)
                {
                    float scarcity = Mathf.Clamp01((depth - 1) * DepthOxygenFalloff);
                    o2 = Mathf.Max(MinDeepOxygenFraction, OxygenFraction * (1f - scarcity));
                }
            }

            float remainder = 1f - o2 - ArgonFraction - CarbonDioxideFraction;
            return new TargetMix
            {
                nitrogen = Mathf.Max(0f, remainder),
                oxygen = o2,
                argon = ArgonFraction,
                carbonDioxide = CarbonDioxideFraction,
            };
        }

        // Surface and upper (A+) decks: infinite ambient even in sealed rooms.
        public static bool ForcesAmbientInEnclosedRooms(Map map)
        {
            if (map == null)
            {
                return false;
            }
            return !StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map);
        }

        // 0–1 lerp strength toward the depth-adjusted target each atmosphere cycle.
        public static float NaturalReplenishRate(Map map)
        {
            if (map == null)
            {
                return 0f;
            }
            if (ForcesAmbientInEnclosedRooms(map))
            {
                return 1f;
            }
            if (!StrataMapUtility.IsUnderground(map))
            {
                return 1f;
            }
            int depth = StrataDepth.Of(map);
            if (depth <= 1)
            {
                return 0.42f;
            }
            return Mathf.Max(0.06f, 0.32f - depth * 0.05f);
        }

        public static bool IsAtmosphericComponent(StrataGasDef gas)
        {
            return gas == StrataGasDefOf.Strata_Nitrogen
                || gas == StrataGasDefOf.Strata_Oxygen
                || gas == StrataGasDefOf.Strata_Argon
                || gas == StrataGasDefOf.Strata_CarbonDioxide;
        }

        public static bool IsPollutantGas(StrataGasDef gas)
        {
            return gas != null && !IsAtmosphericComponent(gas);
        }

        public static bool IsPressurizedGas(StrataGasDef gas)
        {
            return gas == StrataGasDefOf.Strata_DeepGas;
        }

        // Sum of composable fractions (deep gas above 1.0 counts only its first unit).
        public static float ComposableTotal(float[] density)
        {
            if (density == null)
            {
                return 0f;
            }
            int deepIdx = DeepGasIndex();
            float total = 0f;
            for (int i = 0; i < density.Length; i++)
            {
                if (i == deepIdx && density[i] > 1f)
                {
                    total += 1f;
                }
                else
                {
                    total += density[i];
                }
            }
            return total;
        }

        // Deep-gas overpressure above 1.0 (pressurized pockets only).
        public static float PressurizedOverage(float[] density)
        {
            int deepIdx = DeepGasIndex();
            if (deepIdx < 0 || density == null)
            {
                return 0f;
            }
            return Mathf.Max(0f, density[deepIdx] - 1f);
        }

        public static float DisplayTotal(float[] density)
        {
            return ComposableTotal(density) + PressurizedOverage(density);
        }

        // Pollutant / non-breathable share for overlay "load" (smoke, deep gas,
        // excess CO₂, methane, etc. — not baseline N₂/O₂/Ar at ambient levels).
        public static float PollutantFraction(float[] density, Map map)
        {
            if (density == null)
            {
                return 0f;
            }
            TargetMix target = TargetForMap(map);
            float sum = 0f;
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float d = density[gas.index];
                if (d <= NormalizeEpsilon)
                {
                    continue;
                }
                if (IsPollutantForLoad(gas, d, target))
                {
                    sum += d;
                }
            }
            return sum;
        }

        public static bool IsPollutantForLoad(StrataGasDef gas, float density, TargetMix target)
        {
            if (gas == null || density <= NormalizeEpsilon)
            {
                return false;
            }
            if (!IsAtmosphericComponent(gas))
            {
                return true;
            }
            if (gas == StrataGasDefOf.Strata_CarbonDioxide)
            {
                return density > target.carbonDioxide + 0.008f;
            }
            if (gas == StrataGasDefOf.Strata_Oxygen)
            {
                return density < target.oxygen - 0.02f;
            }
            if (gas == StrataGasDefOf.Strata_Nitrogen)
            {
                return density < target.nitrogen - 0.05f;
            }
            if (gas == StrataGasDefOf.Strata_Argon)
            {
                return density > target.argon + 0.008f;
            }
            return false;
        }

        public static bool SignificantForReadout(StrataGasDef gas, float density, Map map)
        {
            if (gas == null || density <= 0.001f)
            {
                return false;
            }
            if (IsAtmosphericComponent(gas))
            {
                TargetMix target = TargetForMap(map);
                if (gas == StrataGasDefOf.Strata_CarbonDioxide || gas == StrataGasDefOf.Strata_Argon)
                {
                    float targetFrac = gas == StrataGasDefOf.Strata_CarbonDioxide
                        ? target.carbonDioxide
                        : target.argon;
                    return density > targetFrac + 0.008f;
                }
                if (gas == StrataGasDefOf.Strata_Oxygen)
                {
                    return density < target.oxygen - 0.015f || density > target.oxygen + 0.03f;
                }
                if (gas == StrataGasDefOf.Strata_Nitrogen)
                {
                    return density < target.nitrogen - 0.04f || density > target.nitrogen + 0.05f;
                }
            }
            return density > gas.overlayThreshold;
        }

        // Returns the pollutant fraction actually injected (0 if refused).
        public static float InjectGas(
            float[] density,
            StrataGasDef gas,
            float amount,
            Map map,
            float maxFraction,
            bool bypassCap)
        {
            if (density == null || gas == null || amount <= 0f)
            {
                return 0f;
            }

            int idx = gas.index;
            if (bypassCap && IsPressurizedGas(gas))
            {
                density[idx] = Mathf.Min(MaxDeepGasDensity, density[idx] + amount);
                return amount;
            }

            float current = density[idx];
            float cap = bypassCap ? 1f : Mathf.Clamp01(maxFraction);
            float target = Mathf.Min(cap, current + amount);
            float delta = target - current;
            if (delta <= NormalizeEpsilon)
            {
                return 0f;
            }

            if (IsPollutantGas(gas))
            {
                delta = ClampPollutantInjection(density, map, delta);
                if (delta <= NormalizeEpsilon)
                {
                    return 0f;
                }
                density[idx] = current + delta;
                DisplaceInertsOnly(density, idx, delta);
            }
            else
            {
                density[idx] = target;
                DisplaceOthers(density, idx, delta);
            }

            FinalizeMix(density, map);
            return delta;
        }

        public static void ConsumeGas(float[] density, StrataGasDef gas, float amount, Map map)
        {
            if (density == null || gas == null || amount <= 0f)
            {
                return;
            }

            int idx = gas.index;
            float removed = Mathf.Min(density[idx], amount);
            if (removed <= NormalizeEpsilon)
            {
                return;
            }
            density[idx] -= removed;
            RefillWithAmbient(density, map, removed);
            FinalizeMix(density, map);
        }

        public static void RefillWithAmbient(float[] density, Map map, float volume)
        {
            if (density == null || volume <= NormalizeEpsilon)
            {
                return;
            }
            TargetMix target = TargetForMap(map);
            int n2 = IndexOf(StrataGasDefOf.Strata_Nitrogen);
            int o2 = IndexOf(StrataGasDefOf.Strata_Oxygen);
            int ar = IndexOf(StrataGasDefOf.Strata_Argon);
            int co2 = IndexOf(StrataGasDefOf.Strata_CarbonDioxide);
            if (n2 >= 0)
            {
                density[n2] += volume * target.nitrogen;
            }
            if (o2 >= 0)
            {
                density[o2] += volume * target.oxygen;
            }
            if (ar >= 0)
            {
                density[ar] += volume * target.argon;
            }
            if (co2 >= 0)
            {
                density[co2] += volume * target.carbonDioxide;
            }
        }

        // Outdoor / vent flush: pollutants leave fast; removed volume backfills with
        // ambient mix and active intake pulls O₂ (and other breathables) toward the
        // depth-adjusted target. O₂ is never drained here — only imported. Sealed
        // rooms never call this; every outdoor exhaust path does (doors, vents,
        // open roof, fans, ducts, gas-pipe terminals, open-air disperse).
        public static void ApplyOutdoorVentDrain(float[] density, Map map, float drainRate)
        {
            if (density == null || drainRate <= NormalizeEpsilon)
            {
                return;
            }
            drainRate = Mathf.Clamp01(drainRate);
            TargetMix target = TargetForMap(map);
            float removed = 0f;
            float ambientDrain = drainRate * 0.35f;
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                float d = density[gas.index];
                if (d <= NormalizeEpsilon)
                {
                    continue;
                }
                if (IsPollutantGas(gas))
                {
                    float pollutantDrop = d * drainRate;
                    if (pollutantDrop <= NormalizeEpsilon)
                    {
                        continue;
                    }
                    density[gas.index] = d - pollutantDrop;
                    removed += pollutantDrop;
                    continue;
                }
                if (!IsAtmosphericComponent(gas))
                {
                    continue;
                }
                // Life-support: vent paths intake O₂, never exhaust it.
                if (gas == StrataGasDefOf.Strata_Oxygen)
                {
                    continue;
                }
                float targetFrac = TargetFraction(gas, target);
                if (d <= targetFrac + NormalizeEpsilon)
                {
                    continue;
                }
                float excess = d - targetFrac;
                float drop = excess * ambientDrain;
                if (drop <= NormalizeEpsilon)
                {
                    continue;
                }
                density[gas.index] = d - drop;
                removed += drop;
            }
            RefillWithAmbient(density, map, removed);
            float intakeStrength = Mathf.Clamp01(drainRate * 0.82f + 0.15f);
            IntakeFreshAir(density, map, intakeStrength);
            FinalizeMix(density, map);
        }

        // Active ambient intake for outdoor-vent paths: push breathables toward the
        // map target (surface ~21% O₂, B1+ depth-scarce mix). O₂ lerps faster than
        // other channels so a vented smoky kitchen gains O₂ as smoke leaves.
        public static void IntakeFreshAir(float[] density, Map map, float strength)
        {
            if (density == null || map == null || strength <= NormalizeEpsilon)
            {
                return;
            }
            strength = Mathf.Clamp01(strength);
            if (ForcesAmbientInEnclosedRooms(map))
            {
                EnforceAmbientBreathables(density, map);
                return;
            }

            TargetMix target = TargetForMap(map);
            float reserved = PollutantReservedFraction(density, target);
            float ambientBudget = Mathf.Max(0f, 1f - reserved);
            float goalO2 = target.oxygen * ambientBudget;
            float goalN2 = target.nitrogen * ambientBudget;
            float goalAr = target.argon * ambientBudget;
            float goalCo2 = target.carbonDioxide * ambientBudget;
            float o2Strength = Mathf.Clamp01(strength * 1.4f);
            ApplyAmbientChannel(density, StrataGasDefOf.Strata_Oxygen, goalO2, o2Strength, hardLock: false);
            ApplyAmbientChannel(density, StrataGasDefOf.Strata_Nitrogen, goalN2, strength, hardLock: false);
            ApplyAmbientChannel(density, StrataGasDefOf.Strata_Argon, goalAr, strength, hardLock: false);
            ApplyAmbientChannel(density, StrataGasDefOf.Strata_CarbonDioxide, goalCo2, strength, hardLock: false);
        }

        public static void EnsureNormalized(float[] density)
        {
            if (density == null)
            {
                return;
            }
            float total = ComposableTotal(density);
            if (total <= 1f + NormalizeEpsilon)
            {
                return;
            }
            int deepIdx = DeepGasIndex();
            float scale = 1f / total;
            for (int i = 0; i < density.Length; i++)
            {
                if (i == deepIdx && density[i] > 1f)
                {
                    float over = density[i] - 1f;
                    density[i] = 1f + over * scale;
                }
                else
                {
                    density[i] *= scale;
                }
            }
        }

        // Scale oversum mixes, then hard-lock surface/A+ breathables so O₂ is
        // never scaled down by smoke + ambient exceeding 1.0.
        public static void FinalizeMix(float[] density, Map map)
        {
            EnsureNormalized(density);
            if (map != null && ForcesAmbientInEnclosedRooms(map))
            {
                EnforceAmbientBreathables(density, map);
            }
        }

        public static void ApplyToRoom(
            AtmosphereMapComponent atmosphere,
            Room room,
            IntVec3 sample,
            Map map,
            float strength)
        {
            if (atmosphere == null || room == null || strength <= 0f
                || !atmosphere.EnsureRoomDensity(room, sample, out float[] density))
            {
                return;
            }

            TargetMix target = TargetForMap(map);
            strength = Mathf.Clamp01(strength);
            bool hardLock = strength >= 0.98f;
            bool surfaceAmbient = ForcesAmbientInEnclosedRooms(map);

            if (surfaceAmbient && hardLock)
            {
                // Infinite outdoor air: O₂/CO₂/Ar stay at ambient; pollutants
                // reserve volume from N₂ only (never steal breathables).
                float reserved = PollutantReservedFraction(density, target);
                float goalN2 = Mathf.Max(0f, 1f - reserved
                    - target.oxygen - target.argon - target.carbonDioxide);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Oxygen, target.oxygen, strength, hardLock: true);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Argon, target.argon, strength, hardLock: true);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_CarbonDioxide, target.carbonDioxide, strength, hardLock: true);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Nitrogen, goalN2, strength, hardLock: true);
            }
            else
            {
                float reserved = ReservedFraction(density, target);
                float ambientBudget = Mathf.Max(0f, 1f - reserved);
                float goalN2 = target.nitrogen * ambientBudget;
                float goalO2 = target.oxygen * ambientBudget;
                float goalAr = target.argon * ambientBudget;
                float goalCo2 = target.carbonDioxide * ambientBudget;

                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Nitrogen, goalN2, strength, hardLock);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Oxygen, goalO2, strength, hardLock);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_Argon, goalAr, strength, hardLock);
                ApplyAmbientChannel(density, StrataGasDefOf.Strata_CarbonDioxide, goalCo2, strength, hardLock);
            }

            FinalizeMix(density, map);
            atmosphere.WriteRoomDensity(room, density, sample);
        }

        // Old saves: O₂-only backfill, or dual-channel smoke+ambient oversum → normalize.
        public static void MigrateLegacyRoomMix(AtmosphereMapComponent atmosphere, Room room, IntVec3 sample, Map map)
        {
            if (atmosphere == null || room == null || !atmosphere.TryGetRoomDensity(room, out float[] density))
            {
                return;
            }

            float total = ComposableTotal(density);
            if (total > OversumMigrateThreshold)
            {
                float scale = 1f / total;
                for (int i = 0; i < density.Length; i++)
                {
                    density[i] *= scale;
                }
                atmosphere.WriteRoomDensity(room, density, sample);
                return;
            }

            if (StrataGasDefOf.Strata_Nitrogen == null)
            {
                return;
            }
            float o2 = density[StrataGasDefOf.Strata_Oxygen.index];
            float n2 = density[StrataGasDefOf.Strata_Nitrogen.index];
            if (o2 <= 0.05f || n2 > 0.01f)
            {
                return;
            }
            ApplyToRoom(atmosphere, room, sample, map, strength: 1f);
        }

        public static void MergeRoomMix(float[] into, float[] other, int intoCells, int otherCells)
        {
            if (into == null || other == null || intoCells <= 0 || otherCells <= 0)
            {
                return;
            }
            float totalCells = intoCells + otherCells;
            for (int i = 0; i < into.Length && i < other.Length; i++)
            {
                into[i] = (into[i] * intoCells + other[i] * otherCells) / totalCells;
            }
            EnsureNormalized(into);
        }

        public static void EnforceAmbientBreathables(float[] density, Map map)
        {
            if (density == null || !ForcesAmbientInEnclosedRooms(map))
            {
                return;
            }
            TargetMix target = TargetForMap(map);
            int o2 = IndexOf(StrataGasDefOf.Strata_Oxygen);
            int ar = IndexOf(StrataGasDefOf.Strata_Argon);
            int co2 = IndexOf(StrataGasDefOf.Strata_CarbonDioxide);
            if (o2 >= 0)
            {
                density[o2] = target.oxygen;
            }
            if (ar >= 0)
            {
                density[ar] = target.argon;
            }
            if (co2 >= 0)
            {
                density[co2] = target.carbonDioxide;
            }
            float reserved = PollutantReservedFraction(density, target);
            int n2 = IndexOf(StrataGasDefOf.Strata_Nitrogen);
            if (n2 >= 0)
            {
                density[n2] = Mathf.Max(0f, 1f - reserved - target.oxygen - target.argon - target.carbonDioxide);
            }
        }

        // Pollutants + pressurized overage + excess CO₂ — never O₂ deficit.
        private static float PollutantReservedFraction(float[] density, TargetMix target)
        {
            float reserved = 0f;
            List<StrataGasDef> gases = AtmosphereMapComponent.Gases;
            for (int i = 0; i < gases.Count; i++)
            {
                StrataGasDef gas = gases[i];
                if (IsAtmosphericComponent(gas))
                {
                    continue;
                }
                float d = density[gas.index];
                if (d > NormalizeEpsilon)
                {
                    reserved += d;
                }
            }
            reserved += PressurizedOverage(density);
            int co2Idx = IndexOf(StrataGasDefOf.Strata_CarbonDioxide);
            if (co2Idx >= 0)
            {
                reserved += Mathf.Max(0f, density[co2Idx] - target.carbonDioxide);
            }
            return Mathf.Clamp(reserved, 0f, 1f);
        }

        private static float ReservedFraction(float[] density, TargetMix target)
        {
            float reserved = PollutantReservedFraction(density, target);
            int o2Idx = IndexOf(StrataGasDefOf.Strata_Oxygen);
            if (o2Idx >= 0)
            {
                reserved += Mathf.Max(0f, target.oxygen - density[o2Idx]);
            }
            return Mathf.Clamp(reserved, 0f, 1f);
        }

        private static void ApplyAmbientChannel(
            float[] density,
            StrataGasDef gas,
            float goal,
            float strength,
            bool hardLock)
        {
            if (gas == null)
            {
                return;
            }
            float current = density[gas.index];
            float next = hardLock ? goal : Mathf.Lerp(current, goal, strength);
            if (Mathf.Abs(next - current) > NormalizeEpsilon * 0.5f)
            {
                density[gas.index] = Mathf.Max(0f, next);
            }
        }

        // Pollutant inflow budget: respect load cap, fill composable headroom
        // without displacement, then consume N₂ then Ar only — never O₂/CO₂.
        private static float ClampPollutantInjection(float[] density, Map map, float requested)
        {
            float pollutantLoad = PollutantFraction(density, map);
            if (pollutantLoad >= PollutantLoadFullThreshold)
            {
                return 0f;
            }

            float byLoad = PollutantLoadFullThreshold - pollutantLoad;
            float delta = Mathf.Min(requested, byLoad);

            float headroom = Mathf.Max(0f, 1f - ComposableTotal(density));
            float withoutDisplace = Mathf.Min(delta, headroom);
            float needsInert = delta - withoutDisplace;
            if (needsInert > NormalizeEpsilon)
            {
                needsInert = Mathf.Min(needsInert, AvailableInertBuffer(density));
            }
            return withoutDisplace + needsInert;
        }

        private static float AvailableInertBuffer(float[] density)
        {
            int n2 = IndexOf(StrataGasDefOf.Strata_Nitrogen);
            int ar = IndexOf(StrataGasDefOf.Strata_Argon);
            float avail = 0f;
            if (n2 >= 0)
            {
                avail += density[n2];
            }
            if (ar >= 0)
            {
                avail += density[ar];
            }
            return avail;
        }

        // N₂ first, then Ar. O₂ and CO₂ are life-support — never touched here.
        private static void DisplaceInertsOnly(float[] density, int exceptIdx, float amount)
        {
            int n2 = IndexOf(StrataGasDefOf.Strata_Nitrogen);
            if (n2 >= 0 && n2 != exceptIdx && amount > NormalizeEpsilon)
            {
                float take = Mathf.Min(density[n2], amount);
                density[n2] -= take;
                amount -= take;
            }
            int ar = IndexOf(StrataGasDefOf.Strata_Argon);
            if (ar >= 0 && ar != exceptIdx && amount > NormalizeEpsilon)
            {
                float take = Mathf.Min(density[ar], amount);
                density[ar] -= take;
                amount -= take;
            }
        }

        private static void DisplaceOthers(float[] density, int exceptIdx, float amount)
        {
            int n2 = IndexOf(StrataGasDefOf.Strata_Nitrogen);
            if (n2 >= 0 && n2 != exceptIdx && amount > NormalizeEpsilon)
            {
                float take = Mathf.Min(density[n2], amount);
                density[n2] -= take;
                amount -= take;
            }
            if (amount <= NormalizeEpsilon)
            {
                return;
            }

            float sum = 0f;
            for (int i = 0; i < density.Length; i++)
            {
                if (i == exceptIdx)
                {
                    continue;
                }
                if (i == DeepGasIndex() && density[i] > 1f)
                {
                    continue;
                }
                sum += density[i];
            }
            if (sum <= NormalizeEpsilon)
            {
                return;
            }
            for (int i = 0; i < density.Length; i++)
            {
                if (i == exceptIdx)
                {
                    continue;
                }
                if (i == DeepGasIndex() && density[i] > 1f)
                {
                    continue;
                }
                float share = density[i] / sum;
                density[i] -= amount * share;
                if (density[i] < 0f)
                {
                    density[i] = 0f;
                }
            }
        }

        private static int DeepGasIndex()
        {
            return StrataGasDefOf.Strata_DeepGas?.index ?? -1;
        }

        private static float TargetFraction(StrataGasDef gas, TargetMix target)
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

        private static int IndexOf(StrataGasDef gas)
        {
            return gas?.index ?? -1;
        }
    }
}
