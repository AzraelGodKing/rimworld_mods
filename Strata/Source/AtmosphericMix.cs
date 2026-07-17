using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Earth-like atmospheric baseline and depth-scarce O₂ for underground levels.
    // Room clouds store each channel as a volume fraction; ambient supply tops
    // them toward the targets below. Pollutants (smoke, deep gas, spores) stay
    // additive on their own channels.
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

        public static void ApplyToRoom(
            AtmosphereMapComponent atmosphere,
            Room room,
            IntVec3 sample,
            Map map,
            float strength)
        {
            if (atmosphere == null || room == null || strength <= 0f)
            {
                return;
            }
            TargetMix target = TargetForMap(map);
            strength = Mathf.Clamp01(strength);
            bool hardLock = strength >= 0.98f;

            ApplyChannel(atmosphere, room, sample, StrataGasDefOf.Strata_Nitrogen, target.nitrogen, strength, hardLock);
            ApplyChannel(atmosphere, room, sample, StrataGasDefOf.Strata_Oxygen, target.oxygen, strength, hardLock);
            ApplyChannel(atmosphere, room, sample, StrataGasDefOf.Strata_Argon, target.argon, strength, hardLock);
            ApplyChannel(atmosphere, room, sample, StrataGasDefOf.Strata_CarbonDioxide, target.carbonDioxide, strength, hardLock);
        }

        private static void ApplyChannel(
            AtmosphereMapComponent atmosphere,
            Room room,
            IntVec3 sample,
            StrataGasDef gas,
            float goal,
            float strength,
            bool hardLock)
        {
            if (gas == null)
            {
                return;
            }
            float current = atmosphere.DensityInRoom(room, gas);
            if (hardLock)
            {
                if (Mathf.Abs(current - goal) > 0.002f)
                {
                    atmosphere.SetRoomGasDensity(room, gas, goal, sample);
                }
                return;
            }
            float next = Mathf.Lerp(current, goal, strength);
            if (Mathf.Abs(next - current) > 0.0005f)
            {
                atmosphere.SetRoomGasDensity(room, gas, next, sample);
            }
        }

        // Old saves that only stored O₂ at ~21%: backfill N₂/Ar/CO₂ once.
        public static void MigrateLegacyRoomMix(AtmosphereMapComponent atmosphere, Room room, IntVec3 sample, Map map)
        {
            if (atmosphere == null || room == null || StrataGasDefOf.Strata_Nitrogen == null)
            {
                return;
            }
            float o2 = atmosphere.DensityInRoom(room, StrataGasDefOf.Strata_Oxygen);
            float n2 = atmosphere.DensityInRoom(room, StrataGasDefOf.Strata_Nitrogen);
            if (o2 <= 0.05f || n2 > 0.01f)
            {
                return;
            }
            ApplyToRoom(atmosphere, room, sample, map, strength: 1f);
        }

        public static bool IsAtmosphericComponent(StrataGasDef gas)
        {
            return gas == StrataGasDefOf.Strata_Nitrogen
                || gas == StrataGasDefOf.Strata_Oxygen
                || gas == StrataGasDefOf.Strata_Argon
                || gas == StrataGasDefOf.Strata_CarbonDioxide;
        }
    }
}
