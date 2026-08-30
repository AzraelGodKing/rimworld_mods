using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class StormproofSettings : ModSettings
    {
        public bool enableBrownout = true;
        public float brownoutSeverity = 1f;
        public bool enableStormWear = true;
        public bool enableAlmanac = true;
        public bool enableFulgurite = true;

        public bool incidentIonStorm = true;
        public bool incidentHeatDome = true;
        public bool incidentPolarFront = true;
        public bool incidentToxicSurge = true;
        public bool incidentDryLightning = true;
        public float incidentFrequency = 1f;

        public bool allowAtmosphericBarrier = true;
        public bool allowClimateStabilizer = true;
        public bool allowSkyRestorer = true;
        public bool allowFireSuppressor = true;
        public bool allowDroughtCondenser = true;

        public float zzztChanceFactor = 1f;

        private static readonly Dictionary<string, float> OriginalIncidentChance =
            new Dictionary<string, float>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableBrownout, "enableBrownout", true);
            Scribe_Values.Look(ref brownoutSeverity, "brownoutSeverity", 1f);
            Scribe_Values.Look(ref enableStormWear, "enableStormWear", true);
            Scribe_Values.Look(ref enableAlmanac, "enableAlmanac", true);
            Scribe_Values.Look(ref enableFulgurite, "enableFulgurite", true);
            Scribe_Values.Look(ref incidentIonStorm, "incidentIonStorm", true);
            Scribe_Values.Look(ref incidentHeatDome, "incidentHeatDome", true);
            Scribe_Values.Look(ref incidentPolarFront, "incidentPolarFront", true);
            Scribe_Values.Look(ref incidentToxicSurge, "incidentToxicSurge", true);
            Scribe_Values.Look(ref incidentDryLightning, "incidentDryLightning", true);
            Scribe_Values.Look(ref incidentFrequency, "incidentFrequency", 1f);
            Scribe_Values.Look(ref allowAtmosphericBarrier, "allowAtmosphericBarrier", true);
            Scribe_Values.Look(ref allowClimateStabilizer, "allowClimateStabilizer", true);
            Scribe_Values.Look(ref allowSkyRestorer, "allowSkyRestorer", true);
            Scribe_Values.Look(ref allowFireSuppressor, "allowFireSuppressor", true);
            Scribe_Values.Look(ref allowDroughtCondenser, "allowDroughtCondenser", true);
            Scribe_Values.Look(ref zzztChanceFactor, "zzztChanceFactor", 1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Clamp();
                ApplyIncidentChances();
            }
        }

        public void ResetToDefaults()
        {
            enableBrownout = true;
            brownoutSeverity = 1f;
            enableStormWear = true;
            enableAlmanac = true;
            enableFulgurite = true;
            incidentIonStorm = true;
            incidentHeatDome = true;
            incidentPolarFront = true;
            incidentToxicSurge = true;
            incidentDryLightning = true;
            incidentFrequency = 1f;
            allowAtmosphericBarrier = true;
            allowClimateStabilizer = true;
            allowSkyRestorer = true;
            allowFireSuppressor = true;
            allowDroughtCondenser = true;
            zzztChanceFactor = 1f;
            ApplyIncidentChances();
        }

        public void ApplySoft()
        {
            ResetToDefaults();
            enableStormWear = false;
            brownoutSeverity = 0.6f;
            incidentFrequency = 0.6f;
            zzztChanceFactor = 0.5f;
            ApplyIncidentChances();
        }

        public void ApplyHard()
        {
            ResetToDefaults();
            brownoutSeverity = 1.35f;
            incidentFrequency = 1.5f;
            zzztChanceFactor = 1.5f;
            ApplyIncidentChances();
        }

        public void Clamp()
        {
            brownoutSeverity = brownoutSeverity < 0.25f ? 0.25f : (brownoutSeverity > 2f ? 2f : brownoutSeverity);
            incidentFrequency = incidentFrequency < 0f ? 0f : (incidentFrequency > 2.5f ? 2.5f : incidentFrequency);
            zzztChanceFactor = zzztChanceFactor < 0f ? 0f : (zzztChanceFactor > 3f ? 3f : zzztChanceFactor);
        }

        public bool IncidentEnabled(string defName)
        {
            switch (defName)
            {
                case "Stormproof_IonStorm": return incidentIonStorm;
                case "Stormproof_HeatDome": return incidentHeatDome;
                case "Stormproof_PolarFront": return incidentPolarFront;
                case "Stormproof_ToxicSurge": return incidentToxicSurge;
                case "Stormproof_DryLightning": return incidentDryLightning;
                default: return true;
            }
        }

        public static void CaptureOriginalChances()
        {
            Remember("Stormproof_IonStorm");
            Remember("Stormproof_HeatDome");
            Remember("Stormproof_PolarFront");
            Remember("Stormproof_ToxicSurge");
            Remember("Stormproof_DryLightning");
        }

        public void ApplyIncidentChances()
        {
            Clamp();
            ApplyOne("Stormproof_IonStorm", incidentIonStorm);
            ApplyOne("Stormproof_HeatDome", incidentHeatDome);
            ApplyOne("Stormproof_PolarFront", incidentPolarFront);
            ApplyOne("Stormproof_ToxicSurge", incidentToxicSurge);
            ApplyOne("Stormproof_DryLightning", incidentDryLightning);
        }

        private static void Remember(string defName)
        {
            if (OriginalIncidentChance.ContainsKey(defName))
            {
                return;
            }
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            if (def != null)
            {
                OriginalIncidentChance[defName] = def.baseChance;
            }
        }

        private void ApplyOne(string defName, bool enabled)
        {
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }
            if (!OriginalIncidentChance.ContainsKey(defName))
            {
                OriginalIncidentChance[defName] = def.baseChance;
            }
            def.baseChance = enabled ? OriginalIncidentChance[defName] * incidentFrequency : 0f;
        }
    }
}
