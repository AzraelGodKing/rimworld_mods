using RimWorld;
using Verse;

namespace Strata
{
    // Mine canaries react to bad air sooner than colonists — used by canary cages.
    public static class StrataCanaryUtility
    {
        // Toxic gases: distress / death as a fraction of each gas's harmThreshold.
        public const float DistressFractionAbove = 0.55f;
        public const float LethalFractionAbove = 0.88f;

        // Oxygen (harmWhenBelow): warn while O₂ is still above the human harm line.
        public const float DistressFractionBelowOxygen = 1.30f;
        public const float LethalFractionBelowOxygen = 0.92f;

        public enum ThreatLevel
        {
            None = 0,
            Distress = 1,
            Lethal = 2,
        }

        public static bool NaturalGasesActive =>
            StrataMod.Settings == null || StrataMod.Settings.NaturalGasesActive;

        public static bool PollutantGasesActive =>
            StrataMod.Settings == null || StrataMod.Settings.PollutantGasesActive;

        public static ThreatLevel EvaluateRoom(AtmosphereMapComponent atmosphere, Room room, out StrataGasDef worstGas)
        {
            worstGas = null;
            if (atmosphere == null || room == null || room.PsychologicallyOutdoors)
            {
                return ThreatLevel.None;
            }
            ThreatLevel worst = ThreatLevel.None;
            foreach (StrataGasDef gas in AtmosphereMapComponent.Gases)
            {
                if (gas?.harmHediff == null)
                {
                    continue;
                }
                if (!NaturalGasesActive
                    && (gas.harmWhenBelow
                        || gas == StrataGasDefOf.Strata_Oxygen
                        || gas == StrataGasDefOf.Strata_CarbonDioxide))
                {
                    continue;
                }
                if (!PollutantGasesActive && AtmosphericMix.IsPollutantGas(gas))
                {
                    continue;
                }
                ThreatLevel level = ThreatLevelFor(gas, atmosphere.DensityInRoom(room, gas));
                if (level > worst)
                {
                    worst = level;
                    worstGas = gas;
                }
            }
            return worst;
        }

        public static ThreatLevel ThreatLevelFor(StrataGasDef gas, float density)
        {
            if (gas.harmWhenBelow)
            {
                if (density <= gas.harmThreshold * LethalFractionBelowOxygen)
                {
                    return ThreatLevel.Lethal;
                }
                if (density <= gas.harmThreshold * DistressFractionBelowOxygen)
                {
                    return ThreatLevel.Distress;
                }
                return ThreatLevel.None;
            }
            if (density >= gas.harmThreshold * LethalFractionAbove)
            {
                return ThreatLevel.Lethal;
            }
            if (density >= gas.harmThreshold * DistressFractionAbove)
            {
                return ThreatLevel.Distress;
            }
            return ThreatLevel.None;
        }
    }
}
